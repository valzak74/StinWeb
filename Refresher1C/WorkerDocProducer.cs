using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Refresher1C.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C
{
    public class WorkerDocProducer : BackgroundService
    {
        private ISharedQueue _sharedQueue;
        private IServiceScopeFactory _scopeFactory;
        private ILogger<WorkerDocProducer> _logger;
        public WorkerDocProducer(
            ILogger<WorkerDocProducer> logger,
            ISharedQueue sharedQueue, 
            IServiceScopeFactory scopeFactory
        )
        {
            _logger = logger;
            _sharedQueue = sharedQueue;
            _scopeFactory = scopeFactory;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckQueueAndDocProduce(stoppingToken);
                await Task.Delay(100, stoppingToken);
            }
        }
        private async Task CheckQueueAndDocProduce(CancellationToken stoppingToken)
        {
            try
            {
                if (_sharedQueue.TryDequeue(out var workItem))
                {
                    using IDocCreateOrUpdate docCreate = _scopeFactory.CreateScope()
                           .ServiceProvider.GetService<IDocCreateOrUpdate>();

                    await workItem(docCreate, stoppingToken);
                    while (_sharedQueue.TryDequeue(out workItem) && !stoppingToken.IsCancellationRequested) 
                    {
                        await workItem(docCreate, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
        }
    }
}
