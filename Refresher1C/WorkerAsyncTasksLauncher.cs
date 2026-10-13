using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Refresher1C.Service;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C
{
    public class WorkerAsyncTasksLauncher : BackgroundService
    {
        private IAsyncTasksQueue _asyncTasksQueue;
        private IServiceProvider _serviceProvider;
        private ILogger<WorkerAsyncTasksLauncher> _logger;
        public WorkerAsyncTasksLauncher(
            ILogger<WorkerAsyncTasksLauncher> logger,
            IAsyncTasksQueue asyncTasksQueue,
            IServiceProvider serviceProvider
        )
        {
            _logger = logger;
            _asyncTasksQueue = asyncTasksQueue;
            _serviceProvider = serviceProvider;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckQueueAndLaunch(stoppingToken);
                await Task.Delay(100, stoppingToken);
            }
        }
        private async Task CheckQueueAndLaunch(CancellationToken stoppingToken)
        {
            try
            {
                if (_asyncTasksQueue.TryDequeue(out var workItem))
                {
                    IAsyncTaskService asyncTaskService = _serviceProvider.CreateScope()
                           .ServiceProvider.GetService<IAsyncTaskService>();

                    await workItem(asyncTaskService, stoppingToken);
                    while (_asyncTasksQueue.TryDequeue(out workItem) && !stoppingToken.IsCancellationRequested)
                    {
                        await workItem(asyncTaskService, stoppingToken);
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
