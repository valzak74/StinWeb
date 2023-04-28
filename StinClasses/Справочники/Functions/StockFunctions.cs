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
    internal static class StockExtensions
    {
        internal static T Map<T>(this Sc55 entity) where T : RefBook, new()
        {
            if (entity == null)
                return null;
            return new T
            {
                Id = entity.Id,
                Code = entity.Code.Trim(),
                Наименование = entity.Descr.Trim(),
                Deleted = entity.Ismark,
                BookType = RefBookType.Stock
            };
        }
        internal static СкладExtended MapExtended(this Sc55 entity)
        {
            if (entity == null)
                return null;
            var result = entity.Map<СкладExtended>();
            result.SaturdayOn = entity.Sp14104 == 1;
            result.SundayOn = entity.Sp14105 == 1;
            return result;
        }
    }
    public class StockFunctions : IStockFunctions
    {
        StinDbContext _context;
        IMemoryCache _cache;
        RefBookType _type;
        readonly string _type36;
        public StockFunctions(StinDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
            _type = RefBookType.Stock;
            _type36 = ((long)_type).Encode36();
        }
        public async Task<RefBook> GetStockByIdAsync(string Id, CancellationToken cancellationToken)
        {
            if (_cache.TryGetValue(_type36 + Id, out RefBook result))
                return result;
            var entity = await _context.Sc55s
                .FirstOrDefaultAsync(x => x.Id == Id, cancellationToken);
            result = entity.Map<RefBook>();
            if (result != null)
                _cache.Set(_type36 + Id, result, TimeSpan.FromDays(1));
            return result;
        }
        public async Task<СкладExtended> GetStockExtByIdAsync(string Id, CancellationToken cancellationToken)
        {
            if (_cache.TryGetValue(_type36 + Id + "Ext", out СкладExtended result))
                return result;
            var entity = await _context.Sc55s
                .FirstOrDefaultAsync(x => x.Id == Id, cancellationToken);
            result = entity.MapExtended();
            if (result != null)
                _cache.Set(_type36 + Id + "Ext", result, TimeSpan.FromDays(1));
            return result;
        }
        public async Task<int> NextBusinessDay(string stockId, DateTime checkingDate, int addDays, CancellationToken cancellationToken)
        {
            if (checkingDate == DateTime.MinValue)
                checkingDate = DateTime.Today;
            DateTime checkDate = checkingDate.AddDays(addDays);
            if (!_cache.TryGetValue(checkDate, out bool isBusinessDay))
            {
                var holidaysSchedule = await _context.Sc14108s.SingleOrDefaultAsync(x => !x.Ismark && (x.Parentext == stockId) && (x.Sp14106 == checkDate), cancellationToken);
                if (holidaysSchedule != null)
                    isBusinessDay = holidaysSchedule.Sp14182 == 1;
                else
                {
                    isBusinessDay = true;
                    var dayOfWeek = checkDate.DayOfWeek;
                    if ((dayOfWeek == DayOfWeek.Sunday) || (dayOfWeek == DayOfWeek.Saturday))
                    {
                        var stock = await GetStockExtByIdAsync(stockId, cancellationToken);
                        isBusinessDay = dayOfWeek == DayOfWeek.Sunday ? stock.SundayOn : stock.SaturdayOn;
                    }
                }
                _cache.Set(checkDate, isBusinessDay, TimeSpan.FromDays(1));
            }
            if (!isBusinessDay)
            {
                if (addDays >= 0)
                    addDays++;
                else
                    addDays--;
                addDays = await NextBusinessDay(stockId, checkingDate, addDays, cancellationToken);
            }
            return addDays;
        }
        public async Task<List<string>> ПолучитьСкладIdОстатковMarketplace()
        {
            if (!_cache.TryGetValue("СкладIdОстатковMarketplace", out List<string> result))
            {
                result = await _context.Sc55s
                    .Where(x => x.Sp14180 > 0)
                    .OrderBy(x => x.Sp14180)
                    .Select(x => x.Id)
                    .ToListAsync();
                _cache.Set("СкладIdОстатковMarketplace", result, TimeSpan.FromDays(1));
            }
            return result;
        }
    }
}
