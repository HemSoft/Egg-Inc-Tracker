namespace HemSoft.EggIncTracker.Domain;

using System.Linq;

using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Data.Dtos;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public static class GoalManager
{
    public static async Task<bool> SaveGoalAsync(GoalDto goal, ILogger? logger)
    {
        try
        {
            var context = new EggIncContext();

            var existingGoal = await context.Goals
                .FirstOrDefaultAsync(x => x.Id == goal.Id);

            if (existingGoal != null)
            {
                // Update specific properties
                existingGoal.PlayerName = goal.PlayerName;
                existingGoal.SEGoal = goal.SEGoal;
                existingGoal.EBGoal = goal.EBGoal;
                existingGoal.MERGoal = goal.MERGoal;
                existingGoal.JERGoal = goal.JERGoal;
                existingGoal.WeeklySEGainGoal = goal.WeeklySEGainGoal;
                logger?.LogInformation("Updating existing goal...");
            }
            else
            {
                await context.Goals.AddAsync(goal);
                logger?.LogInformation("Adding new goal...");
            }

            await context.SaveChangesAsync();
            logger?.LogInformation("Done.");
            return true;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to save goal");
            return false;
        }
    }

    public static async Task<GoalDto?> GetPlayerGoalAsync(string playerName, ILogger? logger)
    {
        try
        {
            var context = new EggIncContext();

            var goal = await context.Goals
                .FirstOrDefaultAsync(x => x.PlayerName == playerName);

            return goal;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, $"Failed to retrieve goal for player {playerName}");
            return null;
        }
    }
}
