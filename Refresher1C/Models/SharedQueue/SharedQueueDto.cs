using Refresher1C.Service;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C.Models.SharedQueue
{
    public sealed record SharedQueueDto
    {
        public string Key { get; init; }

        public Func<IDocCreateOrUpdate, CancellationToken, Task> WorkItem { get; init; }
    }
}
