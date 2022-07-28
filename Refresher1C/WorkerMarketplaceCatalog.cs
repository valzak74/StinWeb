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
    public class WorkerMarketplaceCatalog : IHostedService, IDisposable
    {
        private readonly ILogger<WorkerMarketplaceCatalog> _logger;
        private IServiceScopeFactory _serviceScopeFactory;
        private List<TimeSpan> _executeTime;
        private Timer _timer;
        private Task _executingTask;
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();
        public WorkerMarketplaceCatalog(ILogger<WorkerMarketplaceCatalog> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            var executeTime = config["Catalog:executeTime"].Split(';');
            _executeTime = new List<TimeSpan>();
            foreach (var timeSpan in executeTime)
            {
               if (TimeSpan.TryParseExact(timeSpan, @"hh\:mm", null, out TimeSpan parsedTimeSpan))
                    _executeTime.Add(parsedTimeSpan);
            }
        }
        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WorkerCatalog StartAsync");
            _timer = new Timer(ExecuteTask, null, GetNextStartDelay(), TimeSpan.FromMilliseconds(-1));

            return Task.CompletedTask;
        }
        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("WorkerCatalog is stopping.");

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
            //_logger.LogInformation("WorkerCatalog running at: {time}", DateTimeOffset.Now);
            await CheckMarketplaceCatalog(stoppingToken);
            //_logger.LogInformation("WorkerCatalog finished at: {time}", DateTimeOffset.Now);
        }
        private async Task CheckMarketplaceCatalog(CancellationToken stoppingToken)
        {
            try
            {
                using IMarketplaceService MarketplaceScope = _serviceScopeFactory.CreateScope()
                       .ServiceProvider.GetService<IMarketplaceService>();
                await MarketplaceScope.CheckCatalog(stoppingToken);
            }
            catch
            {

            }
        }
    }
}
