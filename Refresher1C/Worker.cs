using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Refresher1C.Service;
using Microsoft.Extensions.Hosting;

namespace Refresher1C
{
    public class Worker : BackgroundService
    {
        private IServiceScopeFactory _scopeFactory;
        private TimeSpan _delay;
        public Worker(IServiceScopeFactory serviceScopeFactory, IConfiguration config)
        {
            _scopeFactory = serviceScopeFactory;
            int.TryParse(config["YouKassa:refreshIntervalSec"], out int refreshInterval);
            refreshInterval = Math.Max(refreshInterval, 1);
            _delay = TimeSpan.FromSeconds(refreshInterval);
            //if (int.TryParse(config["YouKassa:refreshIntervalSec"], out _refreshInterval))
            //    _refreshInterval = Math.Max(_refreshInterval, 1);
            //else
            //    _refreshInterval = 10;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckYouKassaData(stoppingToken);
                await Task.Delay(_delay, stoppingToken);
            }
        }
        //public override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
        //{
        //    await CheckYouKassaData(stoppingToken);
        //}
        private async Task CheckYouKassaData(CancellationToken stoppingToken)
        {
            try
            {
                using IYouKassaService YouKasaScope = _scopeFactory.CreateScope()
                       .ServiceProvider.GetService<IYouKassaService>();
                await YouKasaScope.CheckPaymentStatusAsync(stoppingToken);
            }
            catch
            {

            }
        }
    }
}
