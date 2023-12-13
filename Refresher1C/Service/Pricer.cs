using HttpExtensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StinClasses.Models;
using StinClasses.Справочники;
using StinClasses.Справочники.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C.Service
{
    public class Pricer : IPricer
    {
        ILogger<IPricer> _logger;
        IHttpService _httpService;
        IMarketplaceFunctions _marketplaceFunctions;
        int _limit;
        decimal _checkCoeff;
        readonly Dictionary<string, string> _firmProxy;
        public Pricer(IHttpService httpService, IConfiguration configuration, ILogger<IPricer> logger,
            IMarketplaceFunctions marketplaceFunctions) 
        {
            _httpService = httpService;
            _logger = logger;
            _marketplaceFunctions = marketplaceFunctions;

            if (int.TryParse(configuration["Pricer:maxPerRequest"], out _limit))
                _limit = Math.Max(_limit, 1);
            else
                _limit = 50;
            if (decimal.TryParse(configuration["Pricer:checkCoefficient"], out _checkCoeff))
                _checkCoeff = Math.Max(_checkCoeff, 1);
            else
                _checkCoeff = 1.2m;
            _firmProxy = new Dictionary<string, string>();
            foreach (var item in configuration.GetSection("CommonSettings:FirmData").GetChildren())
            {
                var configData = item.AsEnumerable();
                var firmaId = configData.FirstOrDefault(x => x.Key.EndsWith("FirmaId")).Value;
                var proxy = configData.FirstOrDefault(x => x.Key.EndsWith("Proxy")).Value;
                _firmProxy.Add(firmaId, proxy);
            }
        }
        public async Task UpdatePrices(CancellationToken stoppingToken)
        {
            var markerplaces = await _marketplaceFunctions.GetAllAsync(stoppingToken);
            foreach (var marketplace in markerplaces)
                //if (marketplace.Тип == "OZON") 
                await UpdatePriceMarketplace(marketplace, stoppingToken);
        }
        async Task UpdatePriceMarketplace(Marketplace marketplace, CancellationToken stoppingToken)
        {
            var data = await _marketplaceFunctions.GetMarketUseInfoForPriceAsync(marketplace, _limit, stoppingToken);
            if (data?.Count() > 0)
            {
                if (long.TryParse(marketplace.FeedId, out long feedId))
                    feedId = 0;
                var priceData = _marketplaceFunctions.GetPriceData(data, marketplace, _checkCoeff);
                if (priceData?.Count() > 0)
                {
                    List<string> uploadIds = new List<string>();
                    switch (marketplace.Тип)
                    {
                        case "ЯНДЕКС":
                            await SetYandexPrice(uploadIds, priceData, marketplace, feedId, stoppingToken);
                            break;
                        case "OZON":
                            await SetOzonPrice(uploadIds, priceData, marketplace, stoppingToken);
                            break;
                        case "SBER":
                            await SetSberPrice(uploadIds, priceData, marketplace, stoppingToken);
                            break;
                        case "ALIEXPRESS":
                            await SetAliExpressPrice(uploadIds, priceData, marketplace, stoppingToken);
                            break;
                        case "WILDBERRIES":
                            await SetWildberriesPrice(uploadIds, priceData, marketplace, stoppingToken);
                            break;
                    }
                    if (uploadIds.Count > 0)
                    {
                        await _marketplaceFunctions.UpdateVzUpdPrice(uploadIds, stoppingToken);
                    }
                }
            }
        }
        async Task SetYandexPrice(List<string> uploadIds,
            IEnumerable<(string id, string productId, string offerId, decimal квант, decimal price, decimal priceBeforeDiscount, decimal minPrice)> priceData,
            Marketplace marketplace,
            long feedId,
            CancellationToken cancellationToken)
        {
            var request = new YandexClasses.PriceUpdateRequest();
            foreach (var item in priceData)
            {
                request.Offers.Add(new YandexClasses.PriceOffer
                {
                    Feed = feedId > 0 ? new YandexClasses.PriceFeed { Id = feedId } : null,
                    Id = item.offerId,
                    Delete = false,
                    Price = new YandexClasses.PriceElement
                    {
                        CurrencyId = YandexClasses.CurrencyType.RUR,
                        Value = item.price,
                        Vat = YandexClasses.PriceVatType.vat_not_valid
                    }
                });
            }
            var result = await YandexClasses.YandexOperators.Exchange<YandexClasses.ErrorResponse>(_httpService,
                $"https://{_firmProxy[marketplace.ФирмаId]}api.partner.market.yandex.ru/v2/campaigns/{marketplace.CampaignId}/offer-prices/updates.json",
                HttpMethod.Post,
                marketplace.ClientId,
                marketplace.TokenKey,
                request,
                cancellationToken);
            if ((result.Item1 == YandexClasses.ResponseStatus.ERROR) ||
                (result.Item2?.Status == YandexClasses.ResponseStatus.ERROR))
            {
                if (!string.IsNullOrEmpty(result.Item3))
                    _logger.LogError(result.Item3);
            }
            else
                uploadIds.AddRange(priceData.Select(x => x.id));
        }
        async Task SetOzonPrice(List<string> uploadIds,
            IEnumerable<(string id, string productId, string offerId, decimal квант, decimal price, decimal priceBeforeDiscount, decimal minPrice)> priceData,
            Marketplace marketplace,
            CancellationToken cancellationToken)
        {
            var result = await OzonClasses.OzonOperators.UpdatePrice(_httpService, _firmProxy[marketplace.ФирмаId], marketplace.ClientId, marketplace.TokenKey,
                priceData.Select(x =>
                {
                    var oldPrice = x.priceBeforeDiscount * x.квант;
                    var price = x.price * x.квант;
                    var minPrice = Math.Min(price, x.minPrice * x.квант);
                    if (((oldPrice > 400) && (oldPrice <= 10000)) || ((price > 400) && (price <= 10000)))
                    {
                        if ((oldPrice - price) <= (Math.Max(price, oldPrice) / 20))
                            oldPrice = price;
                    }
                    else if ((oldPrice > 10000) || (price > 10000))
                    {
                        if ((oldPrice - price) <= 500)
                            oldPrice = price;
                    }
                    if (oldPrice < price)
                        oldPrice = price;
                    if ((minPrice > 0) && (price / minPrice >= 2))//если минимальная цена < 50% от розницы
                        minPrice = price / 2 + 1; //минимальная цена = 50% + 1 руб.
                    return new OzonClasses.PriceRequest
                    {
                        Offer_id = x.offerId,
                        Old_price = oldPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                        Price = price.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                        Min_price = minPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                    };
                }).ToList(),
                cancellationToken);
            if (result.Item1 != null)
            {
                uploadIds.AddRange(priceData.Where(x => result.Item1.Contains(x.offerId)).Select(x => x.id));
                if (!string.IsNullOrEmpty(result.Item2))
                    _logger.LogError(result.Item2);
            }
            else
            {
                if (!string.IsNullOrEmpty(result.Item2))
                    _logger.LogError(result.Item2);
            }
        }
        async Task SetSberPrice(List<string> uploadIds,
            IEnumerable<(string id, string productId, string offerId, decimal квант, decimal price, decimal priceBeforeDiscount, decimal minPrice)> priceData,
            Marketplace marketplace,
            CancellationToken cancellationToken)
        {
            var result = await SberClasses.Functions.UpdatePrice(_httpService, _firmProxy[marketplace.ФирмаId], marketplace.TokenKey,
                priceData.Select(x => new
                {
                    Offer_id = x.offerId,
                    Price = (int)(x.price * x.квант)
                }).ToDictionary(k => k.Offer_id, v => v.Price),
               cancellationToken);
            if (result.success)
            {
                uploadIds.AddRange(priceData.Select(x => x.id));
                if (!string.IsNullOrEmpty(result.error))
                    _logger.LogError(result.error);
            }
            else
            {
                if (!string.IsNullOrEmpty(result.error))
                    _logger.LogError(result.error);
            }
        }
        async Task SetAliExpressPrice(List<string> uploadIds,
            IEnumerable<(string id, string productId, string offerId, decimal квант, decimal price, decimal priceBeforeDiscount, decimal minPrice)> priceData,
            Marketplace marketplace,
            CancellationToken cancellationToken)
        {
            var result = await AliExpressClasses.Functions.UpdatePrice(_httpService, _firmProxy[marketplace.ФирмаId], marketplace.TokenKey,
                priceData.Select(x => new AliExpressClasses.PriceProduct
                {
                    Product_id = x.productId,
                    Skus = new List<AliExpressClasses.PriceSku>
                    {
                        new AliExpressClasses.PriceSku
                        {
                            Sku_code = x.offerId,
                            Discount_price = (x.квант * x.price).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                            Price = (x.квант * (x.priceBeforeDiscount > 0 ? x.priceBeforeDiscount : x.price)).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                        }
                    }
                }).ToList(),
                cancellationToken);
            if (result.UpdatedIds != null)
            {
                uploadIds.AddRange(priceData.Where(x => result.UpdatedIds.Contains(x.productId)).Select(x => x.id));
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                    _logger.LogError(result.ErrorMessage);
            }
            else
            {
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                    _logger.LogError(result.ErrorMessage);
            }
        }
        async Task SetWildberriesPrice(List<string> uploadIds,
            IEnumerable<(string id, string productId, string offerId, decimal квант, decimal price, decimal priceBeforeDiscount, decimal minPrice)> priceData,
            Marketplace marketplace,
            CancellationToken cancellationToken)
        {
            var result = await WbClasses.Functions.UpdatePrice(_httpService, _firmProxy[marketplace.ФирмаId], marketplace.TokenKey,
                priceData.Select(x =>
                {
                    long.TryParse(x.productId, out long nmId);
                    return new WbClasses.PriceRequest
                    {
                        NmId = nmId,
                        Price = decimal.ToInt32(x.квант * x.price)
                    };
                }).ToList(),
                cancellationToken);
            if (result.Item1)
            {
                uploadIds.AddRange(priceData.Select(x => x.id));
                if (!string.IsNullOrEmpty(result.Item2))
                    _logger.LogError(result.Item2);
            }
            else
            {
                if (!string.IsNullOrEmpty(result.Item2))
                    _logger.LogError(result.Item2);
            }
        }
    }
}
