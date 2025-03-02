SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GetPlayerProgress]
    @Days INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Calculate the daily start date (in CST)
    DECLARE @DailyStartDate DATE = DATEADD(DAY, -@Days, 
        CONVERT(DATE, SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'Central Standard Time'));

    -- Calculate the weekly start date (Monday) corresponding to @DailyStartDate.
    DECLARE @WeeklyStartDate DATE = DATEADD(DAY, -((DATEPART(WEEKDAY, @DailyStartDate) + 5) % 7), @DailyStartDate);

    --------------------------------------------------------------------------------
    -- Daily Progress CTE Chain
    --------------------------------------------------------------------------------
    ;WITH
        DailyData
        AS
        (
            SELECT
                PlayerName,
                EarningsBonusPercentage,
                SoulEggs,
                SoulEggsFull,
                ProphecyEggs,
                MER,
                JER,
                Prestiges,
                Updated,
                -- Convert Updated to CST date for grouping by day
                CAST(Updated AT TIME ZONE 'UTC' AT TIME ZONE 'Central Standard Time' AS DATE) AS UpdateDate
            FROM [dbo].[Players]
            WHERE Updated >= @DailyStartDate
        ),
        DailyStats
        AS
        (
            SELECT
                PlayerName,
                UpdateDate,
                -- Get the first and last SoulEggsFull values of the day
                FIRST_VALUE(TRY_CAST(REPLACE(SoulEggsFull, ',', '') AS DECIMAL(38, 0)))
                OVER (PARTITION BY PlayerName, UpdateDate ORDER BY Updated) AS StartingSE,
                FIRST_VALUE(TRY_CAST(REPLACE(SoulEggsFull, ',', '') AS DECIMAL(38, 0)))
                OVER (PARTITION BY PlayerName, UpdateDate ORDER BY Updated DESC) AS EndingSE,
                -- Get the latest values of other columns for the day
                FIRST_VALUE(EarningsBonusPercentage)
                OVER (PARTITION BY PlayerName, UpdateDate ORDER BY Updated DESC) AS EB,
                FIRST_VALUE(SoulEggs)
                OVER (PARTITION BY PlayerName, UpdateDate ORDER BY Updated DESC) AS SE,
                FIRST_VALUE(ProphecyEggs)
                OVER (PARTITION BY PlayerName, UpdateDate ORDER BY Updated DESC) AS PE,
                FIRST_VALUE(MER)
                OVER (PARTITION BY PlayerName, UpdateDate ORDER BY Updated DESC) AS MER,
                FIRST_VALUE(JER)
                OVER (PARTITION BY PlayerName, UpdateDate ORDER BY Updated DESC) AS JER,
                -- Get the final (latest) prestige count for the day
                FIRST_VALUE(Prestiges)
                OVER (PARTITION BY PlayerName, UpdateDate ORDER BY Updated DESC) AS LatestPrestiges,
                FIRST_VALUE(Updated)
                OVER (PARTITION BY PlayerName, UpdateDate ORDER BY Updated DESC) AS LastUpdate
            FROM DailyData
        ),
        RankedDays
        AS
        (
            SELECT
                *,
                ROW_NUMBER() OVER (PARTITION BY PlayerName, UpdateDate ORDER BY LastUpdate DESC) AS RowNum
            FROM DailyStats
        ),
        DailyFinalStats
        AS
        (
            SELECT
                PlayerName,
                UpdateDate,
                EB,
                SE,
                PE,
                MER,
                JER,
                LatestPrestiges,
                LastUpdate,
                StartingSE,
                EndingSE,
                -- Use LAG to compute the daily prestige gain (difference from previous day)
                LAG(LatestPrestiges) OVER (PARTITION BY PlayerName ORDER BY UpdateDate) AS PrevDailyPrestiges
            FROM RankedDays
            WHERE RowNum = 1
        ),

        --------------------------------------------------------------------------------
        -- Weekly Progress CTE Chain
        --------------------------------------------------------------------------------
        WeeklyData
        AS
        (
            SELECT
                PlayerName,
                EarningsBonusPercentage,
                SoulEggs,
                SoulEggsFull,
                ProphecyEggs,
                MER,
                JER,
                Prestiges,
                Updated,
                -- Compute WeekStartDate (Monday) for each record in CST
                DATEADD(DAY, -((DATEPART(WEEKDAY, 
                        CAST(Updated AT TIME ZONE 'UTC' AT TIME ZONE 'Central Standard Time' AS DATE)
                     ) + 5) % 7),
                CAST(Updated AT TIME ZONE 'UTC' AT TIME ZONE 'Central Standard Time' AS DATE)
            ) AS WeekStartDate
            FROM [dbo].[Players]
            WHERE Updated >= @WeeklyStartDate
        ),
        WeeklyStats
        AS
        (
            SELECT
                PlayerName,
                WeekStartDate,
                FIRST_VALUE(TRY_CAST(REPLACE(SoulEggsFull, ',', '') AS DECIMAL(38, 0)))
                OVER (PARTITION BY PlayerName, WeekStartDate ORDER BY Updated) AS StartingSE,
                FIRST_VALUE(TRY_CAST(REPLACE(SoulEggsFull, ',', '') AS DECIMAL(38, 0)))
                OVER (PARTITION BY PlayerName, WeekStartDate ORDER BY Updated DESC) AS EndingSE,
                FIRST_VALUE(EarningsBonusPercentage)
                OVER (PARTITION BY PlayerName, WeekStartDate ORDER BY Updated DESC) AS EB,
                FIRST_VALUE(SoulEggs)
                OVER (PARTITION BY PlayerName, WeekStartDate ORDER BY Updated DESC) AS SE,
                FIRST_VALUE(ProphecyEggs)
                OVER (PARTITION BY PlayerName, WeekStartDate ORDER BY Updated DESC) AS PE,
                FIRST_VALUE(MER)
                OVER (PARTITION BY PlayerName, WeekStartDate ORDER BY Updated DESC) AS MER,
                FIRST_VALUE(JER)
                OVER (PARTITION BY PlayerName, WeekStartDate ORDER BY Updated DESC) AS JER,
                -- Get the final (latest) prestige count for the week
                FIRST_VALUE(Prestiges)
                OVER (PARTITION BY PlayerName, WeekStartDate ORDER BY Updated DESC) AS LatestWeeklyPrestiges,
                FIRST_VALUE(Updated)
                OVER (PARTITION BY PlayerName, WeekStartDate ORDER BY Updated DESC) AS LastUpdate
            FROM WeeklyData
        ),
        RankedWeeks
        AS
        (
            SELECT
                *,
                ROW_NUMBER() OVER (PARTITION BY PlayerName, WeekStartDate ORDER BY LastUpdate DESC) AS RowNum
            FROM WeeklyStats
        ),
        WeeklyFinalStats
        AS
        (
            SELECT
                PlayerName,
                WeekStartDate,
                EB,
                SE,
                PE,
                MER,
                JER,
                LatestWeeklyPrestiges,
                LastUpdate,
                StartingSE,
                EndingSE,
                -- Use LAG to compute the weekly prestige gain (difference from previous week)
                LAG(LatestWeeklyPrestiges) OVER (PARTITION BY PlayerName ORDER BY WeekStartDate) AS PrevWeeklyPrestiges
            FROM RankedWeeks
            WHERE RowNum = 1
        )

    --------------------------------------------------------------------------------
    -- Combine Daily and Weekly Results into a Single Result Set
    --------------------------------------------------------------------------------
    SELECT
        d.PlayerName,
        d.EB,
        d.SE,
        d.PE,
        d.MER,
        d.JER,
        d.LatestPrestiges AS Prestiges,
        FORMAT(d.LastUpdate AT TIME ZONE 'UTC' AT TIME ZONE 'Central Standard Time', 'yyyy-MM-dd HH:mm') AS Updated,
        d.UpdateDate,
        -- Daily calculated columns
        CASE 
            WHEN d.PrevDailyPrestiges IS NOT NULL THEN d.LatestPrestiges - d.PrevDailyPrestiges 
            ELSE NULL 
        END AS DPG,
        CASE 
            WHEN d.EndingSE >= d.StartingSE 
                 THEN dbo.FormatBigInteger(CAST(d.EndingSE - d.StartingSE AS VARCHAR(50))) 
            ELSE NULL 
        END AS DSEG,
        -- Weekly calculated columns (joined by matching the daily row's week)
        w.WeekStartDate,
        CASE 
            WHEN w.PrevWeeklyPrestiges IS NOT NULL THEN w.LatestWeeklyPrestiges - w.PrevWeeklyPrestiges 
            ELSE NULL 
        END AS WPG,
        CASE 
            WHEN w.EndingSE >= w.StartingSE 
                 THEN dbo.FormatBigInteger(CAST(w.EndingSE - w.StartingSE AS VARCHAR(50))) 
            ELSE NULL 
        END AS WSEG
    FROM DailyFinalStats d
        LEFT JOIN WeeklyFinalStats w
        ON d.PlayerName = w.PlayerName
            -- Match the daily row to its week (compute the Monday of the daily UpdateDate)
            AND DATEADD(DAY, -((DATEPART(WEEKDAY, d.UpdateDate) + 5) % 7), d.UpdateDate) = w.WeekStartDate
    ORDER BY d.PlayerName, d.UpdateDate DESC;
END
GO
