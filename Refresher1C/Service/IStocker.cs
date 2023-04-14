using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C.Service
{
    public interface IStocker
    {
        Task UpdateStock(bool regular, CancellationToken stoppingToken);
    }
}
