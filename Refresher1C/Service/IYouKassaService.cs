using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C.Service
{
    interface IYouKassaService : IDisposable
    {
        Task CheckPaymentStatusAsync(CancellationToken stoppingToken);
    }
}
