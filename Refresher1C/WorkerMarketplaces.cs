using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refresher1C.Service;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C
{
    public class WorkerMarketplaces : TimedWorker 
    {
        public WorkerMarketplaces(IServiceScopeFactory serviceScopeFactory, IConfiguration config)
            :base(serviceScopeFactory)
        {
            if (int.TryParse(config["Marketplace:refreshIntervalSec"], out _refreshInterval))
                _refreshInterval = Math.Max(_refreshInterval, 1);
            else
                _refreshInterval = 60;
            _dueTime = TimeSpan.FromSeconds(_refreshInterval);
        }
        public override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
        {
            await CheckMarketplaceData(stoppingToken);
        }
        private async Task CheckMarketplaceData(CancellationToken stoppingToken)
        {
            try
            {
                using IMarketplaceService MarketplaceScope = _scope.CreateScope()
                       .ServiceProvider.GetService<IMarketplaceService>();
                await MarketplaceScope.CheckNaborNeeded(stoppingToken);
                await MarketplaceScope.PrepareYandexFbsBoxes(stoppingToken);
                await MarketplaceScope.PrepareFbsLabels(stoppingToken);
                await MarketplaceScope.RefreshBuyerInfo(stoppingToken);
                await MarketplaceScope.ChangeOrderStatus(stoppingToken);
            }
            catch
            {

            }
        }
    }
}
