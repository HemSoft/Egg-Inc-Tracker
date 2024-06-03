using System;
using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Domain;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace UpdatePlayerFunction
{
    public class UpdatePlayerTimerTrigger
    {
        private readonly ILogger _logger;

        public UpdatePlayerTimerTrigger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UpdatePlayerTimerTrigger>();
        }

        [Function("UpdatePlayerTimerTrigger")]
        public void Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            using var context = new EggIncContext();
            context.Database.EnsureCreated();

            var player = Api.CallApi("EI6335140328505344", "King Friday!", "https://eggincdatacollection.azurewebsites.net/api/formulae/all?eid=EI6335140328505344").Result;
            PlayerManager.SavePlayer(player, _logger);
            var player2 = Api.CallApi("EI5435770400276480", "King Saturday!", "https://eggincdatacollection.azurewebsites.net/api/formulae/all?eid=EI5435770400276480").Result;
            PlayerManager.SavePlayer(player2, _logger);
            var player3 = Api.CallApi("EI6306349753958400", "King Sunday!", "https://eggincdatacollection.azurewebsites.net/api/formulae/all?eid=EI6306349753958400").Result;
            PlayerManager.SavePlayer(player3, _logger);

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}
