using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Refresher1C.Service;

namespace Refresher1C
{
    public class Worker : IHostedService, IDisposable
    {
        private readonly ILogger<Worker> _logger;
        private IServiceScopeFactory _serviceScopeFactory;
        private int _YouKassaRefreshInterval;
        private Timer _timer;
        private Task _executingTask;
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();
        public Worker(ILogger<Worker> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            if (int.TryParse(config["YouKassa:refreshIntervalSec"], out _YouKassaRefreshInterval))
                _YouKassaRefreshInterval = Math.Max(_YouKassaRefreshInterval, 1);
            else
                _YouKassaRefreshInterval = 10;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker StartAsync");

            _timer = new Timer(ExecuteTask, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(-1));

            return Task.CompletedTask;
        }
        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker is stopping.");

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
            _timer.Change(TimeSpan.FromSeconds(_YouKassaRefreshInterval), TimeSpan.FromMilliseconds(-1));
        }
        private async Task RunJobAsync(CancellationToken stoppingToken)
        {
            //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await CheckYouKassaData(stoppingToken);
            //_logger.LogInformation("Worker finished at: {time}", DateTimeOffset.Now);
        }
        private async Task CheckYouKassaData(CancellationToken stoppingToken)
        {
            try
            {
                using IYouKassaService YouKasaScope = _serviceScopeFactory.CreateScope()
                       .ServiceProvider.GetService<IYouKassaService>();
                await YouKasaScope.CheckPaymentStatusAsync(stoppingToken);
            }
            catch
            {

            }
        }
    }
}
