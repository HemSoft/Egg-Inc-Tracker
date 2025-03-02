SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GetEBHistoryForExcel]
    @PlayerName NVARCHAR(MAX) = NULL,
    @Days INT = 30
AS
BEGIN
    SET NOCOUNT ON;

    -- First, get the base data with timestamps and clean EB values
    WITH
        BaseData
        AS
        (
            SELECT
                PlayerName,
                Updated,
                EarningsBonusPercentage,
                FORMAT(Updated AT TIME ZONE 'UTC' AT TIME ZONE 'Central Standard Time', 'yyyy-MM-dd HH:mm') AS UpdatedFormatted,
                -- Remove % and commas, then extract numeric part
                REPLACE(
                REPLACE(
                    SUBSTRING(
                        EarningsBonusPercentage,
                        1,
                        CASE 
                            WHEN CHARINDEX('%', EarningsBonusPercentage) > 0 
                            THEN CHARINDEX('%', EarningsBonusPercentage) - 1 
                            ELSE LEN(EarningsBonusPercentage) 
                        END
                    ),
                    ',', ''
                ),
                ' ', ''
            ) as CleanValue,
                -- Get the last character before % (the suffix)
                RIGHT(
                REPLACE(
                    SUBSTRING(
                        EarningsBonusPercentage,
                        1,
                        CASE 
                            WHEN CHARINDEX('%', EarningsBonusPercentage) > 0 
                            THEN CHARINDEX('%', EarningsBonusPercentage) - 1 
                            ELSE LEN(EarningsBonusPercentage) 
                        END
                    ),
                    ' ', ''
                ),
                1
            ) as Suffix
            FROM Players
            WHERE (@PlayerName IS NULL OR PlayerName = @PlayerName)
                AND Updated >= DATEADD(DAY, -@Days, GETUTCDATE())
        ),
        -- Get the first record for each player
        FirstRecords
        AS
        (
            SELECT
                PlayerName,
                CleanValue,
                Suffix,
                ROW_NUMBER() OVER (PARTITION BY PlayerName ORDER BY Updated) as RowNum
            FROM BaseData
        ),
        -- Create a suffix ranking table
        SuffixRanks
        AS
        (
            SELECT Value, Rank
            FROM (VALUES
                    ('', 0),
                    ('K', 1),
                    ('M', 2),
                    ('B', 3),
                    ('T', 4),
                    ('q', 5),
                    ('Q', 6),
                    ('s', 7),
                    ('S', 8),
                    ('O', 9),
                    ('N', 10),
                    ('d', 11),
                    ('U', 12),
                    ('D', 13),
                    ('!', 14)
        ) AS Suffixes(Value, Rank)
        )
    -- Final selection with growth calculations
    SELECT
        b.PlayerName,
        b.UpdatedFormatted as DateTime,
        b.EarningsBonusPercentage as EBP,
        -- Cast the numeric part to decimal
        CAST(LEFT(b.CleanValue, CASE 
            WHEN CHARINDEX(b.Suffix, b.CleanValue) > 0 
            THEN CHARINDEX(b.Suffix, b.CleanValue) - 1 
            ELSE LEN(b.CleanValue) 
        END) AS DECIMAL(38,10)) as EB,
        b.Suffix,
        -- Calculate relative position in the progression
        CASE 
            WHEN b.Suffix = f.Suffix THEN -- Same suffix, can compare directly
                FORMAT(
                    (CAST(LEFT(b.CleanValue, CASE 
                        WHEN CHARINDEX(b.Suffix, b.CleanValue) > 0 
                        THEN CHARINDEX(b.Suffix, b.CleanValue) - 1 
                        ELSE LEN(b.CleanValue) 
                    END) AS DECIMAL(38,10)) / 
                     CAST(LEFT(f.CleanValue, CASE 
                        WHEN CHARINDEX(f.Suffix, f.CleanValue) > 0 
                        THEN CHARINDEX(f.Suffix, f.CleanValue) - 1 
                        ELSE LEN(f.CleanValue) 
                    END) AS DECIMAL(38,10)) - 1) * 100,
                    'N2'
                ) + '%'
            WHEN (SELECT Rank
        FROM SuffixRanks
        WHERE Value = b.Suffix) > 
                 (SELECT Rank
        FROM SuffixRanks
        WHERE Value = f.Suffix) THEN 
                '>1000%'
            ELSE 
                '<0%'
        END as RelativeGrowth
    FROM BaseData b
        LEFT JOIN FirstRecords f ON b.PlayerName = f.PlayerName AND f.RowNum = 1
    ORDER BY b.PlayerName, b.Updated ASC;
-- Changed from DESC to ASC
END
GO
