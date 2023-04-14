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
        private IServiceScopeFactory _scopeFactory;
        private TimeSpan _delay;
        private int _errorsTaskCount;
        public WorkerMarketplaceStocker(IServiceScopeFactory serviceScopeFactory, IConfiguration config)
        {
            _scopeFactory = serviceScopeFactory;
            int.TryParse(config["Stocker:refreshIntervalMilliSec"], out int refreshInterval);
            refreshInterval = Math.Max(refreshInterval, 100);
            _delay = TimeSpan.FromMilliseconds(refreshInterval);
            int.TryParse(config["Stocker:checkErrorsEvery"], out _errorsTaskCount);
            _errorsTaskCount = Math.Max(_errorsTaskCount, 1);
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

        private async Task CheckStockAsync(bool regular, CancellationToken stoppingToken)
        {
            try
            {
                //using IMarketplaceService MarketplaceScope = _scopeFactory.CreateScope()
                //       .ServiceProvider.GetService<IMarketplaceService>();
                //await MarketplaceScope.UpdateStock(regular, stoppingToken);
                using (IServiceScope scope = _scopeFactory.CreateScope())
                {
                    IStocker stocker = scope.ServiceProvider.GetService<IStocker>();
                    await stocker.UpdateStock(regular, stoppingToken);
                }
            }
            catch
            {

            }
        }
    }
}
