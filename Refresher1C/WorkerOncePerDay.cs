using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Refresher1C.Service;
using StinClasses.Справочники.Functions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C
{
    public class WorkerOncePerDay : BackgroundService
    {
        private IServiceScopeFactory _scopeFactory;
        private TimeSpan _executeTime;
        public WorkerOncePerDay(IServiceScopeFactory serviceScopeFactory, IConfiguration config)
        {
            _scopeFactory = serviceScopeFactory;
            if (!TimeSpan.TryParseExact(config["OncePerDay:executeTime"], @"hh\:mm", null, out _executeTime))
                _executeTime = TimeSpan.Zero;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(GetNextStartDelay(), stoppingToken);
                await CheckMarketplaceData(stoppingToken);
            }
        }
        //public override Task StartAsync(CancellationToken stoppingToken)
        //{
        //    _timer = new Timer(ExecuteTask, null, GetNextStartDelay(), TimeSpan.FromMilliseconds(-1));

        //    return Task.CompletedTask;
        //}
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
        //public override void ExecuteTask(object state)
        //{
        //    _timer?.Change(Timeout.Infinite, 0);
        //    if (_executingTask?.Status != TaskStatus.Running)
        //        _executingTask = ExecuteTaskAsync(_stoppingCts.Token);
        //    _timer?.Change(GetNextStartDelay(), TimeSpan.FromMilliseconds(-1));
        //}

        //public override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
        //{
        //    await CheckMarketplaceData(stoppingToken);
        //}
        private async Task CheckMarketplaceData(CancellationToken stoppingToken)
        {
            try
            {
                Task pickupExpired = Task.Run(async () =>
                {
                    using IMarketplaceService MarketplaceScope = _scopeFactory.CreateScope()
                           .ServiceProvider.GetService<IMarketplaceService>();
                    await MarketplaceScope.CheckPickupExpired(stoppingToken);
                });
                Task tariffs = Task.Run(async () =>
                {
                    using IMarketplaceService MarketplaceScope = _scopeFactory.CreateScope()
                           .ServiceProvider.GetService<IMarketplaceService>();
                    await MarketplaceScope.UpdateTariffs(stoppingToken);
                });
                Task checkComissStack = Task.Run(async () =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    var marketplaceFunctions = scope.ServiceProvider.GetRequiredService<IMarketplaceFunctions>();
                    await marketplaceFunctions.CheckOrdersStackInComission(stoppingToken);
                });
                await Task.WhenAll(
                    pickupExpired,
                    tariffs,
                    checkComissStack
                    );
            }
            catch
            {

            }
        }
    }
}
