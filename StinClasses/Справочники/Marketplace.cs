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
        public EncodeVersion Encoding { get; set; }
        public string FeedId { get; set; }
        public decimal КоэфПроверкиЦен { get; set; }
        public string КонтрагентId { get; set; }
        public string ДоговорId { get; set; }
        private string _складId;
        public string СкладId { 
            get => _складId; 
            set 
            {
                if (!string.IsNullOrWhiteSpace(value) && (value != Common.ПустоеЗначение))
                    _складId = value;
                else
                    _складId = "";
            } 
        }
        public bool NeedStockUpdate { get; set; }
    }
    public interface IMarketplace : IDisposable
    {
        Task<Marketplace> ПолучитьMarketplace(string authApi, string campaignId);
        Task<Marketplace> ПолучитьMarketplaceByOrderId(string orderId);
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
        static Marketplace Map(Sc14042 entity)
        {
            if (entity == null)
                return null;
            return new Marketplace
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
                Encoding = (EncodeVersion)entity.Sp14153,
                FeedId = entity.Sp14154.Trim(),
                КоэфПроверкиЦен = entity.Sp14165,
                КонтрагентId = entity.Sp14175,
                ДоговорId = entity.Sp14176,
                СкладId = entity.Sp14241,
                NeedStockUpdate = entity.Sp14177 == 1,
            };
        }
        public async Task<Marketplace> ПолучитьMarketplace(string Id)
        {
            var entity = await _context.Sc14042s
                .FirstOrDefaultAsync(x => x.Id == Id);
            return Map(entity);
                //.Where(x => x.Id == Id)
                //.Select(entity => new Marketplace
                //{
                //    Id = entity.Id,
                //    CampaignId = entity.Code.Trim(),
                //    Тип = entity.Sp14155.Trim().ToUpper(),
                //    Модель = entity.Sp14164.Trim().ToUpper(),
                //    Наименование = entity.Descr.Trim(),
                //    ShortName = entity.Sp14156.Trim(),
                //    Сортировка = entity.Sp14157,
                //    ClientId = entity.Sp14053.Trim(),
                //    TokenKey = entity.Sp14054.Trim(),
                //    UrlApi = entity.Sp14076.Trim(),
                //    Authorization = entity.Sp14077.Trim(),
                //    Encoding = (EncodeVersion)entity.Sp14153,
                //    FeedId = entity.Sp14154.Trim(),
                //    КоэфПроверкиЦен = entity.Sp14165,
                //    КонтрагентId = entity.Sp14175,
                //    ДоговорId = entity.Sp14176,
                //    СкладId = entity.Sp14241,
                //    NeedStockUpdate = entity.Sp14177 == 1,
                //})
                //.FirstOrDefaultAsync()
                //;
        }
        public async Task<Marketplace> ПолучитьMarketplace(string authApi, string campaignId)
        {
            var entity = await _context.Sc14042s
                .FirstOrDefaultAsync(x => (x.Code.Trim() == campaignId) && (x.Sp14077.Trim() == authApi));
            return Map(entity);
                //.Where(x => (x.Code.Trim() == campaignId) && (x.Sp14077.Trim() == authApi))
                //.Select(entity => new Marketplace
                //{
                //    Id = entity.Id,
                //    CampaignId = entity.Code.Trim(),
                //    Тип = entity.Sp14155.Trim().ToUpper(),
                //    Модель = entity.Sp14164.Trim().ToUpper(),
                //    Наименование = entity.Descr.Trim(),
                //    ShortName = entity.Sp14156.Trim(),
                //    Сортировка = entity.Sp14157,
                //    ClientId = entity.Sp14053.Trim(),
                //    TokenKey = entity.Sp14054.Trim(),
                //    UrlApi = entity.Sp14076.Trim(),
                //    Authorization = entity.Sp14077.Trim(),
                //    Encoding = (EncodeVersion)entity.Sp14153,
                //    FeedId = entity.Sp14154.Trim(),
                //    КоэфПроверкиЦен = entity.Sp14165,
                //    КонтрагентId = entity.Sp14175,
                //    ДоговорId = entity.Sp14176,
                //    СкладId = entity.Sp14241,
                //    NeedStockUpdate = entity.Sp14177 == 1,
                //})
                //.FirstOrDefaultAsync();
        }
        public async Task<Marketplace> ПолучитьMarketplaceByFirma(string authApi, string firmaId)
        {
            var entity = await _context.Sc14042s
                .FirstOrDefaultAsync(x => (x.Parentext == firmaId) && (x.Sp14077.Trim() == authApi));
            return Map(entity);
                //.Where(x => (x.Parentext == firmaId) && (x.Sp14077.Trim() == authApi))
                //.Select(entity => new Marketplace 
                //{
                //    Id = entity.Id,
                //    CampaignId = entity.Code.Trim(),
                //    Тип = entity.Sp14155.Trim().ToUpper(),
                //    Модель = entity.Sp14164.Trim().ToUpper(),
                //    Наименование = entity.Descr.Trim(),
                //    ShortName = entity.Sp14156.Trim(),
                //    Сортировка = entity.Sp14157,
                //    ClientId = entity.Sp14053.Trim(),
                //    TokenKey = entity.Sp14054.Trim(),
                //    UrlApi = entity.Sp14076.Trim(),
                //    Authorization = entity.Sp14077.Trim(),
                //    Encoding = (EncodeVersion)entity.Sp14153,
                //    FeedId = entity.Sp14154.Trim(),
                //    КоэфПроверкиЦен = entity.Sp14165,
                //    КонтрагентId = entity.Sp14175,
                //    ДоговорId = entity.Sp14176,
                //    СкладId = entity.Sp14241,
                //    NeedStockUpdate = entity.Sp14177 == 1,
                //})
                //.FirstOrDefaultAsync();
        }
        public async Task<Marketplace> ПолучитьMarketplaceByOrderId(string orderId)
        {
            var entity = await (from market in _context.Sc14042s
                                join order in _context.Sc13994s on market.Id equals order.Sp14038
                                where order.Id == orderId
                                select market)
                                .FirstOrDefaultAsync();
            return Map(entity);
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
                            ((marketUsing.Sp14214 > 0) || (nom.Sp14215 > 0))
                          select new
                          {
                              Id = nom.Id,
                              DeltaStock = market.Sp14216 == 1 ? 0 : (marketUsing.Sp14214 > 0 ? marketUsing.Sp14214 : nom.Sp14215), 
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
