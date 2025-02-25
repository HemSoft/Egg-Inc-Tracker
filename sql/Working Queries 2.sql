-- SE Goals this week:
-- 11/02 -- 5.7s 10/25
-- MER: 36.84 / 81.97
-- MER: 37.72 / 83.06
GetRankedPlayerRecords @PlayerName = NULL, @RecordLimit = 30, @SampleDaysBack = 30, @SEGoal = '9.16s', @EBGoal = '38.69N', @MERGoal = 38.168, @JERGoal = 84.93
exec dbo.GetEBHistoryForExcel @PlayerName = 'King Friday!', @Days = 7

-- Get daily player progress 
GetPlayerProgress @Days = 14
GetPlayerPrestigeStatus NULL, 15
GetPlayerMaxMetrics

-- MAX(MER) = 36.07
-- MAX(JER) = 85.64
