SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GetRankedPlayerRecords]
    @PlayerName NVARCHAR(MAX) = NULL,
    @RecordLimit INT = 40,
    @SampleDaysBack INT = 30,
    @MERGoal DECIMAL(5,2) = NULL,
    @JERGoal DECIMAL(5,2) = NULL,
    @SEGoal NVARCHAR(10) = NULL,
    @EBGoal NVARCHAR(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- First CTE: Get Ranked Records
    WITH
        RankedRecords
        AS
        (
            SELECT
                p.*,
                FORMAT(p.[TitleProgress], 'N2') AS Progr,
                FORMAT(p.ProjectedTitleChange AT TIME ZONE 'UTC' AT TIME ZONE 'Central Standard Time', 'yyyy-MM-dd HH:mm') AS ProjectedFormatted,
                FORMAT(p.Updated AT TIME ZONE 'UTC' AT TIME ZONE 'Central Standard Time', 'yyyy-MM-dd HH:mm') AS UpdatedFormatted,
                TRY_CAST(REPLACE(p.[SoulEggsFull], ',', '') AS DECIMAL(38, 0)) AS SoulEggsFullNum,
                ROW_NUMBER() OVER (PARTITION BY p.[PlayerName] ORDER BY p.[Updated] DESC) AS rn
            FROM [dbo].[Players] p
            WHERE (@PlayerName IS NULL OR p.[PlayerName] = @PlayerName)
        ),
        -- Second CTE: CalculatedRecords using OUTER APPLY
        CalculatedRecords
        AS
        (
            SELECT
                rr.[Id],
                rr.[PlayerName],
                rr.[Title],
                rr.Progr,
                rr.[NextTitle],
                rr.ProjectedFormatted AS Projected,
                rr.UpdatedFormatted AS Updated,
                rr.[SoulEggs],
                rr.[ProphecyEggs] AS PE,
                rr.[EarningsBonusPercentage] AS EarnBonus,
                rr.[EarningsBonusPerHour] AS EBHour,
                rr.[MER],
                rr.[JER],
                rr.[LLC],
                rr.rn,
                rr.[Updated] AS CurrentUpdated,
                rr.SoulEggsFullNum AS CurrentSoulEggsFullNum,
                -- Use OUTER APPLY to get the earliest record within the past @SampleDaysBack days
                ca.EarliestUpdated,
                ca.EarliestSoulEggsFullNum
            FROM RankedRecords rr
        OUTER APPLY (
            SELECT TOP 1
                    rr_prev.[Updated] AS EarliestUpdated,
                    rr_prev.SoulEggsFullNum AS EarliestSoulEggsFullNum
                FROM RankedRecords rr_prev
                WHERE rr_prev.[PlayerName] = rr.[PlayerName]
                    AND rr_prev.[Updated] >= DATEADD(DAY, -@SampleDaysBack, rr.[Updated])
                    AND rr_prev.[Updated] <= rr.[Updated]
                ORDER BY rr_prev.[Updated] ASC
        ) ca
        ),
        -- Third CTE: FinalCalculations
        FinalCalculations
        AS
        (
            SELECT
                *,
                -- Calculate Total Soul Eggs Gained and Time Difference
                CASE
                WHEN EarliestSoulEggsFullNum IS NOT NULL AND CurrentSoulEggsFullNum >= EarliestSoulEggsFullNum THEN
                    CurrentSoulEggsFullNum - EarliestSoulEggsFullNum
                ELSE NULL
            END AS TotalSoulEggsGained,
                DATEDIFF(SECOND, EarliestUpdated, CurrentUpdated) AS TimeDifferenceSeconds
            FROM CalculatedRecords
        ),
        -- Fourth CTE: Calculate rates with window functions for max values
        RatesCalculation
        AS
        (
            SELECT
                *,
                -- Calculate current rates
                CASE 
                WHEN TotalSoulEggsGained IS NOT NULL AND TimeDifferenceSeconds > 0 THEN
                    CAST(TotalSoulEggsGained / (TimeDifferenceSeconds / 3600.0) AS DECIMAL(38, 0))
                ELSE NULL
            END AS SEHourRate,
                CASE 
                WHEN TotalSoulEggsGained IS NOT NULL AND TimeDifferenceSeconds > 0 THEN
                    CAST(TotalSoulEggsGained / (TimeDifferenceSeconds / 86400.0) AS DECIMAL(38, 0))
                ELSE NULL
            END AS SEDayRate,
                CASE 
                WHEN TotalSoulEggsGained IS NOT NULL AND TimeDifferenceSeconds > 0 THEN
                    CAST(TotalSoulEggsGained / (TimeDifferenceSeconds / 604800.0) AS DECIMAL(38, 0))
                ELSE NULL
            END AS SEWeekRate
            FROM FinalCalculations
        )
    SELECT
        [Id],
        [PlayerName],
        Progr AS P,
        Updated,
        PE,
        EarnBonus AS EB,
        @EBGoal AS EBG,
        [SoulEggs] AS SE,
        @SEGoal AS SEG,
        dbo.FormatBigInteger(SEHourRate) AS SEH,
        dbo.FormatBigInteger(MAX(SEHourRate) OVER (PARTITION BY [PlayerName])) AS SEHM,
        dbo.FormatBigInteger(SEDayRate) AS SED,
        dbo.FormatBigInteger(MAX(SEDayRate) OVER (PARTITION BY [PlayerName])) AS SEDM,
        dbo.FormatBigInteger(SEWeekRate) AS SEW,
        dbo.FormatBigInteger(MAX(SEWeekRate) OVER (PARTITION BY [PlayerName])) AS SEWM,
        Projected,
        EBHour,
        [MER],
        @MERGoal AS MERGoal,
        [JER],
        @JERGoal AS JERGoal,
        [Title],
        [NextTitle] AS NTitle,
        [LLC]
    FROM RatesCalculation
    WHERE rn <= @RecordLimit
    ORDER BY [PlayerName], rn;
END
GO
