using Refresher1C.Models.AsyncTasksQueue;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C.Service
{
    public class AsyncTasksQueue : IAsyncTasksQueue
    {
        private readonly ConcurrentQueue<AsyncTasksQueueDto> _queue = new();
        private readonly ConcurrentDictionary<string, int> _keys = new();

        public bool Contains(string orderId)
        {
            return _keys.ContainsKey(orderId);
        }

        public void Enqueue(AsyncTasksQueueDto item)
        {
            if (item == null || string.IsNullOrEmpty(item.Key) || item.WorkItem == null)
            {
                return;
            }

            if (_keys.TryAdd(item.Key, 0))
            {
                _queue.Enqueue(item);
            }
        }

        public bool TryDequeue(out Func<IAsyncTaskService, CancellationToken, Task> workItem)
        {
            workItem = null;
            var success = _queue.TryDequeue(out var item);
            if (success)
            {
                _keys.TryRemove(item.Key, out _);
                workItem = item.WorkItem;
            }
            return success;
        }
    }
}
