-- SELECT TOP (100) [Id]
--       ,[PlayerName]
--       ,[Title]
--       ,FORMAT([TitleProgress], 'N2') AS Progress
--       ,[NextTitle]
--       ,FORMAT(ProjectedTitleChange AT TIME ZONE 'UTC' AT TIME ZONE 'Central Standard Time', 'yyyy-MM-dd HH:mm') AS Projected
--       ,FORMAT(Updated AT TIME ZONE 'UTC' AT TIME ZONE 'Central Standard Time', 'yyyy-MM-dd HH:mm') AS Updated
--       ,[SoulEggs]
--       ,[ProphecyEggs] AS PE
--       ,[EarningsBonusPercentage] AS EarnBonus
--       ,[MER]
--       ,[JER]
--       ,[LLC]
--   FROM [dbo].[Players]
--   --WHERE PlayerName = 'King Friday!'
--   ORDER BY Updated DESC
-- --ORDER BY PlayerName,MER DESC
-- -- JER Max = 76.85
-- -- MER Max = 29.9

-- --DELETE FROM Players WHERE Id IN (46,16,47,17)



WITH RankedRecords AS (
    SELECT 
        [Id],
        [PlayerName],
        [Title],
        FORMAT([TitleProgress], 'N2') AS Progress,
        [NextTitle],
        FORMAT(ProjectedTitleChange AT TIME ZONE 'UTC' AT TIME ZONE 'Central Standard Time', 'yyyy-MM-dd HH:mm') AS Projected,
        FORMAT(Updated AT TIME ZONE 'UTC' AT TIME ZONE 'Central Standard Time', 'yyyy-MM-dd HH:mm') AS Updated,
        [SoulEggs],
        [ProphecyEggs] AS PE,
        [EarningsBonusPercentage] AS EarnBonus,
        [EarningsBonusPerHour] AS EBHour,
        [MER],
        [JER],
        [LLC],
        ROW_NUMBER() OVER (PARTITION BY [PlayerName] ORDER BY [Updated] DESC) AS rn
    FROM [dbo].[Players]
)
SELECT *
FROM RankedRecords
WHERE rn <= 50
ORDER BY [PlayerName], rn;

--SELECT PlayerName, MAX(MER)AS MaxMer, MAX(JER) AS MaxJer FROM Players GROUP BY PlayerName
-- SELECT
--     p.PlayerName,
--     JER,
--     Updated
-- FROM
--     Players p
-- WHERE
--     p.PlayerName = 'King Friday!'
-- ORDER BY
--     Updated
