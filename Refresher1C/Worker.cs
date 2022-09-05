using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Refresher1C.Service;

namespace Refresher1C
{
    public class Worker : TimedWorker 
    {
        public Worker(IServiceScopeFactory serviceScopeFactory, IConfiguration config)
            :base(serviceScopeFactory)
        {
            if (int.TryParse(config["YouKassa:refreshIntervalSec"], out _refreshInterval))
                _refreshInterval = Math.Max(_refreshInterval, 1);
            else
                _refreshInterval = 10;
        }

        public override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
        {
            await CheckYouKassaData(stoppingToken);
        }
        private async Task CheckYouKassaData(CancellationToken stoppingToken)
        {
            try
            {
                using IYouKassaService YouKasaScope = _scope.CreateScope()
                       .ServiceProvider.GetService<IYouKassaService>();
                await YouKasaScope.CheckPaymentStatusAsync(stoppingToken);
            }
            catch
            {

            }
        }
    }
}
