using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C.Service
{
    public interface IAsyncTaskService
    {
        Task DownloadPdfLabels(string firmaId, string clientId, string authToken,
            string orderId,
            string postingNumber,
            long taskId,
            int tryCount,
            CancellationToken cancellationToken
        );
    }
}
