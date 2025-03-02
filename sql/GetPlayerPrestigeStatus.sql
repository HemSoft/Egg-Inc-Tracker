SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GetPlayerPrestigeStatus]
    @SinceDate DATETIME2(7) = NULL,
    @PrestigeCountGoal INT = 38
AS
BEGIN
    SET NOCOUNT ON;

    IF @SinceDate IS NULL
    BEGIN
        SET @SinceDate = CONVERT(DATETIME2(7), CONVERT(DATE,
            SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'Central Standard Time'));
    END

    SELECT
        PlayerName,
        COUNT(*) AS PrestigeCount,
        @PrestigeCountGoal - COUNT(*) AS Needed,
        MAX(Updated) AS LastUpdatedTime
    FROM
        [dbo].[Players]
    WHERE 
        Updated >= @SinceDate
    GROUP BY 
        PlayerName
    ORDER BY 
        PlayerName;
END
GO
