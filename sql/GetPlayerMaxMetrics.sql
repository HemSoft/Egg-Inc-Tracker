SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GetPlayerMaxMetrics]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        PlayerName AS [Player Name],
        MAX(MER) AS [MER],
        MAX(JER) AS [JER],
        MAX(PlayerLegendaries) AS Legendaries,
        MAX(CraftingLevel) AS CraftingLevel,
        MAX(EarningsBonusPerHour) AS EBHour
    FROM
        [dbo].[Players]
    GROUP BY
        PlayerName;
END
GO
