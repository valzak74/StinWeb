using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Refresher1C.Service;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C
{
    public class WorkerMarketplaceStocker : BackgroundService
    {
        //private int _refreshIntervalForErrors;
        //private Timer _timerForErrors;
        //private Task _executingErrorTask;
        private IServiceScopeFactory _scopeFactory;
        private TimeSpan _delay;
        private int _errorsTaskCount;
        public WorkerMarketplaceStocker(IServiceScopeFactory serviceScopeFactory, IConfiguration config)
            //:base(serviceScopeFactory)
        {
            _scopeFactory = serviceScopeFactory;
            int.TryParse(config["Stocker:refreshIntervalMilliSec"], out int refreshInterval);
            refreshInterval = Math.Max(refreshInterval, 100);
            _delay = TimeSpan.FromMilliseconds(refreshInterval);
            int.TryParse(config["Stocker:checkErrorsEvery"], out _errorsTaskCount);
            _errorsTaskCount = Math.Max(_errorsTaskCount, 1);
            //if (int.TryParse(config["Stocker:refreshIntervalMilliSec"], out _refreshInterval))
            //    _refreshInterval = Math.Max(_refreshInterval, 1);
            //else
            //    _refreshInterval = 1000; //1 sec
            //if (int.TryParse(config["Stocker:refreshIntervalErrorsSec"], out _refreshIntervalForErrors))
            //    _refreshIntervalForErrors = Math.Max(_refreshIntervalForErrors, 1);
            //else
            //    _refreshIntervalForErrors = 60; //sec
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int taskCount = _errorsTaskCount - 1;
            while (!stoppingToken.IsCancellationRequested)
            {
                taskCount++;
                bool regular = true;
                if (taskCount == _errorsTaskCount)
                {
                    regular = false;
                    taskCount = 0;
                }
                await CheckStockAsync(regular, stoppingToken);
                await Task.Delay(_delay, stoppingToken);
            }
        }
        //public override Task StartAsync(CancellationToken stoppingToken)
        //{
        //    _timer = new Timer(ExecuteTask, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(-1));
        //    _timerForErrors = new Timer(ExecuteErrorTask, null, TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(-1));

        //    return Task.CompletedTask;
        //}
        //public override async Task StopAsync(CancellationToken cancellationToken)
        //{

        //    _timer?.Change(Timeout.Infinite, 0);
        //    _timerForErrors?.Change(Timeout.Infinite, 0);

        //    // Stop called without start
        //    if ((_executingTask == null) && (_executingErrorTask == null))
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
        //        if (_executingErrorTask != null)
        //            taskPool.Add(_executingErrorTask);
        //        taskPool.Add(Task.Delay(Timeout.Infinite, cancellationToken));
        //        // Wait until the task completes or the stop token triggers
        //        await Task.WhenAny(taskPool);
        //    }
        //}

        //public override void Dispose()
        //{
        //    base.Dispose();
        //   _timerForErrors?.Dispose();
        //}
        //public override void ExecuteTask(object state)
        //{
        //    _timer?.Change(Timeout.Infinite, 0);
        //    if (_executingTask?.Status != TaskStatus.Running)
        //        _executingTask = ExecuteTaskAsync(true, _stoppingCts.Token);
        //    _timer?.Change(TimeSpan.FromMilliseconds(_refreshInterval), TimeSpan.FromMilliseconds(-1));
        //}
        //private void ExecuteErrorTask(object state)
        //{
        //    _timerForErrors?.Change(Timeout.Infinite, 0);
        //    if (_executingErrorTask?.Status != TaskStatus.Running)
        //        _executingErrorTask = ExecuteTaskAsync(false, _stoppingCts.Token);
        //    _timerForErrors?.Change(TimeSpan.FromSeconds(_refreshIntervalForErrors), TimeSpan.FromMilliseconds(-1));
        //}

        //private async Task ExecuteTaskAsync(bool regular, CancellationToken stoppingToken)
        //{
        //    await CheckStockAsync(regular, stoppingToken);
        //}
        private async Task CheckStockAsync(bool regular, CancellationToken stoppingToken)
        {
            try
            {
                using IMarketplaceService MarketplaceScope = _scopeFactory.CreateScope()
                       .ServiceProvider.GetService<IMarketplaceService>();
                await MarketplaceScope.UpdateStock(regular, stoppingToken);
            }
            catch
            {

            }
        }

        //public override Task ExecuteTaskAsync(CancellationToken stoppingToken)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
