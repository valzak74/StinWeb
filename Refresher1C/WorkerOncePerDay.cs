using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refresher1C.Service;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C
{
    public class WorkerOncePerDay : TimedWorker 
    {
        private TimeSpan _executeTime;
        public WorkerOncePerDay(IServiceScopeFactory serviceScopeFactory, IConfiguration config)
            :base(serviceScopeFactory)
        {
            if (!TimeSpan.TryParseExact(config["OncePerDay:executeTime"], @"hh\:mm", null, out _executeTime))
                _executeTime = TimeSpan.Zero;
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
            if (_executeTime < currentTime)
            {
                dayStart = DateTime.Today.AddDays(1);
            }
            return dayStart.AddTicks(_executeTime.Ticks) - currentDateTime;
        }
        public override void ExecuteTask(object state)
        {
            _timer?.Change(Timeout.Infinite, 0);
            _executingTask = ExecuteTaskAsync(_stoppingCts.Token);
            _timer?.Change(GetNextStartDelay(), TimeSpan.FromMilliseconds(-1));
        }

        public override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
        {
            await CheckMarketplaceData(stoppingToken);
        }
        private async Task CheckMarketplaceData(CancellationToken stoppingToken)
        {
            try
            {
                using IMarketplaceService MarketplaceScope = _scope.CreateScope()
                       .ServiceProvider.GetService<IMarketplaceService>();
                await MarketplaceScope.CheckPickupExpired(stoppingToken);
                await MarketplaceScope.UpdateTariffs(stoppingToken);
            }
            catch
            {

            }
        }
    }
}
