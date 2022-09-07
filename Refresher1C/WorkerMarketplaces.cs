using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Refresher1C.Service;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C
{
    public class WorkerMarketplaces : BackgroundService
    {
        private IServiceScopeFactory _scopeFactory;
        private TimeSpan _delay;
        public WorkerMarketplaces(IServiceScopeFactory serviceScopeFactory, IConfiguration config)
        {
            _scopeFactory = serviceScopeFactory;
            int.TryParse(config["Marketplace:refreshIntervalSec"], out int refreshInterval);
            refreshInterval = Math.Max(refreshInterval, 1);
            _delay = TimeSpan.FromSeconds(refreshInterval);
            //if (int.TryParse(config["Marketplace:refreshIntervalSec"], out _refreshInterval))
            //    _refreshInterval = Math.Max(_refreshInterval, 1);
            //else
            //    _refreshInterval = 60;
            //_dueTime = TimeSpan.FromSeconds(_refreshInterval);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckMarketplaceData(stoppingToken);
                await Task.Delay(_delay, stoppingToken);
            }
        }
        //public override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
        //{
        //    await CheckMarketplaceData(stoppingToken);
        //}
        private async Task CheckMarketplaceData(CancellationToken stoppingToken)
        {
            try
            {
                //using IMarketplaceService MarketplaceScope = _scopeFactory.CreateScope()
                //       .ServiceProvider.GetService<IMarketplaceService>();
                Task checkNabor = Task.Run(async () => 
                {
                    using IMarketplaceService MarketplaceScope = _scopeFactory.CreateScope()
                           .ServiceProvider.GetService<IMarketplaceService>();
                    await MarketplaceScope.CheckNaborNeeded(stoppingToken);
                });
                Task yandexBoxes = Task.Run(async () =>
                {
                    using IMarketplaceService MarketplaceScope = _scopeFactory.CreateScope()
                           .ServiceProvider.GetService<IMarketplaceService>();
                    await MarketplaceScope.PrepareYandexFbsBoxes(stoppingToken);
                });
                Task fbsLabels = Task.Run(async () =>
                {
                    using IMarketplaceService MarketplaceScope = _scopeFactory.CreateScope()
                           .ServiceProvider.GetService<IMarketplaceService>();
                    await MarketplaceScope.PrepareFbsLabels(stoppingToken);
                });
                Task buyerInfo = Task.Run(async () =>
                {
                    using IMarketplaceService MarketplaceScope = _scopeFactory.CreateScope()
                           .ServiceProvider.GetService<IMarketplaceService>();
                    await MarketplaceScope.RefreshBuyerInfo(stoppingToken);
                });
                Task orderStatus = Task.Run(async () =>
                {
                    using IMarketplaceService MarketplaceScope = _scopeFactory.CreateScope()
                           .ServiceProvider.GetService<IMarketplaceService>();
                    await MarketplaceScope.ChangeOrderStatus(stoppingToken);
                });
                await Task.WhenAll(
                    checkNabor,
                    yandexBoxes,
                    fbsLabels,
                    buyerInfo,
                    orderStatus
                    );
                //await MarketplaceScope.CheckNaborNeeded(stoppingToken);
                //await MarketplaceScope.PrepareYandexFbsBoxes(stoppingToken);
                //await MarketplaceScope.PrepareFbsLabels(stoppingToken);
                //await MarketplaceScope.RefreshBuyerInfo(stoppingToken);
                //await MarketplaceScope.ChangeOrderStatus(stoppingToken);
            }
            catch
            {

            }
        }
    }
}
