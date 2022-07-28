using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Refresher1C.Service;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C
{
    public class WorkerMarketplacePricer : IHostedService, IDisposable
    {
        private readonly ILogger<WorkerMarketplacePricer> _logger;
        private IServiceScopeFactory _serviceScopeFactory;
        private int _RefreshInterval;
        private Timer _timer;
        private Task _executingTask;
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();
        public WorkerMarketplacePricer(ILogger<WorkerMarketplacePricer> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            if (int.TryParse(config["Pricer:refreshIntervalSec"], out _RefreshInterval))
                _RefreshInterval = Math.Max(_RefreshInterval, 1);
            else
                _RefreshInterval = 60;
        }
        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WorkerPricer StartAsync");

            _timer = new Timer(ExecuteTask, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(-1));

            return Task.CompletedTask;
        }
        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("WorkerPricer is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            // Stop called without start
            if (_executingTask == null)
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
                // Wait until the task completes or the stop token triggers
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }
        }

        public void Dispose()
        {
            _stoppingCts.Cancel();
            _timer?.Dispose();
        }
        private void ExecuteTask(object state)
        {
            _timer?.Change(Timeout.Infinite, 0);
            _executingTask = ExecuteTaskAsync(_stoppingCts.Token);
        }

        private async Task ExecuteTaskAsync(CancellationToken stoppingToken)
        {
            await RunJobAsync(stoppingToken);
            _timer.Change(TimeSpan.FromSeconds(_RefreshInterval), TimeSpan.FromMilliseconds(-1));
        }
        private async Task RunJobAsync(CancellationToken stoppingToken)
        {
            //_logger.LogInformation("WorkerPricer running at: {time}", DateTimeOffset.Now);
            await CheckMarketplacePriceData(stoppingToken);
            //_logger.LogInformation("WorkerPricer finished at: {time}", DateTimeOffset.Now);
        }
        private async Task CheckMarketplacePriceData(CancellationToken stoppingToken)
        {
            try
            {
                using IMarketplaceService MarketplaceScope = _serviceScopeFactory.CreateScope()
                       .ServiceProvider.GetService<IMarketplaceService>();
                await MarketplaceScope.UpdatePrices(stoppingToken);
            }
            catch
            {

            }
        }
    }
}
