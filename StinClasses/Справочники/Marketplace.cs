using Microsoft.EntityFrameworkCore;
using StinClasses.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StinClasses.Справочники
{
    public class Marketplace
    {
        public string Id { get; set; }
        public string CampaignId { get; set; }    
        public string Тип { get; set; }
        public string Модель { get; set; }
        public string Наименование { get; set; }
        public string ShortName { get; set; }
        public decimal Сортировка { get; set; }
        public string ClientId { get; set; }
        public string TokenKey { get; set; }
        public string UrlApi { get; set; }
        public string Authorization { get; set; }
        public bool HexEncoding { get; set; }
        public string FeedId { get; set; }
        public decimal КоэфПроверкиЦен { get; set; }
        public string КонтрагентId { get; set; }
        public string ДоговорId { get; set; }
        public bool NeedStockUpdate { get; set; }
    }
    public interface IMarketplace : IDisposable
    {
        Task<Marketplace> ПолучитьMarketplace(string authApi, string campaignId);
        Task<Marketplace> ПолучитьMarketplaceByFirma(string authApi, string firmaId);
        Task<Marketplace> ПолучитьMarketplace(string Id);
        Task<List<string>> GetLockedMarketplaceCatalogEntries(string authApi, List<string> nomenkCodes);
        Task<Dictionary<string, decimal>> GetQuantumInfo(string marketId, List<string> nomenkCodes, CancellationToken cancellationToken);
        Task<Dictionary<string, decimal>> GetDeltaStockInfo(string marketId, List<string> nomenkCodes, CancellationToken cancellationToken);
        Task<Dictionary<string, decimal>> GetDeltaPriceInfo(string marketId, List<string> nomenkCodes, CancellationToken cancellationToken);
    }
    public class MarketplaceEntity : IMarketplace
    {
        private StinDbContext _context;
        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            this.disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public MarketplaceEntity(StinDbContext context)
        {
            _context = context;
        }
        public async Task<Marketplace> ПолучитьMarketplace(string Id)
        {
            return await _context.Sc14042s
                .Where(x => x.Id == Id)
                .Select(entity => new Marketplace
                {
                    Id = entity.Id,
                    CampaignId = entity.Code.Trim(),
                    Тип = entity.Sp14155.Trim().ToUpper(),
                    Модель = entity.Sp14164.Trim().ToUpper(),
                    Наименование = entity.Descr.Trim(),
                    ShortName = entity.Sp14156.Trim(),
                    Сортировка = entity.Sp14157,
                    ClientId = entity.Sp14053.Trim(),
                    TokenKey = entity.Sp14054.Trim(),
                    UrlApi = entity.Sp14076.Trim(),
                    Authorization = entity.Sp14077.Trim(),
                    HexEncoding = entity.Sp14153 == 1,
                    FeedId = entity.Sp14154.Trim(),
                    КоэфПроверкиЦен = entity.Sp14165,
                    КонтрагентId = entity.Sp14175,
                    ДоговорId = entity.Sp14176,
                    NeedStockUpdate = entity.Sp14177 == 1,
                })
                .FirstOrDefaultAsync();
        }
        public async Task<Marketplace> ПолучитьMarketplace(string authApi, string campaignId)
        {
            return await _context.Sc14042s
                .Where(x => (x.Code.Trim() == campaignId) && (x.Sp14077.Trim() == authApi))
                .Select(entity => new Marketplace
                {
                    Id = entity.Id,
                    CampaignId = entity.Code.Trim(),
                    Тип = entity.Sp14155.Trim().ToUpper(),
                    Модель = entity.Sp14164.Trim().ToUpper(),
                    Наименование = entity.Descr.Trim(),
                    ShortName = entity.Sp14156.Trim(),
                    Сортировка = entity.Sp14157,
                    ClientId = entity.Sp14053.Trim(),
                    TokenKey = entity.Sp14054.Trim(),
                    UrlApi = entity.Sp14076.Trim(),
                    Authorization = entity.Sp14077.Trim(),
                    HexEncoding = entity.Sp14153 == 1,
                    FeedId = entity.Sp14154.Trim(),
                    КоэфПроверкиЦен = entity.Sp14165,
                    КонтрагентId = entity.Sp14175,
                    ДоговорId = entity.Sp14176,
                    NeedStockUpdate = entity.Sp14177 == 1,
                })
                .FirstOrDefaultAsync();
        }
        public async Task<Marketplace> ПолучитьMarketplaceByFirma(string authApi, string firmaId)
        {
            return await _context.Sc14042s
                .Where(x => (x.Parentext == firmaId) && (x.Sp14077.Trim() == authApi))
                .Select(entity => new Marketplace 
                {
                    Id = entity.Id,
                    CampaignId = entity.Code.Trim(),
                    Тип = entity.Sp14155.Trim().ToUpper(),
                    Модель = entity.Sp14164.Trim().ToUpper(),
                    Наименование = entity.Descr.Trim(),
                    ShortName = entity.Sp14156.Trim(),
                    Сортировка = entity.Sp14157,
                    ClientId = entity.Sp14053.Trim(),
                    TokenKey = entity.Sp14054.Trim(),
                    UrlApi = entity.Sp14076.Trim(),
                    Authorization = entity.Sp14077.Trim(),
                    HexEncoding = entity.Sp14153 == 1,
                    FeedId = entity.Sp14154.Trim(),
                    КоэфПроверкиЦен = entity.Sp14165,
                    КонтрагентId = entity.Sp14175,
                    ДоговорId = entity.Sp14176,
                    NeedStockUpdate = entity.Sp14177 == 1,
                })
                .FirstOrDefaultAsync();
        }
        public async Task<List<string>> GetLockedMarketplaceCatalogEntries(string authApi, List<string> nomenkCodes)
        {
            return await (from marketUsing in _context.Sc14152s
                          join nom in _context.Sc84s on marketUsing.Parentext equals nom.Id
                          join market in _context.Sc14042s on marketUsing.Sp14147 equals market.Id
                          where marketUsing.Ismark && 
                            (market.Sp14077.Trim() == authApi) && 
                            nomenkCodes.Contains(nom.Code)
                          select nom.Id).ToListAsync();
        }
        public async Task<Dictionary<string,decimal>> GetQuantumInfo(string marketId, List<string> nomenkCodes, CancellationToken cancellationToken)
        {
            return await (from marketUsing in _context.Sc14152s
                          join nom in _context.Sc84s on marketUsing.Parentext equals nom.Id
                          join market in _context.Sc14042s on marketUsing.Sp14147 equals market.Id
                          where (market.Id == marketId) &&
                            nomenkCodes.Contains(nom.Code) &&
                            (marketUsing.Sp14187 > 1)
                          select new
                          {
                              Id = nom.Id,
                              Quantum = marketUsing.Sp14187,
                              IsMark = marketUsing.Ismark
                          }
                )
                .GroupBy(x => x.Id)
                .Select(gr => gr.OrderBy(o => o.IsMark).FirstOrDefault())
                .ToDictionaryAsync(k => k.Id, v => v.Quantum, cancellationToken);
        }
        public async Task<Dictionary<string, decimal>> GetDeltaStockInfo(string marketId, List<string> nomenkCodes, CancellationToken cancellationToken)
        {
            return await (from marketUsing in _context.Sc14152s
                          join nom in _context.Sc84s on marketUsing.Parentext equals nom.Id
                          join market in _context.Sc14042s on marketUsing.Sp14147 equals market.Id
                          where (market.Id == marketId) &&
                            nomenkCodes.Contains(nom.Code) &&
                            (marketUsing.Sp14214 > 0)
                          select new
                          {
                              Id = nom.Id,
                              DeltaStock = market.Sp14216 == 1 ? 0 : nom.Sp14215, //marketUsing.Sp14214,
                              IsMark = marketUsing.Ismark
                          }
                )
                .GroupBy(x => x.Id)
                .Select(gr => gr.OrderBy(o => o.IsMark).FirstOrDefault())
                .ToDictionaryAsync(k => k.Id, v => v.DeltaStock, cancellationToken);
        }
        public async Task<Dictionary<string, decimal>> GetDeltaPriceInfo(string marketId, List<string> nomenkCodes, CancellationToken cancellationToken)
        {
            return await (from marketUsing in _context.Sc14152s
                          join nom in _context.Sc84s on marketUsing.Parentext equals nom.Id
                          join market in _context.Sc14042s on marketUsing.Sp14147 equals market.Id
                          where (market.Id == marketId) &&
                            nomenkCodes.Contains(nom.Code) &&
                            (marketUsing.Sp14213 != 0)
                          select new
                          {
                              Id = nom.Id,
                              DeltaPrice = marketUsing.Sp14213,
                              IsMark = marketUsing.Ismark
                          }
                )
                .GroupBy(x => x.Id)
                .Select(gr => gr.OrderBy(o => o.IsMark).FirstOrDefault())
                .ToDictionaryAsync(k => k.Id, v => v.DeltaPrice, cancellationToken);
        }
    }
}
