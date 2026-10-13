using Refresher1C.Service;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C.Models.AsyncTasksQueue
{
    public sealed record AsyncTasksQueueDto
    {
        public string Key { get; init; }

        public Func<IAsyncTaskService, CancellationToken, Task> WorkItem { get; init; }
    }
}
