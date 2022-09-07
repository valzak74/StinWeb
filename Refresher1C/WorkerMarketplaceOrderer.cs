using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Refresher1C.Service;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace Refresher1C
{
    public class WorkerMarketplaceOrderer : BackgroundService
    {
        //private Timer _timerSlow;
        //private Task _executingSlowTask;
        //private TimeSpan _dueTimeSlow;
        private IServiceScopeFactory _scopeFactory;
        private TimeSpan _delay;
        public WorkerMarketplaceOrderer(IServiceScopeFactory serviceScopeFactory, IConfiguration config)
        {
            _scopeFactory = serviceScopeFactory;
            int.TryParse(config["Orderer:refreshIntervalMilliSec"], out int refreshInterval);
            refreshInterval = Math.Max(refreshInterval, 100);
            _delay = TimeSpan.FromMilliseconds(refreshInterval);
            //if (int.TryParse(config["Orderer:refreshIntervalMilliSec"], out int _refreshInterval))
            //    _refreshInterval = Math.Max(_refreshInterval, 1);
            //else
            //    _refreshInterval = 1000; //1 sec
            //_dueTime = TimeSpan.FromMilliseconds(_refreshInterval);
            //int.TryParse(config["Orderer:refreshSlowIntervalSec"], out int refreshSlowInterval);
            //refreshSlowInterval = Math.Max(refreshSlowInterval, 1);
            //_dueTimeSlow = TimeSpan.FromSeconds(refreshSlowInterval);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckUnfulfilledOrders(stoppingToken);
                await Task.Delay(_delay, stoppingToken);
            }
        }
        //public override Task StartAsync(CancellationToken stoppingToken)
        //{
        //    _timer = new Timer(ExecuteTask, true, TimeSpan.Zero, TimeSpan.FromMilliseconds(-1));
        //    _timerSlow = new Timer(ExecuteTask, false, TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(-1));

        //    return Task.CompletedTask;
        //}
        //public override async Task StopAsync(CancellationToken cancellationToken)
        //{

        //    _timer?.Change(Timeout.Infinite, 0);
        //    _timerSlow?.Change(Timeout.Infinite, 0);

        //    // Stop called without start
        //    if ((_executingTask == null) && (_executingSlowTask == null))
        //    {
        //        return;
        //    }

        //    try
        //    {
        //        // Signal cancellation to the executing method
        //        _stoppingCts.Cancel();
        //    }
        //    finally
        //    {
        //        var taskPool = new List<Task>();
        //        if (_executingTask != null)
        //            taskPool.Add(_executingTask);
        //        if (_executingSlowTask != null)
        //            taskPool.Add(_executingSlowTask);
        //        taskPool.Add(Task.Delay(Timeout.Infinite, cancellationToken));
        //        // Wait until the task completes or the stop token triggers
        //        await Task.WhenAny(taskPool);
        //    }
        //}
        //public override void Dispose()
        //{
        //    base.Dispose();
        //    _timerSlow?.Dispose();
        //}
        //public override void ExecuteTask(object state)
        //{
        //    if ((bool)state)
        //    {
        //        _timer?.Change(Timeout.Infinite, 0);
        //        if (_executingTask?.Status != TaskStatus.Running)
        //            _executingTask = ExecuteTaskAsync(_stoppingCts.Token);
        //        _timer?.Change(_dueTime, TimeSpan.FromMilliseconds(-1));
        //    }
        //    else
        //    {
        //        _timerSlow?.Change(Timeout.Infinite, 0);
        //        if (_executingSlowTask?.Status != TaskStatus.Running)
        //            _executingSlowTask = CheckSlowOrders(_stoppingCts.Token);
        //        _timerSlow?.Change(_dueTimeSlow, TimeSpan.FromMilliseconds(-1));
        //    }
        //}
        //private async Task CheckSlowOrders(CancellationToken stoppingToken)
        //{
        //    try
        //    {
        //        using IMarketplaceService MarketplaceScope = _scope.CreateScope()
        //               .ServiceProvider.GetService<IMarketplaceService>();
        //        await MarketplaceScope.RefreshSlowOrders(stoppingToken);
        //    }
        //    catch
        //    {

        //    }
        //}
        //public override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
        //{
        //    await CheckUnfulfilledOrders(stoppingToken);
        //}
        private async Task CheckUnfulfilledOrders(CancellationToken stoppingToken)
        {
            try
            {
                using IMarketplaceService MarketplaceScope = _scopeFactory.CreateScope()
                       .ServiceProvider.GetService<IMarketplaceService>();
                await MarketplaceScope.RefreshOrders(stoppingToken);
            }
            catch
            {

            }
        }
    }
}
