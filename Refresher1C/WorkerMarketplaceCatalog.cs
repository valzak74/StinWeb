using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refresher1C.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C
{
    public class WorkerMarketplaceCatalog : TimedWorker 
    {
        private List<TimeSpan> _executeTime;
        public WorkerMarketplaceCatalog(IServiceScopeFactory serviceScopeFactory, IConfiguration config)
            :base(serviceScopeFactory)
        {
            var executeTime = config["Catalog:executeTime"].Split(';');
            _executeTime = new List<TimeSpan>();
            foreach (var timeSpan in executeTime)
            {
               if (TimeSpan.TryParseExact(timeSpan, @"hh\:mm", null, out TimeSpan parsedTimeSpan))
                    _executeTime.Add(parsedTimeSpan);
            }
        }
        public override Task StartAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(ExecuteTask, null, GetNextStartDelay(), TimeSpan.FromMilliseconds(-1));

            return Task.CompletedTask;
        }
        private TimeSpan GetNextStartDelay()
        {
            var currentDateTime = DateTime.Now;
            var currentTime = currentDateTime.TimeOfDay;
            var dayStart = DateTime.Today;
            var timeStart = _executeTime
                .Where(x => x > currentTime)
                .OrderBy(x => x)
                .FirstOrDefault();
            if (timeStart == TimeSpan.Zero)
            {
                dayStart = DateTime.Today.AddDays(1);
                timeStart = _executeTime
                    .OrderBy(x => x)
                    .FirstOrDefault();
            }
            return dayStart.AddTicks(timeStart.Ticks) - currentDateTime;
        }
        public override void ExecuteTask(object state)
        {
            _timer?.Change(Timeout.Infinite, 0);
            _executingTask = ExecuteTaskAsync(_stoppingCts.Token);
            _timer?.Change(GetNextStartDelay(), TimeSpan.FromMilliseconds(-1));
        }

        public override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
        {
            await CheckMarketplaceCatalog(stoppingToken);
        }
        private async Task CheckMarketplaceCatalog(CancellationToken stoppingToken)
        {
            try
            {
                using IMarketplaceService MarketplaceScope = _scope.CreateScope()
                       .ServiceProvider.GetService<IMarketplaceService>();
                await MarketplaceScope.CheckCatalog(stoppingToken);
            }
            catch
            {

            }
        }
    }
}
