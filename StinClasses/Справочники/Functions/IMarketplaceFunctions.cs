using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StinClasses.Справочники.Functions
{
    public interface IMarketplaceFunctions
    {
        Task<IEnumerable<Marketplace>> GetAllAsync(CancellationToken cancellationToken);
        Task<IEnumerable<MarketUseInfoPrice>> GetMarketUseInfoForPriceAsync(Marketplace marketplace, int limit, CancellationToken cancellationToken);
        Task<IEnumerable<MarketUseInfoStock>> GetMarketUseInfoForStockAsync(Marketplace marketplace, bool regular, int limit, CancellationToken cancellationToken);
        Task ClearTakenMarkVzUpdStock(string marketplaceId, CancellationToken cancellationToken);
        List<(string id, string productId, string offerId, decimal квант, decimal price, decimal priceBeforeDiscount)> GetPriceData(IEnumerable<MarketUseInfoPrice> data,
            Marketplace marketplace, decimal checkCoeff);
        Task UpdateVzUpdPrice(List<string> muIds, CancellationToken cancellationToken);
    }
}
