namespace Domain.src;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using HemSoft.EggIncTracker.Data.Dtos;
using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Domain;

using Microsoft.Extensions.Logging;

public static class ProjectionCalculator
{
    private const int DEFAULT_SAMPLE_DAYS = 30;
    private const int MIN_DATA_POINTS = 3;
    private const double RECENT_DATA_WEIGHT = 2.0; // Recent data points count more
    private const double MIN_R2_THRESHOLD = 0.75; // Minimum R² value for exponential fit
    private const double MAX_GROWTH_RATE = 1.01; // Maximum 1% growth per hour
    private const double MIN_GROWTH_RATE = 1.0001; // Minimum 0.01% growth per hour

    private class EarningsBonusDataPoint
    {
        public DateTime Updated { get; set; }
        public BigInteger EB { get; set; }
        public double Weight { get; set; }
    }

    private class RegressionPoint
    {
        public double Hours { get; set; }
        public double LogEB { get; set; }
        public double Weight { get; set; }
    }

    public static DateTime CalculateProjectedTitleChange(PlayerDto player, ILogger? logger = null)
    {
        try
        {
            var context = new EggIncContext();

            // Get historical data points for the last 30 days with null checking
            var historicalData = context.Players
                .Where(p => p.PlayerName == player.PlayerName &&
                       p.Updated >= DateTime.UtcNow.AddDays(-DEFAULT_SAMPLE_DAYS))
                .OrderBy(p => p.Updated)
                .AsEnumerable()  // Moves the calculation to memory to avoid SQL null issues
                .Select(p =>
                {
                    try
                    {
                        return new EarningsBonusDataPoint
                        {
                            Updated = p.Updated,
                            EB = PlayerManager.CalculateEarningsBonusPercentageNumber(p),
                            Weight = 1.0
                        };
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning($"Skipping data point for {p.PlayerName} at {p.Updated}: {ex.Message}");
                        return null;
                    }
                })
                .Where(p => p != null)  // Filter out any failed calculations
                .ToList();

            if (historicalData.Count < MIN_DATA_POINTS)
            {
                logger?.LogInformation($"Insufficient data points for {player.PlayerName}. Need at least {MIN_DATA_POINTS} data points.");
                return DateTime.MinValue;
            }

            // Apply recency weighting
            ApplyRecencyWeights(historicalData);

            // Safely calculate growth rates with null checking
            var (expGrowthRate, expR2) = CalculateExponentialGrowthRate(historicalData);
            var (linearGrowthRate, linearR2) = CalculateLinearGrowthRate(historicalData);

            // Choose the better model based on R² values and sanity checks
            double selectedGrowthRate;
            string modelType;

            if (expR2 > linearR2 && expR2 >= MIN_R2_THRESHOLD && expGrowthRate <= MAX_GROWTH_RATE)
            {
                selectedGrowthRate = expGrowthRate;
                modelType = "exponential";
            }
            else
            {
                // Safely calculate current EB for linear rate conversion
                var currentEBValue = PlayerManager.CalculateEarningsBonusPercentageNumber(player);
                if (currentEBValue == 0)
                {
                    logger?.LogWarning($"Invalid current EB value for {player.PlayerName}");
                    return DateTime.MinValue;
                }

                selectedGrowthRate = Math.Max(MIN_GROWTH_RATE,
                    Math.Min(MAX_GROWTH_RATE, 1 + (linearGrowthRate / (double)currentEBValue)));
                modelType = "linear";
            }

            // Calculate current EB and target EB with null checking
            var currentEB = PlayerManager.CalculateEarningsBonusPercentageNumber(player);
            if (currentEB == 0)
            {
                logger?.LogWarning($"Invalid current EB value for {player.PlayerName}");
                return DateTime.MinValue;
            }

            var targetEB = GetNextTitleThreshold(currentEB);
            if (targetEB <= currentEB)
            {
                logger?.LogInformation($"Player {player.PlayerName} has already reached target EB");
                return DateTime.MinValue;
            }

            // Calculate historical average daily progress
            var dailyProgress = CalculateAverageDailyProgress(historicalData);
            if (dailyProgress == 0)
            {
                logger?.LogWarning($"Invalid daily progress calculation for {player.PlayerName}");
                return DateTime.MinValue;
            }

            // Calculate time needed using selected growth model
            double hoursNeeded;
            try
            {
                if (modelType == "exponential")
                {
                    hoursNeeded = Math.Log((double)targetEB / (double)currentEB) / Math.Log(selectedGrowthRate);
                }
                else
                {
                    var progressPerHour = (double)dailyProgress / 24.0;
                    var ebDifference = (double)(targetEB - currentEB);
                    var linearTimeEstimate = ebDifference / progressPerHour;

                    var logRatio = Math.Log((double)targetEB / (double)currentEB);
                    hoursNeeded = Math.Min(linearTimeEstimate, logRatio / Math.Log(MIN_GROWTH_RATE));
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error calculating hours needed for {player.PlayerName}: {ex.Message}");
                return DateTime.MinValue;
            }

            // Add safety margin based on R² value
            var r2 = modelType == "exponential" ? expR2 : linearR2;
            var safetyMargin = 1 + (1 - r2); // Poor fit adds up to 100% more time
            hoursNeeded *= safetyMargin;

            // Log projection details
            logger?.LogInformation($"Projection details for {player.PlayerName}:");
            logger?.LogInformation($"Current EB: {FormatBigInteger(currentEB)}");
            logger?.LogInformation($"Target EB: {FormatBigInteger(targetEB)}");
            logger?.LogInformation($"Model type: {modelType}");
            logger?.LogInformation($"Growth Rate: {selectedGrowthRate:F6} per hour");
            logger?.LogInformation($"R² value: {r2:F4}");
            logger?.LogInformation($"Safety margin: {safetyMargin:F2}x");
            logger?.LogInformation($"Daily progress: {FormatBigInteger(dailyProgress)}/day");
            logger?.LogInformation($"Estimated hours needed: {hoursNeeded:F1}");

            return DateTime.Now.AddHours(hoursNeeded);
        }
        catch (Exception ex)
        {
            logger?.LogError($"Error in CalculateProjectedTitleChange for {player.PlayerName}: {ex.Message}");
            return DateTime.MinValue;
        }
    }

    private static void ApplyRecencyWeights(List<EarningsBonusDataPoint> data)
    {
        var totalDuration = (data.Last().Updated - data.First().Updated).TotalHours;
        if (totalDuration <= 0) return;

        foreach (var point in data)
        {
            var hoursFromStart = (point.Updated - data.First().Updated).TotalHours;
            var recencyFactor = hoursFromStart / totalDuration;
            point.Weight = 1.0 + (RECENT_DATA_WEIGHT - 1.0) * recencyFactor;
        }
    }

    private static (double GrowthRate, double R2) CalculateExponentialGrowthRate(
        List<EarningsBonusDataPoint> historicalData)
    {
        var baseTime = historicalData.First().Updated;
        var points = historicalData.Select(d => new RegressionPoint
        {
            Hours = (d.Updated - baseTime).TotalHours,
            LogEB = Math.Log((double)d.EB),
            Weight = d.Weight
        }).ToList();

        return CalculateWeightedRegression(points);
    }

    private static (double Rate, double R2) CalculateLinearGrowthRate(
        List<EarningsBonusDataPoint> historicalData)
    {
        var baseTime = historicalData.First().Updated;
        var points = historicalData.Select(d => new RegressionPoint
        {
            Hours = (d.Updated - baseTime).TotalHours,
            LogEB = (double)d.EB,
            Weight = d.Weight
        }).ToList();

        return CalculateWeightedRegression(points);
    }

    private static (double Rate, double R2) CalculateWeightedRegression(
        List<RegressionPoint> points)
    {
        double sumW = 0, sumWX = 0, sumWY = 0, sumWXY = 0, sumWXX = 0, sumWYY = 0;

        foreach (var point in points)
        {
            sumW += point.Weight;
            sumWX += point.Weight * point.Hours;
            sumWY += point.Weight * point.LogEB;
            sumWXY += point.Weight * point.Hours * point.LogEB;
            sumWXX += point.Weight * point.Hours * point.Hours;
            sumWYY += point.Weight * point.LogEB * point.LogEB;
        }

        var slope = (sumW * sumWXY - sumWX * sumWY) / (sumW * sumWXX - sumWX * sumWX);
        var intercept = (sumWY - slope * sumWX) / sumW;

        var rSquared = Math.Pow(sumW * sumWXY - sumWX * sumWY, 2) /
            ((sumW * sumWXX - sumWX * sumWX) * (sumW * sumWYY - sumWY * sumWY));

        return (Math.Exp(slope), rSquared);
    }

    private static BigInteger CalculateAverageDailyProgress(List<EarningsBonusDataPoint> data)
    {
        var daysBetween = (data.Last().Updated - data.First().Updated).TotalDays;
        if (daysBetween <= 0) return 0;

        var totalProgress = data.Last().EB - data.First().EB;
        return totalProgress / (BigInteger)Math.Ceiling(daysBetween);
    }

    private static BigInteger GetNextTitleThreshold(BigInteger currentEB)
    {
        foreach (var (limit, _) in PlayerManager.Titles)
        {
            if (currentEB < limit)
            {
                return limit;
            }
        }
        return BigInteger.Zero;
    }

    private static string FormatBigInteger(BigInteger value)
    {
        return value.ToString("N0");
    }
}