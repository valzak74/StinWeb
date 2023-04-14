using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C.Service
{
    public interface IPricer
    {
        Task UpdatePrices(CancellationToken stoppingToken);
    }
}
