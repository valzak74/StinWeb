using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Refresher1C.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C
{
    public class WorkerOncePerDay : IHostedService, IDisposable
    {
        private readonly ILogger<WorkerOncePerDay> _logger;
        private IServiceScopeFactory _serviceScopeFactory;
        private TimeSpan _executeTime;
        private Timer _timer;
        private Task _executingTask;
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();
        public WorkerOncePerDay(ILogger<WorkerOncePerDay> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            if (!TimeSpan.TryParseExact(config["OncePerDay:executeTime"], @"hh\:mm", null, out _executeTime))
                _executeTime = TimeSpan.Zero;
        }
        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WorkerOncePerDay StartAsync");
            _timer = new Timer(ExecuteTask, null, GetNextStartDelay(), TimeSpan.FromMilliseconds(-1));

            return Task.CompletedTask;
        }
        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("WorkerOncePerDay is stopping.");

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
        private void ExecuteTask(object state)
        {
            _timer?.Change(Timeout.Infinite, 0);
            _executingTask = ExecuteTaskAsync(_stoppingCts.Token);
        }

        private async Task ExecuteTaskAsync(CancellationToken stoppingToken)
        {
            await RunJobAsync(stoppingToken);
            _timer.Change(GetNextStartDelay(), TimeSpan.FromMilliseconds(-1));
        }
        private async Task RunJobAsync(CancellationToken stoppingToken)
        {
            //_logger.LogInformation("WorkerOncePerDay running at: {time}", DateTimeOffset.Now);
            await CheckMarketplaceData(stoppingToken);
            //_logger.LogInformation("WorkerOncePerDay finished at: {time}", DateTimeOffset.Now);
        }
        private async Task CheckMarketplaceData(CancellationToken stoppingToken)
        {
            try
            {
                using IMarketplaceService MarketplaceScope = _serviceScopeFactory.CreateScope()
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
