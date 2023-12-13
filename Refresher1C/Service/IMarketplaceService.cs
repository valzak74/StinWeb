using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C.Service
{
    public interface IMarketplaceService: IDisposable
    {
        Task PrepareYandexFbsBoxes(bool regular, CancellationToken stoppingToken);
        Task PrepareFbsLabels(bool regular, CancellationToken stoppingToken);
        Task ChangeOrderStatus(CancellationToken stoppingToken);
        Task RefreshBuyerInfo(CancellationToken stoppingToken);
        Task CheckNaborNeeded(CancellationToken stoppingToken);
        Task CheckPickupExpired(CancellationToken stoppingToken);
        Task UpdatePrices(CancellationToken stoppingToken);
        Task CheckCatalog(CancellationToken stoppingToken);
        Task UpdateStock(bool regular, CancellationToken stoppingToken);
        Task RefreshOrders(CancellationToken stoppingToken);
        Task RefreshSlowOrders(string marketplaceId, CancellationToken stoppingToken);
        Task UpdateTariffs(CancellationToken cancellationToken);
        Task CheckReturns(CancellationToken cancellationToken);
    }
}
