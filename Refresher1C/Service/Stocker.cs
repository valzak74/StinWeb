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
using System.Security.Cryptography;
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
        IStockFunctions _stockFunctions;
        IFirmaFunctions _firmaFunctions;
        INomenklaturaFunctions _nomenklaturaFunctions;

        readonly string defFirmaId = "";
        readonly Dictionary<string, string> _firmProxy;
        public Stocker(StinDbContext context, IHttpService httpService, IConfiguration configuration, ILogger<IStocker> logger,
            IStockFunctions stockFunctions,
            IFirmaFunctions firmFunctions,
            INomenklaturaFunctions nomenklaturaFunctions,
            IMarketplaceFunctions marketplaceFunctions) 
        { 
            _context = context;
            _httpService = httpService;
            _logger = logger;
            _marketplaceFunctions = marketplaceFunctions;
            _stockFunctions = stockFunctions;
            _firmaFunctions = firmFunctions;
            _nomenklaturaFunctions = nomenklaturaFunctions;
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
            var data = await _marketplaceFunctions.GetMarketUseInfoForStockAsync(marketplace, regular, 100, stoppingToken);
            if (data?.Count() > 0)
            {
                bool fullLock = false;
                var notReadyIds = new List<string>();
                if (marketplace.Тип == "OZON")
                {
                    var offersDictionary = data.ToDictionary(k => k.OfferId, v => v.Id);
                    notReadyIds = await GetOzonNotReadyProducts(marketplace.ФирмаId, marketplace.ClientId, marketplace.TokenKey, offersDictionary, stoppingToken);
                }
                else if (marketplace.Тип == "WILDBERRIES")
                {
                    fullLock = await _stockFunctions.NextBusinessDay(Common.SkladEkran, DateTime.Today, 1, stoppingToken) != 1;
                }
                var validItems = data.Where(x => !notReadyIds.Contains(x.Id));
                var listIds = validItems.Select(x => x.Id).ToList();
                if (MarkVzUpdateStock(listIds, notReadyIds, null, null, null) && (listIds.Count > 0))
                {
                    var stockData = await GetStockData(validItems, fullLock, marketplace, stoppingToken);
                    if (stockData.Count > 0)
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
                            MarkVzUpdateStock(null, null, uploadIds, tooManyIds, errorIds);
                    }
                }
            }
        }
        bool MarkVzUpdateStock(List<string> markIds, List<string> notReadyIds, List<string> uploadIds, List<string> tooManyIds, List<string> errorIds)
        {
            int tryCount = 5;
            TimeSpan sleepPeriod = TimeSpan.FromSeconds(3);
            while (true)
            {
                using var tran = _context.Database.BeginTransaction();
                try
                {
                    if (markIds?.Count > 0)
                        _context.VzUpdatingStocks
                            .Where(x => markIds.Contains(x.MuId))
                            .ToList()
                            .ForEach(x =>
                            {
                                x.Flag = false;
                                x.Taken = true;
                                x.IsError = false;
                            });
                    if (notReadyIds?.Count > 0)
                        _context.VzUpdatingStocks
                            .Where(x => notReadyIds.Contains(x.MuId))
                            .ToList()
                            .ForEach(x =>
                            {
                                x.Flag = false;
                                x.Taken = false;
                                x.IsError = true;
                                x.Updated = DateTime.Now;
                            });
                    if (uploadIds?.Count > 0)
                        _context.VzUpdatingStocks
                            .Where(x => uploadIds.Contains(x.MuId) && x.Taken)
                            .ToList()
                            .ForEach(x =>
                            {
                                x.Flag = false;
                                x.Taken = false;
                                x.IsError = false;
                                x.Updated = DateTime.Now;
                            });
                    if (tooManyIds?.Count > 0)
                        _context.VzUpdatingStocks
                            .Where(x => tooManyIds.Contains(x.MuId) && x.Taken)
                            .ToList()
                            .ForEach(x =>
                            {
                                x.Flag = true;
                                x.Taken = false;
                                x.IsError = false;
                                x.Updated = DateTime.Now;
                            });
                    if (errorIds?.Count > 0)
                        _context.VzUpdatingStocks
                            .Where(x => errorIds.Contains(x.MuId) && x.Taken)
                            .ToList()
                            .ForEach(x =>
                            {
                                x.Flag = false;
                                x.Taken = false;
                                x.IsError = true;
                                x.Updated = DateTime.Now;
                            });
                    _context.SaveChanges();
                    tran.Commit();
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
                    Task.Delay(sleepPeriod);
                }
            }
            return false;
        }
        async Task<List<(string productId, string offerId, string barcode, int stock)>> GetStockData(IEnumerable<MarketUseInfoStock> data, bool fullLock,
            Marketplace marketplace,
            CancellationToken cancellationToken)
        {
            var stockData = new List<(string productId, string offerId, string barcode, int stock)>();
            var разрешенныеФирмы = await _firmaFunctions.GetListAcseptedAsync(marketplace.ФирмаId);
            List<string> списокСкладов = null;
            if (string.IsNullOrEmpty(marketplace.СкладId))
            {
                if (marketplace.Модель == "DBS")
                    списокСкладов = await _stockFunctions.ПолучитьСкладIdОстатковMarketplace();
                else
                    списокСкладов = new List<string> { Common.SkladEkran };
            }
            else
                списокСкладов = new List<string> { marketplace.СкладId };
            var validNomIds = data.Select(x => x.NomId).ToList();
            var списокНоменклатуры = await _nomenklaturaFunctions.ПолучитьСвободныеОстатки(разрешенныеФирмы, списокСкладов, validNomIds, false);
            var резервыМаркета = await _nomenklaturaFunctions.GetReserveByMarketplace(marketplace.Id, validNomIds);
            foreach (var item in data)
            {
                long остаток = 0;
                if (!fullLock)
                {
                    var номенклатура = списокНоменклатуры.FirstOrDefault(x => x.Id == item.NomId);
                    if (номенклатура != null)
                    {
                        резервыМаркета.TryGetValue(item.NomId, out decimal резервМаркета);
                        if (item.Квант > 1)
                        {
                            var остатокРегистр = номенклатура.Остатки
                                .Where(x => x.СкладId == Common.SkladEkran)
                                .Sum(x => x.СвободныйОстаток);
                            if (marketplace.Тип == "ЯНДЕКС")
                                остатокРегистр += резервМаркета;
                            остаток = (int)(((остатокРегистр / номенклатура.Единица.Коэффициент) - item.DeltaStock) / item.Квант);
                            if (marketplace.Тип == "ЯНДЕКС")
                                остаток = остаток * (int)item.Квант;
                        }
                        else
                        {
                            var остатокРегистр = номенклатура.Остатки.Sum(x => x.СвободныйОстаток);
                            if (marketplace.Тип == "ЯНДЕКС")
                                остатокРегистр += резервМаркета;
                            остаток = (long)((остатокРегистр - item.DeltaStock) / номенклатура.Единица.Коэффициент);
                        }
                        остаток = Math.Max(остаток, 0);
                    }
                }
                stockData.Add((productId: item.ProductId, offerId: item.OfferId, barcode: item.Barcode, stock: (item.Locked ? 0 : (int)остаток)));
            }
            return stockData;
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
            List<(string productId, string offerId, string barcode, int stock)> stockData, 
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
            List<(string productId, string offerId, string barcode, int stock)> stockData, 
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
            List<(string productId, string offerId, string barcode, int stock)> stockData,
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
            List<(string productId, string offerId, string barcode, int stock)> stockData,
            Marketplace marketplace,
            CancellationToken cancellationToken)
        {
            int.TryParse(marketplace.Code, out int warehouseId);
            var result = await WbClasses.Functions.UpdateStock(_httpService, _firmProxy[marketplace.ФирмаId], marketplace.TokenKey, warehouseId,
                stockData.ToDictionary(k => k.barcode, v => v.stock),
                cancellationToken);
            var commonErrorTags = new List<string> { "common", "errorText", "additionalError" };
            if (result.errors?.Count > 0)
            {
                foreach (var item in result.errors)
                    _logger.LogError(item.Key + ": " + item.Value);
                var errorOffers = result.errors.Where(x => !commonErrorTags.Contains(x.Key)).Select(x => x.Key);
                errorIds.AddRange(data.Where(x => errorOffers.Contains(x.Barcode)).Select(x => x.Id));
                uploadIds.AddRange(data.Where(x => !errorOffers.Contains(x.Barcode)).Select(x => x.Id));
            }
            else
                uploadIds.AddRange(data.Where(x => stockData.Select(y => y.barcode).Contains(x.Barcode)).Select(x => x.Id));
        }
        async Task SetYandexData(List<string> uploadIds,
            List<(string productId, string offerId, string barcode, int stock)> stockData,
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
