using HttpExtensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C.Service
{
    public interface IWildberriesHelper
    {
        Task<DateTime> GetActiveSupplyShipmentDate(string proxyHost, string authToken, string marketplaceId, string officeId, CancellationToken cancellationToken);
    }
}
