using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refresher1C.Service;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C
{
    public class WorkerMarketplaceOrderer : TimedWorker 
    {
        public WorkerMarketplaceOrderer(IServiceScopeFactory serviceScopeFactory, IConfiguration config)
            :base(serviceScopeFactory)
        {
            if (int.TryParse(config["Orderer:refreshIntervalMilliSec"], out _refreshInterval))
                _refreshInterval = Math.Max(_refreshInterval, 1);
            else
                _refreshInterval = 1000; //1 sec
        }
        public override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
        {
            await CheckUnfulfilledOrders(stoppingToken);
        }
        private async Task CheckUnfulfilledOrders(CancellationToken stoppingToken)
        {
            try
            {
                using IMarketplaceService MarketplaceScope = _scope.CreateScope()
                       .ServiceProvider.GetService<IMarketplaceService>();
                await MarketplaceScope.RefreshOrders(stoppingToken);
            }
            catch
            {

            }
        }
    }
}
