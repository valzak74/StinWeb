using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Refresher1C.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C
{
    public class WorkerReturns : BackgroundService
    {
        private IServiceScopeFactory _scopeFactory;
        private List<TimeSpan> _executeTime;
        public WorkerReturns(IServiceScopeFactory serviceScopeFactory, IConfiguration config)
        {
            _scopeFactory = serviceScopeFactory;
            var executeTime = config["Returns:executeTime"].Split(';');
            _executeTime = new List<TimeSpan>();
            foreach (var timeSpan in executeTime)
            {
                if (TimeSpan.TryParseExact(timeSpan, @"hh\:mm", null, out TimeSpan parsedTimeSpan))
                    _executeTime.Add(parsedTimeSpan);
            }
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(GetNextStartDelay(), stoppingToken);
                await CheckReturns(stoppingToken);
            }
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
        private async Task CheckReturns(CancellationToken stoppingToken)
        {
            try
            {
                using IMarketplaceService MarketplaceScope = _scopeFactory.CreateScope()
                       .ServiceProvider.GetService<IMarketplaceService>();
                await MarketplaceScope.CheckReturns(stoppingToken);
            }
            catch
            {

            }
        }
    }
}
