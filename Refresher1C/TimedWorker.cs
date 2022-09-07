using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C
{
    public abstract class TimedWorker : IHostedService, IDisposable
    {
        protected Timer _timer;
        protected int _refreshInterval;
        protected TimeSpan _dueTime;
        protected Task _executingTask;
        protected IServiceScopeFactory _scope;
        protected readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();
        public TimedWorker(IServiceScopeFactory serviceScopeFactory)
        {
            _scope = serviceScopeFactory;
        }
        public virtual void Dispose()
        {
            _stoppingCts.Cancel();
            _timer?.Dispose();
        }

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(ExecuteTask, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(-1));
            return Task.CompletedTask;
        }

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);

            // Stop called without start
            if (_executingTask == null)
                return;

            try
            {
                // Signal cancellation to the executing method
                _stoppingCts.Cancel();
            }
            finally
            {
                // Wait until the task completes or the stop token triggers
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }
        }
        public virtual void ExecuteTask(object state)
        {
            _timer?.Change(Timeout.Infinite, 0);
            if (_executingTask?.Status != TaskStatus.Running)
                _executingTask = ExecuteTaskAsync(_stoppingCts.Token);
            _timer?.Change(_dueTime, TimeSpan.FromMilliseconds(-1));
        }

        public abstract Task ExecuteTaskAsync(CancellationToken stoppingToken);
    }
}
