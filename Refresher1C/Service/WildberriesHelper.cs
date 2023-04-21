using HttpExtensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StinClasses;
using StinClasses.Справочники.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WbClasses;

namespace Refresher1C.Service
{
    public class WildberriesHelper : IWildberriesHelper
    {
        IMemoryCache _cache;
        ILogger<WildberriesHelper> _logger;
        IHttpService _httpService;
        IOrderFunctions _orderFunctions;
        public WildberriesHelper(IMemoryCache cache, IHttpService httpService, IOrderFunctions orderFunctions, ILogger<WildberriesHelper> logger) 
        { 
            _cache = cache;
            _httpService = httpService;
            _orderFunctions = orderFunctions;
            _logger = logger;
        }

        public async Task<DateTime> GetActiveSupplyShipmentDate(string proxyHost, string authToken, string marketplaceId, CancellationToken cancellationToken)
        {
            var supplyListResult = await Functions.GetSuppliesList(_httpService, proxyHost, authToken, cancellationToken);
            if (!string.IsNullOrEmpty(supplyListResult.error))
            {
                _logger.LogError(supplyListResult.error);
                return DateTime.MinValue;
            }
            DateTime shipmentDate = DateTime.MinValue;
            var supplyId = supplyListResult.supplyIds.FirstOrDefault();
            if (!string.IsNullOrEmpty(supplyId))
            {
                if (!_cache.TryGetValue(supplyId, out shipmentDate))
                {
                    shipmentDate = _orderFunctions.GetShipmentDateByServiceName(marketplaceId, supplyId);
                    if (shipmentDate > Common.min1cDate)
                        _cache.Set(supplyId, shipmentDate, TimeSpan.FromDays(1));
                }
            }
            return shipmentDate;
        }
    }
}
