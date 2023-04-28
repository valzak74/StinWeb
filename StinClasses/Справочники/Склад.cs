using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StinClasses.Models;

namespace StinClasses.Справочники
{
    public class Склад
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public string Наименование { get; set; }
    }
    public class ПодСклад
    {
        public string Id { get; set; }
        public string Наименование { get; set; }
    }
    public class СкладExtended : RefBook
    {
        public bool SaturdayOn { get; set;}
        public bool SundayOn { get; set;}
    }
    public interface IСклад : IDisposable
    {
        Task<Склад> GetEntityByIdAsync(string Id);
        Task<ПодСклад> GetПодСкладByIdAsync(string Id);
        IQueryable<ПодСклад> ПолучитьПодСклады(string СкладId);
        IQueryable<Склад> ПолучитьРазрешенныеСклады(string ПользовательId);
        Task<List<string>> ПолучитьСкладIdОстатковMarketplace();
        Task<int> ЭтоРабочийДень(string складId, int addDays, DateTime checkingDate = default);
    }
    public class СкладEntity : IСклад
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
        public СкладEntity(StinDbContext context)
        {
            _context = context;
        }
        public async Task<Склад> GetEntityByIdAsync(string Id)
        {
            return await _context.Sc55s
                .Where(x => x.Id == Id && !x.Ismark)
                .Select(y => new Склад
                {
                    Id = y.Id,
                    Code = y.Code.Trim(),
                    Наименование = y.Descr.Trim(),
                })
                .FirstOrDefaultAsync();
        }
        public async Task<ПодСклад> GetПодСкладByIdAsync(string Id)
        {
            return await _context.Sc8963s
                .Where(x => x.Id == Id && !x.Ismark)
                .Select(y => new ПодСклад
                {
                    Id = y.Id,
                    Наименование = y.Descr.Trim(),
                })
                .FirstOrDefaultAsync();
        }
        public IQueryable<ПодСклад> ПолучитьПодСклады(string СкладId)
        {
            return _context.Sc8963s
                .Where(x => !x.Ismark && x.Parentext == СкладId)
                .OrderBy(x => x.Descr)
                .Select(y => new ПодСклад
                {
                    Id = y.Id,
                    Наименование = y.Descr.Trim(),
                });
        }
        public IQueryable<Склад> ПолучитьРазрешенныеСклады(string ПользовательId)
        {
            return from sc8836 in _context.Sc8836s
                   join sc55 in _context.Sc55s on sc8836.Sp8838 equals sc55.Id
                   where sc8836.Parentext == ПользовательId && sc8836.Ismark == false && sc55.Ismark == false
                   orderby sc55.Descr
                   select new Склад
                   {
                       Id = sc55.Id,
                       Code = sc55.Code.Trim(),
                       Наименование = sc55.Descr.Trim(),
                   };
        }
        public async Task<List<string>> ПолучитьСкладIdОстатковMarketplace()
        {
            return await _context.Sc55s
                .Where(x => x.Sp14180 > 0)
                .OrderBy(x => x.Sp14180)
                .Select(x => x.Id)
                .ToListAsync();
        }
        public async Task<int> ЭтоРабочийДень(string складId, int addDays, DateTime checkingDate = default)
        {
            var result = addDays;
            if (checkingDate == DateTime.MinValue)
                checkingDate = DateTime.Today;
            DateTime checkDate = checkingDate.AddDays(addDays);
            var dayOfWeek = checkDate.DayOfWeek;
            bool isBusinessDay = false;
            switch (dayOfWeek)
            {
                case DayOfWeek.Sunday:
                    isBusinessDay = (await _context.Sc55s.FirstOrDefaultAsync(x => x.Id == складId)).Sp14105 == 1;
                    break;
                case DayOfWeek.Saturday:
                    isBusinessDay = (await _context.Sc55s.FirstOrDefaultAsync(x => x.Id == складId)).Sp14104 == 1;
                    break;
                default:
                    isBusinessDay = true;
                    break;
            }
            if (isBusinessDay)
            {
                isBusinessDay = !await _context.Sc14108s.AnyAsync(x => (x.Parentext == складId) && !x.Ismark && (x.Sp14106 == checkDate) && (x.Sp14182 == 0));
            }
            else
            {
                isBusinessDay = await _context.Sc14108s.AnyAsync(x => (x.Parentext == складId) && !x.Ismark && (x.Sp14106 == checkDate) && (x.Sp14182 == 1));
            }
            if (!isBusinessDay)
            {
                if (addDays >= 0)
                    //увеличиваем addDays
                    addDays++;
                else
                    addDays--;
                result = await ЭтоРабочийДень(складId, addDays, checkingDate);
            }
            return result;
        }
    }
}
