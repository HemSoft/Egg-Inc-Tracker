SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[CurrentContracts]
    @UnfinishedOnly BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.EndTime
      , c.Id
      , c.Name
      , c.KevId
      , c.PlayerCount AS Players
      , c.StartTime
      , c.SeasonId
      , c.MinutesPerToken AS MinPerToken
    FROM
        Contracts c
        LEFT OUTER JOIN PlayerContracts pc ON c.KevId = pc.KevId
    WHERE
        ((@UnfinishedOnly = 0 AND pc.CoopId IS NULL) OR
        (@UnfinishedOnly = 1 AND pc.CoopId IS NOT NULL))
        AND c.EndTime > GETDATE()
    ORDER BY EndTime ASC

END
GO
