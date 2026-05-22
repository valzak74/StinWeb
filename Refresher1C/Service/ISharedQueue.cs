using Refresher1C.Models.SharedQueue;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C.Service
{
    public interface ISharedQueue
    {
        void Enqueue(SharedQueueDto item);
        bool TryDequeue(out Func<IDocCreateOrUpdate, CancellationToken, Task> workItem);
    }
}
