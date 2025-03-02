-- 1952.3
-- 3694.5

-- MAX(MER) = 39.38
-- MAX(JER) = 85.64
-- MAX(LLC) =  2.67
GetRankedPlayerRecords @RecordLimit = 3, @SampleDaysBack = 7, @MERGoal = '39.04', @JERGoal = '83.969', @SEGoal = '27.53', @EBGoal = '1.42d'
exec dbo.GetPlayerProgress @Days = 14;
exec GetWeeklyPlayerProgress @Weeks = 25;
exec dbo.TrackSoulEggGains 'King Friday!';
exec dbo.CurrentEvents;
exec CurrentContracts;
exec GetPlayerPrestigeStatus;
SELECT p.Updated, p.SoulEggs, p.ProphecyEggs FROM Players p WHERE p.PlayerName = 'King Friday!' ORDER BY Updated DESC
SELECT * FROM Contracts
SELECT * FROM dbo.Events
SELECT * FROM PLayerContracts

SELECT DISTINCT
  p.PlayerName, c.KevId, c.PlayerCount, c.PublishDate, c.SeasonId, pc.ContractScore
FROM
  Players p
  INNER JOIN PlayerContracts pc ON p.EID = pc.EID
  INNER JOIN Contracts       c  ON pc.KevId = c.KevId
ORDER BY p.PlayerName, c.PublishDate DESC;


SELECT pc.KevId, pc.CoopId, pc.ContractScore AS CS, pc.ContractScoreChange AS CSDelta, pc.TimeAccepted, pc.CompletionTime, pc.CountedInSeason AS CIS, pc.SeasonId,
       pc.TeamworkScore, pc.ContributionRatio, pc.ChickenRunsSent, pc.TokensSent, pc.TokensReceived,
       pc.*
FROM PlayerContracts pc ORDER BY pc.TimeAccepted DESC

SELECT
    p.PlayerName
  , MAX(p.MER) AS MER
  , MAX(p.JER) AS JER
  , MAX(p.LLC) AS LLC
FROM
    Players p
GROUP BY
    p.PlayerName