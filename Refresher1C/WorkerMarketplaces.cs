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
        int _errorsTaskCount;
        public WorkerMarketplaces(IServiceScopeFactory serviceScopeFactory, IConfiguration config)
        {
            _scopeFactory = serviceScopeFactory;
            int.TryParse(config["Marketplace:refreshIntervalSec"], out int refreshInterval);
            refreshInterval = Math.Max(refreshInterval, 1);
            _delay = TimeSpan.FromSeconds(refreshInterval);
            int.TryParse(config["Marketplace:checkErrorsEvery"], out _errorsTaskCount);
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
                await CheckMarketplaceData(regular, stoppingToken);
                await Task.Delay(_delay, stoppingToken);
            }
        }
        private async Task CheckMarketplaceData(bool regular, CancellationToken stoppingToken)
        {
            try
            {
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
                    await MarketplaceScope.PrepareYandexFbsBoxes(regular, stoppingToken);
                });
                Task fbsLabels = Task.Run(async () =>
                {
                    using IMarketplaceService MarketplaceScope = _scopeFactory.CreateScope()
                           .ServiceProvider.GetService<IMarketplaceService>();
                    await MarketplaceScope.PrepareFbsLabels(regular, stoppingToken);
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
            }
            catch
            {

            }
        }
    }
}
