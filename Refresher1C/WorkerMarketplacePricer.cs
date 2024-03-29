﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Refresher1C.Service;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C
{
    public class WorkerMarketplacePricer : BackgroundService
    {
        private IServiceScopeFactory _scopeFactory;
        private TimeSpan _delay;
        public WorkerMarketplacePricer(IServiceScopeFactory serviceScopeFactory, IConfiguration config)
        {
            _scopeFactory = serviceScopeFactory;
            int.TryParse(config["Pricer:refreshIntervalSec"], out int refreshInterval);
            refreshInterval = Math.Max(refreshInterval, 1);
            _delay = TimeSpan.FromSeconds(refreshInterval);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckMarketplacePriceData(stoppingToken);
                await Task.Delay(_delay, stoppingToken);
            }
        }
        private async Task CheckMarketplacePriceData(CancellationToken stoppingToken)
        {
            try
            {
                //using IMarketplaceService MarketplaceScope = _scopeFactory.CreateScope()
                //       .ServiceProvider.GetService<IMarketplaceService>();
                //await MarketplaceScope.UpdatePrices(stoppingToken);
                using (IServiceScope scope = _scopeFactory.CreateScope())
                {
                    IPricer pricer = scope.ServiceProvider.GetRequiredService<IPricer>();
                    await pricer.UpdatePrices(stoppingToken);
                }
            }
            catch
            {

            }
        }
    }
}
