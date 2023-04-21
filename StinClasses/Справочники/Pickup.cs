using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using StinClasses.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

namespace StinClasses.Справочники
{
    public class Pickup: RefBook
    {
        public override RefBookType BookType => RefBookType.MarketPickup;
        public string PickupId { get; set; }
        public string PickupName { get; set; }
        public string ФирмаId { get; set; }
        public string СкладId { get; set; }
        public string RegionId { get; set; }
        public string RegionName { get; set; }
        public string МаксВремяЗаказаСтрока { get; set; }
        public TimeSpan МаксВремяЗаказа
        {
            get
            {
                if (!TimeSpan.TryParse(МаксВремяЗаказаСтрока, out TimeSpan time))
                {
                    // handle validation error
                }
                return time;
            }
        }
        public int КолВоДнейВыполнения { get; set; }
    }
    public interface IPickupFunctions
    {
        Task<IEnumerable<Pickup>> GetPickups(string фирмаId, string authorizationApi, string regionName = "");
    }
    public interface IPickup 
    {
        Task<List<Pickup>> GetPickups(string фирмаId, string authorizationApi, string regionName = "");
    }
    public class PickupFunctions : IPickupFunctions
    {
        IMemoryCache _cache;
        StinDbContext _context;
        public PickupFunctions(StinDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }
        public async Task<IEnumerable<Pickup>> GetPickups(string фирмаId, string authorizationApi, string regionName = "")
        {
            var sb = new StringBuilder("Pickup");
            sb.Append(фирмаId);
            sb.Append(authorizationApi);
            string searchKey = sb.ToString();
            if (!_cache.TryGetValue(searchKey, out IEnumerable<Pickup> pickups))
            {
                pickups = await (from p in _context.Sc14101s
                                 join m in _context.Sc14042s on p.Parentext equals m.Id
                                 where !p.Ismark &&
                                   (m.Parentext == фирмаId) &&
                                   (m.Sp14077.Trim() == authorizationApi) &&
                                   (string.IsNullOrEmpty(regionName) ? true : p.Sp14098.Trim() == regionName)
                                 select new Pickup
                                 {
                                     Id = p.Id,
                                     PickupId = p.Code.Trim(),
                                     PickupName = p.Descr.Trim(),
                                     ФирмаId = фирмаId,
                                     СкладId = p.Sp14097,
                                     RegionName = p.Sp14098.Trim(),
                                     МаксВремяЗаказаСтрока = p.Sp14099.Trim()
                                 }).ToListAsync();
                if (pickups != null)
                    _cache.Set(searchKey, pickups, TimeSpan.FromDays(1));
            }
            return pickups;
        }

    }
    public class PickupEntity : IPickup
    {
        StinDbContext _context;
        //private bool disposed = false;
        //protected virtual void Dispose(bool disposing)
        //{
        //    if (!this.disposed)
        //    {
        //        if (disposing)
        //        {
        //            _context.Dispose();
        //        }
        //    }
        //    this.disposed = true;
        //}
        //public void Dispose()
        //{
        //    Dispose(true);
        //    GC.SuppressFinalize(this);
        //}
        public PickupEntity(StinDbContext context)
        {
            _context = context;
        }
        public async Task<List<Pickup>> GetPickups(string фирмаId, string authorizationApi, string regionName = "")
        {
            return await (from p in _context.Sc14101s
                          join m in _context.Sc14042s on p.Parentext equals m.Id
                          where !p.Ismark &&
                            (m.Parentext == фирмаId) &&
                            (m.Sp14077.Trim() == authorizationApi) &&
                            (string.IsNullOrEmpty(regionName) ? true : p.Sp14098.Trim() == regionName)
                          select new Pickup
                          {
                              Id = p.Id,
                              PickupId = p.Code.Trim(),
                              PickupName = p.Descr.Trim(),
                              ФирмаId = фирмаId,
                              СкладId = p.Sp14097,
                              RegionName = p.Sp14098.Trim(),
                              МаксВремяЗаказаСтрока = p.Sp14099.Trim()
                          }).ToListAsync();
        }
    }
}
