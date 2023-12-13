using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Refresher1C.Service;
using StinClasses.Справочники.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C
{
    public class WorkerMarketplaceOrdererSlow : BackgroundService
    {
        private IServiceScopeFactory _scopeFactory;
        private TimeSpan _delay;
        public WorkerMarketplaceOrdererSlow(IServiceScopeFactory serviceScopeFactory, IConfiguration config)
        {
            _scopeFactory = serviceScopeFactory;
            int.TryParse(config["Orderer:refreshIntervalMilliSec"], out int refreshInterval);
            refreshInterval = Math.Max(refreshInterval, 100);
            _delay = TimeSpan.FromMilliseconds(refreshInterval);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckOrders(stoppingToken);
                await Task.Delay(_delay, stoppingToken);
            }
        }
        private async Task CheckOrders(CancellationToken stoppingToken)
        {
            try
            {
                IMarketplaceFunctions marketplaceFunctions = _scopeFactory.CreateScope()
                    .ServiceProvider.GetRequiredService<IMarketplaceFunctions>();
                var marketplaces = await marketplaceFunctions.GetAllAsync(stoppingToken);
                var tasks = new List<Task>();
                foreach (var marketplace in marketplaces)
                {
                    var currentMarketplaceId = marketplace.Id;
                    tasks.Add(Task.Run(async () => { 
                        using IMarketplaceService MarketplaceScope = _scopeFactory.CreateScope()
                               .ServiceProvider.GetService<IMarketplaceService>();
                        await MarketplaceScope.RefreshSlowOrders(currentMarketplaceId, stoppingToken);
                    }));
                }
                await Task.WhenAll(tasks);
            }
            catch
            {

            }
        }
    }
}
