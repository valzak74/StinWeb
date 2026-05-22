using Refresher1C.Models.SharedQueue;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C.Service
{
    public class SharedQueue : ISharedQueue
    {
        private readonly ConcurrentQueue<SharedQueueDto> _queue = new();
        private readonly ConcurrentDictionary<string, int> _keys = new();
        public void Enqueue(SharedQueueDto item)
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

        public bool TryDequeue(out Func<IDocCreateOrUpdate, CancellationToken, Task> workItem)
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
