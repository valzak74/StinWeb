using Refresher1C.Models.AsyncTasksQueue;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C.Service
{
    public interface IAsyncTasksQueue
    {
        bool Contains(string orderId);
        void Enqueue(AsyncTasksQueueDto item);
        bool TryDequeue(out Func<IAsyncTaskService, CancellationToken, Task> workItem);
    }
}
