using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StinClasses.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public MarketplaceFunctions(StinDbContext context, IMemoryCache cache) 
        { 
            _context = context;
            _cache = cache;
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
        public async Task<IEnumerable<MarketUseInfoStock>> GetMarketUseInfoForStockAsync(Marketplace marketplace, bool regular, int limit, CancellationToken cancellationToken)
        {
            return await (from markUse in _context.Sc14152s
                          join nom in _context.Sc84s on markUse.Parentext equals nom.Id
                          join sc75 in _context.Sc75s on nom.Sp94 equals sc75.Id
                          join updStock in _context.VzUpdatingStocks on markUse.Id equals updStock.MuId
                          where (markUse.Sp14147 == marketplace.Id) &&
                            (markUse.Sp14158 == 1) && //Есть в каталоге 
                            (((regular ? updStock.Flag : updStock.IsError) &&
                              (updStock.Updated < DateTime.Now.AddSeconds(-150))) ||
                             (updStock.Updated.Date != DateTime.Today))
                          //(nom.Code == "D00040383")
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
                          })
            .OrderByDescending(x => x.UpdatedFlag)
            .ThenBy(x => x.UpdatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
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
                          join updPrice in _context.VzUpdatingPrices on markUse.Id equals updPrice.MuId
                          where !markUse.Ismark &&
                              (markUse.Sp14158 == 1) && //Есть в каталоге
                              (updPrice.Flag || (updPrice.Updated < limitDate)) &&
                              (markUse.Sp14147 == marketplace.Id)
                          //&& ((vzTovar == null) || (vzTovar.Rozn <= 0))
                          //&& nom.Code == "K00035471"
                          //&& nom.Code == "D00040383"
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
                          })
                        .OrderBy(x => x.OfferId)
                        .Take(limit)
                        .ToListAsync(cancellationToken);
        }
        public List<(string id, string productId, string offerId, decimal квант, decimal price, decimal priceBeforeDiscount)> GetPriceData(IEnumerable<MarketUseInfoPrice> data,
            Marketplace marketplace, decimal checkCoeff)
        {
            var priceData = new List<(string id, string productId, string offerId, decimal квант, decimal price, decimal priceBeforeDiscount)>();
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
                var priceBeforeDiscount = Цена < item.Rozn ? item.Rozn : 0.00m;
                priceData.Add((id: item.Id, productId: item.ProductId, offerId: item.OfferId, квант: item.Квант, price: Цена, priceBeforeDiscount: priceBeforeDiscount));
            }
            return priceData;
        }
    }
}
