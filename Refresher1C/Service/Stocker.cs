using HttpExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StinClasses;
using StinClasses.Models;
using StinClasses.Справочники;
using StinClasses.Справочники.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C.Service
{
    public class Stocker : IStocker
    {
        ILogger<IStocker> _logger;
        IHttpService _httpService;
        StinDbContext _context;
        IMarketplaceFunctions _marketplaceFunctions;

        readonly string defFirmaId = "";
        readonly Dictionary<string, string> _firmProxy;
        public Stocker(StinDbContext context, IHttpService httpService, IConfiguration configuration, ILogger<IStocker> logger,
            IMarketplaceFunctions marketplaceFunctions) 
        { 
            _context = context;
            _httpService = httpService;
            _logger = logger;
            _marketplaceFunctions = marketplaceFunctions;
            defFirmaId = configuration["Stocker:" + configuration["Stocker:Firma"] + ":FirmaId"];
            _firmProxy = new Dictionary<string, string>();
            foreach (var item in configuration.GetSection("CommonSettings:FirmData").GetChildren())
            {
                var configData = item.AsEnumerable();
                var firmaId = configData.FirstOrDefault(x => x.Key.EndsWith("FirmaId")).Value;
                var proxy = configData.FirstOrDefault(x => x.Key.EndsWith("Proxy")).Value;
                _firmProxy.Add(firmaId, proxy);
            }
        }
        public async Task UpdateStock(bool regular, CancellationToken stoppingToken)
        {
            var markerplaces = await _marketplaceFunctions.GetAllAsync(stoppingToken);
            foreach (var marketplace in markerplaces.Where(x => x.NeedStockUpdate && (string.IsNullOrEmpty(defFirmaId) ? true : (x.ФирмаId == defFirmaId))))
                await UpdateStockMarketplace(regular, marketplace, stoppingToken);
        }
        async Task UpdateStockMarketplace(bool regular, Marketplace marketplace, CancellationToken stoppingToken)
        {
            await _marketplaceFunctions.ClearTakenMarkVzUpdStock(marketplace.Id, stoppingToken);
            var data = await _marketplaceFunctions.GetMarketUseInfoForStockAsync(null, marketplace, regular, 100, stoppingToken);
            if (data?.Count() > 0)
            {
                var notReadyIds = new List<string>();
                if (marketplace.Тип == "OZON")
                    notReadyIds = await GetOzonNotReadyProducts(marketplace.ФирмаId, marketplace.ClientId, marketplace.TokenKey, data.ToDictionary(k => k.OfferId, v => v.Id), stoppingToken);
                var validItems = data.Where(x => !notReadyIds.Contains(x.Id));
                //validItems = validItems.Where(x => x.OfferId == "D00059998"); //tmp
                var listIds = validItems.Select(x => x.Id).ToList();
                if (await MarkVzUpdateStock(listIds, notReadyIds, null, null, null, stoppingToken) && (listIds.Count > 0))
                {
                    var stockData = await _marketplaceFunctions.GetStockData(validItems, marketplace, stoppingToken);
                    if (stockData.Count() > 0)
                    {
                        List<string> uploadIds = new List<string>();
                        List<string> tooManyIds = new List<string>();
                        List<string> errorIds = new List<string>();
                        switch (marketplace.Тип)
                        {
                            case "OZON":
                                await SetOzonData(uploadIds, tooManyIds, errorIds, data, stockData, marketplace, stoppingToken);
                                break;
                            case "SBER":
                                await SetSberData(uploadIds, stockData, listIds, marketplace, stoppingToken);
                                break;
                            case "ALIEXPRESS":
                                await SetAliExpressData(uploadIds, errorIds, data, stockData, marketplace, stoppingToken);
                                break;
                            case "WILDBERRIES":
                                await SetWildberriesData(uploadIds, errorIds, data, stockData, marketplace, stoppingToken);
                                break;
                            case "ЯНДЕКС":
                                await SetYandexData(uploadIds, stockData, listIds, marketplace, stoppingToken);
                                break;
                        }
                        if ((uploadIds.Count > 0) || (tooManyIds.Count > 0) || (errorIds.Count > 0))
                            await MarkVzUpdateStock(null, null, uploadIds, tooManyIds, errorIds, stoppingToken);
                    }
                }
            }
        }
        async Task<bool> MarkVzUpdateStock(List<string> markIds, List<string> notReadyIds, List<string> uploadIds, List<string> tooManyIds, List<string> errorIds, CancellationToken cancellationToken)
        {
            int tryCount = 5;
            TimeSpan sleepPeriod = TimeSpan.FromSeconds(3);
            while (true)
            {
                using var tran = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    if (markIds?.Count > 0)
                    {
                        var d = await _context.VzUpdatingStocks
                            .Where(x => markIds.Contains(x.MuId))
                            .ToListAsync(cancellationToken);
                        foreach (var entity in d)
                        {
                            entity.Flag = false;
                            entity.Taken = true;
                            entity.IsError = false;
                        }
                    }
                    if (notReadyIds?.Count > 0)
                    {
                        var d = await _context.VzUpdatingStocks
                            .Where(x => notReadyIds.Contains(x.MuId))
                            .ToListAsync(cancellationToken);
                        foreach (var entity in d)
                        {
                            entity.Flag = false;
                            entity.Taken = false;
                            entity.IsError = true;
                            entity.Updated = DateTime.Now;
                        }
                    }
                    if (uploadIds?.Count > 0)
                    {
                        var d = await _context.VzUpdatingStocks
                            .Where(x => uploadIds.Contains(x.MuId) && x.Taken)
                            .ToListAsync(cancellationToken);
                        foreach (var entity in d)
                        {
                            entity.Flag = false;
                            entity.Taken = false;
                            entity.IsError = false;
                            entity.Updated = DateTime.Now;
                        }
                    }
                    if (tooManyIds?.Count > 0)
                    {
                        var d = await _context.VzUpdatingStocks
                            .Where(x => tooManyIds.Contains(x.MuId) && x.Taken)
                            .ToListAsync(cancellationToken);
                        foreach (var entity in d)
                        {
                            entity.Flag = true;
                            entity.Taken = false;
                            entity.IsError = false;
                            entity.Updated = DateTime.Now;
                        }
                    }
                    if (errorIds?.Count > 0)
                    {
                        var d = await _context.VzUpdatingStocks
                            .Where(x => errorIds.Contains(x.MuId) && x.Taken)
                            .ToListAsync(cancellationToken);
                        foreach (var entity in d)
                        {
                            entity.Flag = false;
                            entity.Taken = false;
                            entity.IsError = true;
                            entity.Updated = DateTime.Now;
                        }
                    }
                    await _context.SaveChangesAsync(cancellationToken);
                    await tran.CommitAsync(cancellationToken);
                    return true;
                }
                catch (Exception ex)
                {
                    if (_context.Database.CurrentTransaction != null)
                        _context.Database.CurrentTransaction.Rollback();
                    if (--tryCount == 0)
                    {
                        _logger.LogError(ex.Message);
                        break;
                    }
                    await Task.Delay(sleepPeriod, cancellationToken);
                }
            }
            return false;
        }
        async Task<List<string>> GetOzonNotReadyProducts(string firmaId, string clientId, string authToken, Dictionary<string, string> offersDictionary, CancellationToken cancellationToken)
        {
            var checkResult = await OzonClasses.OzonOperators.ProductNotReady(_httpService, _firmProxy[firmaId], clientId, authToken,
                offersDictionary.Select(x => x.Key).ToList(),
                cancellationToken);
            if (checkResult.Item2 != null && !string.IsNullOrEmpty(checkResult.Item2))
                _logger.LogError("OzonNotReadyProducts:" + checkResult.Item2);

            if ((checkResult.Item1 != null) && (checkResult.Item1.Count > 0))
                return offersDictionary.Where(x => checkResult.Item1.Contains(x.Key)).Select(x => x.Value).ToList();
            return new List<string>();
        }
        async Task SetOzonData(List<string> uploadIds, List<string> tooManyIds, List<string> errorIds,
            IEnumerable<MarketUseInfoStock> data,
            IEnumerable<(string productId, string offerId, string barcode, int stock)> stockData, 
            Marketplace marketplace, 
            CancellationToken cancellationToken)
        {
            var result = await OzonClasses.OzonOperators.UpdateStock(_httpService, _firmProxy[marketplace.ФирмаId], marketplace.ClientId, marketplace.TokenKey,
                stockData.Select(x =>
                {
                    if (!long.TryParse(string.IsNullOrWhiteSpace(x.productId) ? marketplace.Code : x.productId, out long WarehouseId))
                        WarehouseId = 0;
                    return new OzonClasses.StockRequest
                    {
                        Offer_id = x.offerId,
                        Stock = x.stock,
                        Warehouse_id = WarehouseId
                    };
                }).ToList(),
                cancellationToken);
            if (result.errorMessage != null && !string.IsNullOrEmpty(result.errorMessage))
                _logger.LogError(result.errorMessage);
            if (result.updatedOfferIds?.Count > 0)
                uploadIds.AddRange(data.Where(x => result.updatedOfferIds.Contains(x.OfferId)).Select(x => x.Id));
            if (result.tooManyRequests?.Count > 0)
                tooManyIds.AddRange(data.Where(x => result.tooManyRequests.Contains(x.OfferId)).Select(x => x.Id));
            if (result.errorOfferIds?.Count > 0)
                errorIds.AddRange(data.Where(x => result.errorOfferIds.Contains(x.OfferId)).Select(x => x.Id));
        }
        async Task SetSberData(List<string> uploadIds,
            IEnumerable<(string productId, string offerId, string barcode, int stock)> stockData, 
            List<string> listIds,
            Marketplace marketplace,
            CancellationToken cancellationToken)
        {
            var result = await SberClasses.Functions.UpdateStock(_httpService, _firmProxy[marketplace.ФирмаId], marketplace.TokenKey,
                stockData.ToDictionary(k => k.offerId, v => v.stock),
                cancellationToken);
            if (!string.IsNullOrEmpty(result.error))
                _logger.LogError(result.error);
            if (result.success)
                uploadIds.AddRange(listIds);
        }
        async Task SetAliExpressData(List<string> uploadIds, List<string> errorIds,
            IEnumerable<MarketUseInfoStock> data,
            IEnumerable<(string productId, string offerId, string barcode, int stock)> stockData,
            Marketplace marketplace,
            CancellationToken cancellationToken)
        {
            var result = await AliExpressClasses.Functions.UpdateStock(_httpService, _firmProxy[marketplace.ФирмаId], marketplace.TokenKey,
                stockData.Select(x => new AliExpressClasses.Product
                {
                    Product_id = x.productId,
                    Skus = new List<AliExpressClasses.StockSku>
                    {
                                            new AliExpressClasses.StockSku
                                            {
                                                Sku_code = x.offerId,
                                                Inventory = x.stock.ToString()
                                            }
                    }
                }).ToList(),
                cancellationToken);
            if (result.ErrorMessage != null && !string.IsNullOrEmpty(result.ErrorMessage))
                _logger.LogError(result.ErrorMessage);
            if (result.UpdatedIds?.Count > 0)
                uploadIds.AddRange(data
                    .Where(x => result.UpdatedIds.Contains(x.ProductId))
                    .Select(x => x.Id));
            if (result.ErrorIds?.Count > 0)
                errorIds.AddRange(data
                    .Where(x => result.ErrorIds.Contains(x.ProductId))
                    .Select(x => x.Id));
        }
        async Task SetWildberriesData(List<string> uploadIds, List<string> errorIds,
            IEnumerable<MarketUseInfoStock> data,
            IEnumerable<(string productId, string offerId, string barcode, int stock)> stockData,
            Marketplace marketplace,
            CancellationToken cancellationToken)
        {
            var warehouseIdByProductId = data
                .GroupBy(x => x.ProductId)
                .ToDictionary(
                    k => k.Key,
                    v => v.Select(x => x.WarehouseId).First()
                );
            var stockDataByWarehouseIds = stockData
                .Select(x =>
                {
                    var warehouseId = warehouseIdByProductId.GetValueOrDefault(x.productId);
                    if (!int.TryParse(string.IsNullOrWhiteSpace(warehouseId) ? marketplace.Code : warehouseId, out var intWarehouseId))
                        intWarehouseId = 0;
                    return new
                    {
                        Barcode = x.barcode,
                        Stock = x.stock,
                        WarehouseId = intWarehouseId
                    };
                })
                .GroupBy(x => x.WarehouseId)
                .Select(x => new
                {
                    WarehouseId = x.Key,
                    StockData = x.GroupBy(y => y.Barcode).ToDictionary(k => k.Key, v => v.Sum(y => y.Stock))
                })
                .ToList();

            foreach (var stockDataByWarehouseId in stockDataByWarehouseIds)
            {
                var result = await WbClasses.Functions.UpdateStock(_httpService, _firmProxy[marketplace.ФирмаId], marketplace.TokenKey, stockDataByWarehouseId.WarehouseId,
                    stockDataByWarehouseId.StockData,
                    cancellationToken);
                var commonErrorTags = new List<string> { "common", "errorText", "additionalError" };
                if (result.errors?.Count > 0)
                {
                    var errorOffers = result.errors.Where(x => !commonErrorTags.Contains(x.Key)).Select(x => x.Key);
                    errorOffers = errorOffers
                        .Except(
                            data.Where(x => x.Locked).Select(x => x.Barcode)
                        )
                        .ToList();
                    errorIds.AddRange(data.Where(x => errorOffers.Contains(x.Barcode)).Select(x => x.Id));
                    uploadIds.AddRange(data.Where(x => !errorOffers.Contains(x.Barcode)).Select(x => x.Id));
                    foreach (var item in result.errors.Where(x => errorOffers.Contains(x.Key)))
                        _logger.LogError("SetWildberriesData " + item.Key + ": " + item.Value);
                }
                else
                    uploadIds.AddRange(data.Where(x => stockDataByWarehouseId.StockData.Select(y => y.Key).Contains(x.Barcode)).Select(x => x.Id));
            }
        }
        async Task SetYandexData(List<string> uploadIds,
            IEnumerable<(string productId, string offerId, string barcode, int stock)> stockData,
            List<string> listIds,
            Marketplace marketplace,
            CancellationToken cancellationToken)
        {
            var result = await YandexClasses.YandexOperators.UpdateStock(_httpService, _firmProxy[marketplace.ФирмаId], marketplace.Code, marketplace.ClientId, marketplace.TokenKey, marketplace.Secret,
                stockData.ToDictionary(k => k.offerId, v => v.stock.ToString()),
                cancellationToken);
            if (!string.IsNullOrEmpty(result.error))
                _logger.LogError(result.error);
            if (result.success)
                uploadIds.AddRange(listIds);
        }
    }
}
