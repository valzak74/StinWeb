using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using StinClasses.Models;
using StinClasses.Документы;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StinClasses.Справочники.Functions
{
    internal static class MarketplaceExtensions
    {
        internal static Marketplace Map(this Sc14042 entity)
        {
            if (entity == null)
                return null;
            return new Marketplace
            {
                Id = entity.Id,
                Code = entity.Code.Trim(),
                Наименование = entity.Descr.Trim(),
                Deleted = entity.Ismark,
                BookType = RefBookType.Marketplace,
                CampaignId = entity.Code.Trim(),
                Тип = entity.Sp14155.Trim().ToUpper(),
                Модель = entity.Sp14164.Trim().ToUpper(),
                ShortName = entity.Sp14156.Trim(),
                Сортировка = entity.Sp14157,
                ClientId = entity.Sp14053.Trim(),
                Secret = entity.Sp14195.Trim(),
                TokenKey = entity.Sp14054.Trim(),
                UrlApi = entity.Sp14076.Trim(),
                Authorization = entity.Sp14077.Trim(),
                Encoding = (EncodeVersion)entity.Sp14153,
                FeedId = entity.Sp14154.Trim(),
                КоэфПроверкиЦен = entity.Sp14165,
                ФирмаId = entity.Parentext,
                КонтрагентId = entity.Sp14175,
                ДоговорId = entity.Sp14176,
                СкладId = entity.Sp14241,
                NeedStockUpdate = entity.Sp14177 == 1,
                StockOriginal = entity.Sp14216 == 1
            };
        }

    }
    public class MarketplaceFunctions : IMarketplaceFunctions
    {
        StinDbContext _context;
        IMemoryCache _cache;
        IServiceProvider _serviceProvider;
        public MarketplaceFunctions(IServiceProvider serviceProvider, StinDbContext context, IMemoryCache cache) 
        { 
            _context = context;
            _cache = cache;
            _serviceProvider = serviceProvider;
        }
        public async Task<IEnumerable<Marketplace>> GetAllAsync(CancellationToken cancellationToken)
        {
            if (!_cache.TryGetValue("Marketplaces", out List<Marketplace> markerplaces))
            {
                var entities = await _context.Sc14042s.ToListAsync(cancellationToken);
                markerplaces = new();
                entities.ForEach(x => markerplaces.Add(x.Map()));
                if (markerplaces.Count > 0)
                    _cache.Set("Marketplaces", markerplaces, TimeSpan.FromDays(1));
            }
            return markerplaces;
        }
        public async Task<Marketplace> GetMarketplaceByFirmaAsync(string firmaId, string authApi, CancellationToken cancellationToken)
        {
            var sb = new StringBuilder("Market");
            sb.Append(firmaId);
            sb.Append(authApi);
            string searchKey = sb.ToString();
            if (!_cache.TryGetValue(searchKey, out Marketplace marketplace))
            {
                var entity = await _context.Sc14042s
                .FirstOrDefaultAsync(x => (x.Parentext == firmaId) && (x.Sp14077.Trim() == authApi));
                marketplace = entity?.Map();
                if (marketplace != null)
                    _cache.Set(searchKey, marketplace, TimeSpan.FromDays(1));
            }
            return marketplace;
        }
        public async Task<IEnumerable<MarketUseInfoStock>> GetMarketUseInfoForStockAsync(IEnumerable<string> nomCodes, Marketplace marketplace, bool regular, int limit, CancellationToken cancellationToken)
        {
            var data = from markUse in _context.Sc14152s
                       join nom in _context.Sc84s on markUse.Parentext equals nom.Id
                       join sc75 in _context.Sc75s on nom.Sp94 equals sc75.Id
                       join updStock in _context.VzUpdatingStocks on markUse.Id equals updStock.MuId
                       where (markUse.Sp14147 == marketplace.Id) &&
                            ((nomCodes != null && (nomCodes.Count() > 0)) ? nomCodes.Contains(nom.Code) : 
                                (markUse.Sp14158 == 1) && //Есть в каталоге 
                                (((regular ? updStock.Flag : updStock.IsError) &&
                                    (updStock.Updated < DateTime.Now.AddSeconds(-150))) ||
                                    (updStock.Updated.Date != DateTime.Today)))
                       //(nom.Code == "D00040383")
                       orderby updStock.Flag descending, updStock.Updated ascending
                       select new MarketUseInfoStock
                       {
                           Id = markUse.Id,
                           Locked = markUse.Ismark,
                           NomId = markUse.Parentext,
                           OfferId = nom.Code.Encode(marketplace.Encoding),
                           ProductId = markUse.Sp14190.Trim(),
                           Barcode = sc75.Sp80.Trim(),
                           Квант = nom.Sp14188,
                           DeltaStock = marketplace.StockOriginal ? 0 : nom.Sp14215, //markUse.Sp14214,
                           UpdatedAt = updStock.Updated,
                           UpdatedFlag = updStock.Flag
                       };
            if (limit > 0)
                data = data.Take(limit);
            return await data.ToListAsync(cancellationToken);
        }
        public async Task<IEnumerable<(string productId, string offerId, string barcode, int stock)>> GetStockData(IEnumerable<MarketUseInfoStock> data,
            Marketplace marketplace,
            CancellationToken cancellationToken)
        {
            var stockData = new List<(string productId, string offerId, string barcode, int stock)>();
            var validNomIds = data.Select(x => x.NomId).ToList();
            using var scope = _serviceProvider.CreateScope();
            var firmaFunctions = scope.ServiceProvider.GetRequiredService<IFirmaFunctions>();
            var stockFunctions = scope.ServiceProvider.GetRequiredService<IStockFunctions>();
            var nomenklaturaFunctions = scope.ServiceProvider.GetRequiredService<INomenklaturaFunctions>();

            var разрешенныеФирмы = await firmaFunctions.GetListAcseptedAsync(marketplace.ФирмаId);
            List<string> списокСкладов = null;
            if (string.IsNullOrEmpty(marketplace.СкладId))
            {
                if (marketplace.Модель == "DBS")
                    списокСкладов = await stockFunctions.ПолучитьСкладIdОстатковMarketplace();
                else
                    списокСкладов = new List<string> { Common.SkladEkran };
            }
            else
                списокСкладов = new List<string> { marketplace.СкладId };
            bool fullLock = false;
            decimal defDeltaStock = 0;
            if (marketplace.Тип == "WILDBERRIES")
            {
                fullLock = await stockFunctions.NextBusinessDay(Common.SkladEkran, DateTime.Today, 1, cancellationToken) != 1;
                defDeltaStock = 1;
            }
            var списокНоменклатуры = await nomenklaturaFunctions.ПолучитьСвободныеОстатки(разрешенныеФирмы, списокСкладов, validNomIds, false);
            var резервыМаркета = await nomenklaturaFunctions.GetReserveByMarketplace(marketplace.Id, validNomIds);
            foreach (var item in data)
            {
                long остаток = 0;
                if (!fullLock && !item.Locked)
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
                            остаток = (int)(((остатокРегистр / номенклатура.Единица.Коэффициент) - (item.DeltaStock + defDeltaStock)) / item.Квант);
                            if (marketplace.Тип == "ЯНДЕКС")
                                остаток = остаток * (int)item.Квант;
                        }
                        else
                        {
                            var остатокРегистр = номенклатура.Остатки.Sum(x => x.СвободныйОстаток);
                            if (marketplace.Тип == "ЯНДЕКС")
                                остатокРегистр += резервМаркета;
                            остаток = (long)((остатокРегистр - (item.DeltaStock + defDeltaStock)) / номенклатура.Единица.Коэффициент);
                        }
                        остаток = Math.Max(остаток, 0);
                    }
                }
                stockData.Add((productId: item.ProductId, offerId: item.OfferId, barcode: item.Barcode, stock: (int)остаток));
            }
            return stockData;
        }
        public async Task<IEnumerable<(string productId, string offerId, string barcode, int stock, decimal price, bool pickupOnly, Dictionary<Pickup, int> pickupsInfo)>> GetStockData(IEnumerable<MarketUseInfoStock> data,
            Marketplace marketplace,
            string regionName,
            CancellationToken cancellationToken)
        {
            var stockData = new List<(string productId, string offerId, string barcode, int stock, decimal price, bool pickupOnly, Dictionary<Pickup,int> pickupsInfo)>();
            var validNomIds = data.Select(x => x.NomId).ToList();
            using var scope = _serviceProvider.CreateScope();
            var firmaFunctions = scope.ServiceProvider.GetRequiredService<IFirmaFunctions>();
            var stockFunctions = scope.ServiceProvider.GetRequiredService<IStockFunctions>();
            var nomenklaturaFunctions = scope.ServiceProvider.GetRequiredService<INomenklaturaFunctions>();
            var pickupFunctions = scope.ServiceProvider.GetRequiredService<IPickupFunctions>();

            var разрешенныеФирмы = await firmaFunctions.GetListAcseptedAsync(marketplace.ФирмаId);
            List<string> списокСкладов = null;
            if (string.IsNullOrEmpty(marketplace.СкладId))
            {
                if (marketplace.Модель == "DBS")
                    списокСкладов = await stockFunctions.ПолучитьСкладIdОстатковMarketplace();
                else
                    списокСкладов = new List<string> { Common.SkladEkran };
            }
            else
                списокСкладов = new List<string> { marketplace.СкладId };
            bool fullLock = false;
            decimal defDeltaStock = 0;
            if (marketplace.Тип == "WILDBERRIES")
            {
                fullLock = await stockFunctions.NextBusinessDay(Common.SkladEkran, DateTime.Today, 1, cancellationToken) != 1;
                defDeltaStock = 1;
            }
            var списокНоменклатуры = await nomenklaturaFunctions.ПолучитьСвободныеОстатки(разрешенныеФирмы, списокСкладов, validNomIds, false);
            var резервыМаркета = await nomenklaturaFunctions.GetReserveByMarketplace(marketplace.Id, validNomIds);
            var pickups = await pickupFunctions.GetPickups(marketplace.ФирмаId, marketplace.Authorization, regionName);
            foreach (var item in data)
            {
                long остаток = 0;
                decimal price = 0;
                bool pickupOnly = false;
                Dictionary<Pickup, int> pickupsInfo = new();
                if (!fullLock && !item.Locked)
                {
                    var номенклатура = списокНоменклатуры.FirstOrDefault(x => x.Id == item.NomId);
                    if (номенклатура != null)
                    {
                        pickupOnly = номенклатура.PickupOnly;
                        if (номенклатура?.Цена != null)
                            price = номенклатура.Цена.РозСП > 0 ? номенклатура.Цена.РозСП : номенклатура.Цена.Розничная;
                        резервыМаркета.TryGetValue(item.NomId, out decimal резервМаркета);
                        if (item.Квант > 1)
                        {
                            var остатокРегистр = номенклатура.Остатки
                                .Where(x => x.СкладId == Common.SkladEkran)
                                .Sum(x => x.СвободныйОстаток);
                            if (marketplace.Тип == "ЯНДЕКС")
                                остатокРегистр += резервМаркета;
                            остаток = (int)(((остатокРегистр / номенклатура.Единица.Коэффициент) - (item.DeltaStock + defDeltaStock)) / item.Квант);
                            if (marketplace.Тип == "ЯНДЕКС")
                                остаток = остаток * (int)item.Квант;
                            if (остаток > 0)
                            {
                                var pickup = pickups.SingleOrDefault(x => x.СкладId == Common.SkladEkran);
                                if (pickup != null)
                                    pickupsInfo.Add(pickup, (int)остаток);
                            }
                        }
                        else
                        {
                            var остатокРегистр = номенклатура.Остатки.Sum(x => x.СвободныйОстаток);
                            if (marketplace.Тип == "ЯНДЕКС")
                                остатокРегистр += резервМаркета;
                            остаток = (long)((остатокРегистр - (item.DeltaStock + defDeltaStock)) / номенклатура.Единица.Коэффициент);
                            if (остаток > 0)
                            {
                                foreach (var pickup in pickups)
                                {
                                    var pickupStock = (int)(номенклатура.Остатки.Where(x => x.СкладId == pickup.СкладId).Sum(x => x.СвободныйОстаток) / номенклатура.Единица.Коэффициент);
                                    if (pickupStock > 0)
                                        pickupsInfo.Add(pickup, pickupStock);
                                }
                            }
                        }
                        остаток = Math.Max(остаток, 0);
                    }
                }
                stockData.Add((productId: item.ProductId, offerId: item.OfferId, barcode: item.Barcode, stock: (int)остаток, price, pickupOnly, pickupsInfo));
            }
            return stockData;
        }
        public async Task UpdateVzUpdPrice(List<string> muIds, CancellationToken cancellationToken)
        {
            var entities = await _context.VzUpdatingPrices
                .Where(x => muIds.Contains(x.MuId))
                .ToListAsync(cancellationToken);
            foreach (var entity in entities)
            {
                entity.Flag = false;
                entity.Updated = DateTime.Now;
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
        public async Task ClearTakenMarkVzUpdStock(string marketplaceId, CancellationToken cancellationToken)
        {
            var entities = await (from updStock in _context.VzUpdatingStocks
                                  join markUse in _context.Sc14152s on updStock.MuId equals markUse.Id
                                  where updStock.Taken && (markUse.Sp14147 == marketplaceId)
                                  select updStock).ToListAsync(cancellationToken);
            foreach (var entity in entities)
                entity.Taken = false;
            await _context.SaveChangesAsync(cancellationToken);
        }
        public async Task<IEnumerable<MarketUseInfoPrice>> GetMarketUseInfoForPriceAsync(Marketplace marketplace, int limit, CancellationToken cancellationToken)
        {
            DateTime limitDate = DateTime.Today.AddDays(-20);
            return await (from markUse in _context.Sc14152s
                          join nom in _context.Sc84s on markUse.Parentext equals nom.Id
                          join vzTovar in _context.VzTovars on nom.Id equals vzTovar.Id into _vzTovar
                          from vzTovar in _vzTovar.DefaultIfEmpty()
                          join updPrice in _context.VzUpdatingPrices on markUse.Id equals updPrice.MuId into _updPrice
                          from updPrice in _updPrice.DefaultIfEmpty()
                          where !markUse.Ismark &&
                              (markUse.Sp14158 == 1) && //Есть в каталоге
                              (updPrice == null || updPrice.Flag || (updPrice.Updated < limitDate)) &&
                              (markUse.Sp14147 == marketplace.Id)
                          //&& ((vzTovar == null) || (vzTovar.Rozn <= 0))
                          //&& nom.Code == "K00035471"
                          //&& nom.Code == "D00061801"
                          select new MarketUseInfoPrice
                          {
                              Id = markUse.Id,
                              Locked = markUse.Ismark,
                              NomId = markUse.Parentext,
                              OfferId = nom.Code.Encode(marketplace.Encoding),
                              ProductId = markUse.Sp14190.Trim(),
                              Квант = nom.Sp14188,
                              DeltaPrice = markUse.Sp14213,
                              Rozn = vzTovar != null ? vzTovar.Rozn ?? 0 : 0,
                              RoznSp = vzTovar != null ? vzTovar.RoznSp ?? 0 : 0,
                              Zakup = vzTovar != null ? vzTovar.Zakup ?? 0 : 0,
                              Fix = markUse.Sp14148,
                              Multiply = markUse.Sp14149,
                              MinPrice = markUse.Sp14198
                          })
                        .OrderBy(x => x.NomId)
                        .Take(limit)
                        .ToListAsync(cancellationToken);
        }
        public IEnumerable<(string id, string productId, string offerId, decimal квант, decimal price, decimal priceBeforeDiscount, decimal minPrice)> GetPriceData(IEnumerable<MarketUseInfoPrice> data,
            Marketplace marketplace, decimal checkCoeff)
        {
            var priceData = new List<(string id, string productId, string offerId, decimal квант, decimal price, decimal priceBeforeDiscount, decimal minPrice)>();
            foreach (var item in data)
            {
                var Цена = item.RoznSp > 0 ? Math.Min(item.RoznSp, item.Rozn) : item.Rozn;
                if (item.Fix > 0)
                {
                    if (item.Fix >= Цена)
                        Цена = item.Fix;
                    else
                    {
                        var Порог = item.Zakup * (item.Multiply > 0 ? item.Multiply : (marketplace.КоэфПроверкиЦен > 0 ? marketplace.КоэфПроверкиЦен : checkCoeff));
                        if (Порог > item.Fix)
                        {
                            //удалить ЦенаФикс из markUsing ???
                            //entry.Sp14148 = 0;
                        }
                        else
                        {
                            Цена = item.Fix;
                        }
                    }
                }
                else if (item.DeltaPrice != 0)
                {
                    var Порог = item.Zakup * (item.Multiply > 0 ? item.Multiply : (marketplace.КоэфПроверкиЦен > 0 ? marketplace.КоэфПроверкиЦен : checkCoeff));
                    var calcPrice = Цена * (100 + item.DeltaPrice) / 100;
                    if (calcPrice >= Порог)
                        Цена = calcPrice;
                }
                Цена = Math.Max(Цена, item.MinPrice);
                var priceBeforeDiscount = Цена < item.Rozn ? item.Rozn : 0.00m;
                priceData.Add((id: item.Id, productId: item.ProductId, offerId: item.OfferId, квант: item.Квант, price: Цена, priceBeforeDiscount: priceBeforeDiscount, minPrice: item.MinPrice));
            }
            return priceData;
        }
        public async Task CheckOrdersStackInComission(CancellationToken cancellationToken)
        {
            DateTime dateRegTA = _context.GetRegTA();
            string base36 = Common.Encode36((long)ВидДокумента.КомплекснаяПродажа).PadLeft(4);
            var orderIds = await (from r in _context.Rg351s //ПартииОтданные
                                  join doc in _context.Dh12542s on r.Sp364 equals (base36 + doc.Iddoc)
                                  join order in _context.Sc13994s on doc.Sp14005 equals order.Id
                                  where (r.Period == dateRegTA) && (r.Sp357 != 0) &&
                                      ((order.Sp13982 == 5) || (order.Sp13982 == 6) || (order.Sp13982 == 17))
                                  group new { r, order } by order.Id into gr
                                  where gr.Sum(x => x.r.Sp357) != 0
                                  select gr.Key).ToListAsync(cancellationToken);
            if (orderIds?.Count > 0)
            {
                using var scope = _serviceProvider.CreateScope();
                var orderFunctions = scope.ServiceProvider.GetRequiredService<IOrderFunctions>();
                foreach (var orderId in orderIds)
                    await orderFunctions.UpdateOrderStatus(orderId, 14, null, cancellationToken);
            }
        }
    }
}
