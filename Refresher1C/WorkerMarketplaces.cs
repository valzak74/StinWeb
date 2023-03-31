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
        readonly IServiceScopeFactory _scopeFactory;
        private TimeSpan _delay;
        int _errorsTaskCount;
        public WorkerMarketplaces(IServiceScopeFactory scopeFactory, IConfiguration config)
        {
            _scopeFactory = scopeFactory;
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
                var checkNabor = Task.Run(async () => {
                    using var _service = _scopeFactory.CreateScope().ServiceProvider.GetService<IMarketplaceService>();
                    await _service.CheckNaborNeeded(stoppingToken);
                });
                var yandexBoxes = Task.Run(async () => {
                    using var _service = _scopeFactory.CreateScope().ServiceProvider.GetService<IMarketplaceService>();
                    await _service.PrepareYandexFbsBoxes(regular, stoppingToken);
                });
                var fbsLabels = Task.Run(async () => {
                    using var _service = _scopeFactory.CreateScope().ServiceProvider.GetService<IMarketplaceService>();
                    await _service.PrepareFbsLabels(regular, stoppingToken);
                });
                var buyerInfo = Task.Run(async () => {
                    using var _service = _scopeFactory.CreateScope().ServiceProvider.GetService<IMarketplaceService>();
                    await _service.RefreshBuyerInfo(stoppingToken);
                });
                var orderStatus = Task.Run(async () => {
                    using var _service = _scopeFactory.CreateScope().ServiceProvider.GetService<IMarketplaceService>();
                    await _service.ChangeOrderStatus(stoppingToken);
                });

                await Task.WhenAll(checkNabor, yandexBoxes, fbsLabels, buyerInfo, orderStatus);
            }
            catch
            {

            }
        }
    }
}
