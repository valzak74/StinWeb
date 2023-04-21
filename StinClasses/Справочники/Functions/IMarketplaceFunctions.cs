using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StinClasses.Справочники.Functions
{
    public interface IMarketplaceFunctions
    {
        Task<IEnumerable<Marketplace>> GetAllAsync(CancellationToken cancellationToken);
        Task<Marketplace> GetMarketplaceByFirmaAsync(string firmaId, string authApi, CancellationToken cancellationToken);
        Task<IEnumerable<MarketUseInfoPrice>> GetMarketUseInfoForPriceAsync(Marketplace marketplace, int limit, CancellationToken cancellationToken);
        Task<IEnumerable<MarketUseInfoStock>> GetMarketUseInfoForStockAsync(IEnumerable<string> nomCodes, Marketplace marketplace, bool regular, int limit, CancellationToken cancellationToken);
        Task ClearTakenMarkVzUpdStock(string marketplaceId, CancellationToken cancellationToken);
        IEnumerable<(string id, string productId, string offerId, decimal квант, decimal price, decimal priceBeforeDiscount)> GetPriceData(IEnumerable<MarketUseInfoPrice> data,
            Marketplace marketplace, decimal checkCoeff);
        Task<IEnumerable<(string productId, string offerId, string barcode, int stock)>> GetStockData(IEnumerable<MarketUseInfoStock> data, 
            Marketplace marketplace,
            CancellationToken cancellationToken);
        Task<IEnumerable<(string productId, string offerId, string barcode, int stock, decimal price, bool pickupOnly, Dictionary<Pickup, int> pickupsInfo)>> GetStockData(IEnumerable<MarketUseInfoStock> data,
            Marketplace marketplace,
            string regionName,
            CancellationToken cancellationToken);
        Task UpdateVzUpdPrice(List<string> muIds, CancellationToken cancellationToken);
        Task CheckOrdersStackInComission(CancellationToken cancellationToken);
    }
}
