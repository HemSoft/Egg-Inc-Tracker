SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[TrackSoulEggGains]
    @PlayerName NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    -- First CTE: Get initial records with proper numeric conversion
    WITH
        PlayerRecords
        AS
        (
            SELECT
                PlayerName,
                SoulEggs,
                Updated,
                -- Extract numeric part and handle scientific notation
                CASE 
                WHEN SoulEggs LIKE '%s' THEN 
                    TRY_CAST(LEFT(SoulEggs, CHARINDEX('s', SoulEggs) - 1) AS DECIMAL(38,3))
                ELSE NULL 
            END AS NumericPart,
                -- Get whole number for milestone tracking
                CASE 
                WHEN SoulEggs LIKE '%s' THEN 
                    FLOOR(TRY_CAST(LEFT(SoulEggs, CHARINDEX('s', SoulEggs) - 1) AS DECIMAL(38,3)))
                ELSE NULL 
            END AS WholeNumber
            FROM [dbo].[Players]
            WHERE PlayerName = @PlayerName
                AND SoulEggs LIKE '%s' -- Only consider 's' notation records
                AND ISNUMERIC(LEFT(SoulEggs, CHARINDEX('s', SoulEggs) - 1)) = 1
            -- Ensure valid number
        ),
        -- Second CTE: Find first time each milestone was crossed
        FirstCrossings
        AS
        (
            SELECT
                PlayerName,
                WholeNumber,
                MIN(Updated) as CrossingTime
            FROM PlayerRecords
            WHERE WholeNumber IS NOT NULL
            GROUP BY PlayerName, WholeNumber
        ),
        -- Third CTE: Calculate time differences between milestones
        MilestoneCrossings
        AS
        (
            SELECT
                PlayerName,
                WholeNumber,
                CrossingTime,
                LAG(CrossingTime) OVER (PARTITION BY PlayerName ORDER BY WholeNumber) as PreviousCrossing,
                LAG(WholeNumber) OVER (PARTITION BY PlayerName ORDER BY WholeNumber) as PreviousWholeNumber
            FROM FirstCrossings
        )
    -- Final SELECT: Format output with proper timezone handling
    SELECT
        mc.PlayerName,
        pr.SoulEggs as CurrentSoulEggs,
        CAST(mc.WholeNumber as VARCHAR(20)) + 's' as Milestone,
        FORMAT(mc.CrossingTime AT TIME ZONE 'UTC' AT TIME ZONE 'Central Standard Time', 'yyyy-MM-dd HH:mm') AS UpdatedTime,
        CASE 
            WHEN mc.PreviousCrossing IS NULL THEN 'First Record'
            ELSE CAST(
                    -- Calculate full days between dates using CEILING of decimal days
                    CEILING(
                        CAST(DATEDIFF(MINUTE, mc.PreviousCrossing, mc.CrossingTime) AS FLOAT) / (24.0 * 60.0)
                    )
                AS VARCHAR(10)) + ' days'
        END as DaysToReach,
        'Crossed ' + CAST(mc.WholeNumber AS VARCHAR(20)) + 's' as ChangeType,
        -- Add projected next milestone based on current rate
        CASE 
            WHEN mc.PreviousCrossing IS NOT NULL THEN
                FORMAT(
                    DATEADD(DAY, 
                        CEILING(
                            CAST(DATEDIFF(MINUTE, mc.PreviousCrossing, mc.CrossingTime) AS FLOAT) / (24.0 * 60.0)
                        ),
                        mc.CrossingTime
                    ) AT TIME ZONE 'UTC' AT TIME ZONE 'Central Standard Time',
                    'yyyy-MM-dd HH:mm'
                )
            ELSE NULL
        END as ProjectedNextMilestone
    FROM MilestoneCrossings mc
        INNER JOIN PlayerRecords pr ON pr.PlayerName = mc.PlayerName
            AND pr.Updated = mc.CrossingTime
    WHERE mc.WholeNumber >= 1
    ORDER BY mc.CrossingTime DESC;
END
GO
