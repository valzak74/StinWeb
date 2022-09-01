using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Refresher1C.Service;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C
{
    public class WorkerMarketplaceStocker : IHostedService, IDisposable
    {
        private readonly ILogger<WorkerMarketplaceStocker> _logger;
        private IServiceScopeFactory _serviceScopeFactory;
        private int _refreshInterval;
        private int _refreshIntervalForErrors;
        private Timer _timer;
        private Timer _timerForErrors;
        private Task _executingTask;
        private Task _executingErrorTask;
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();
        public WorkerMarketplaceStocker(ILogger<WorkerMarketplaceStocker> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            if (int.TryParse(config["Stocker:refreshIntervalMilliSec"], out _refreshInterval))
                _refreshInterval = Math.Max(_refreshInterval, 1);
            else
                _refreshInterval = 1000; //1 sec
            if (int.TryParse(config["Stocker:refreshIntervalErrorsSec"], out _refreshIntervalForErrors))
                _refreshIntervalForErrors = Math.Max(_refreshIntervalForErrors, 1);
            else
                _refreshIntervalForErrors = 60; //sec
        }
        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WorkerStocker StartAsync");
            _timer = new Timer(ExecuteTask, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(-1));
            _timerForErrors = new Timer(ExecuteErrorTask, null, TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(-1));

            return Task.CompletedTask;
        }
        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("WorkerStocker is stopping.");

            _timer?.Change(Timeout.Infinite, 0);
            _timerForErrors?.Change(Timeout.Infinite, 0);

            // Stop called without start
            if ((_executingTask == null) && (_executingErrorTask == null))
            {
                return;
            }

            try
            {
                // Signal cancellation to the executing method
                _stoppingCts.Cancel();
            }
            finally
            {
                var taskPool = new List<Task>();
                if (_executingTask != null)
                    taskPool.Add(_executingTask);
                if (_executingErrorTask != null)
                    taskPool.Add(_executingErrorTask);
                taskPool.Add(Task.Delay(Timeout.Infinite, cancellationToken));
                // Wait until the task completes or the stop token triggers
                await Task.WhenAny(taskPool);
            }
        }

        public void Dispose()
        {
            _stoppingCts.Cancel();
            _timer?.Dispose();
            _timerForErrors?.Dispose();
        }
        private void ExecuteTask(object state)
        {
            _timer?.Change(Timeout.Infinite, 0);
            _executingTask = ExecuteTaskAsync(true, _stoppingCts.Token);
        }
        private void ExecuteErrorTask(object state)
        {
            _timerForErrors?.Change(Timeout.Infinite, 0);
            _executingErrorTask = ExecuteTaskAsync(false, _stoppingCts.Token);
        }

        private async Task ExecuteTaskAsync(bool regular, CancellationToken stoppingToken)
        {
            await CheckStockAsync(regular, stoppingToken);
            if (regular)
                _timer.Change(TimeSpan.FromMilliseconds(_refreshInterval), TimeSpan.FromMilliseconds(-1));
            else
                _timerForErrors.Change(TimeSpan.FromSeconds(_refreshIntervalForErrors), TimeSpan.FromMilliseconds(-1));
        }
        private async Task CheckStockAsync(bool regular, CancellationToken stoppingToken)
        {
            try
            {
                using IMarketplaceService MarketplaceScope = _serviceScopeFactory.CreateScope()
                       .ServiceProvider.GetService<IMarketplaceService>();
                await MarketplaceScope.UpdateStock(regular, stoppingToken);
            }
            catch
            {

            }
        }
    }
}
