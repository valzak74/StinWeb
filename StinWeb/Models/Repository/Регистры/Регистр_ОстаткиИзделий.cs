using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StinWeb.Models.Repository.Интерфейсы.Регистры;
using StinWeb.Models.DataManager;
using StinClasses.Models;

namespace StinWeb.Models.Repository.Регистры
{
    public class РегистрОстаткиИзделий
    {
        public string НомерКвитанции { get; set; }
        public decimal ДатаКвитанции { get; set; }
        public string СкладId { get; set; }
        public string ПодСкладId { get; set; }
        public string МастерId { get; set; }
        public decimal Количество { get; set; }
    }
    public class Регистр_ОстаткиИзделий : IРегистр_ОстаткиИзделий
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
        public Регистр_ОстаткиИзделий(StinDbContext context)
        {
            this._context = context;
        }
        public async Task<List<РегистрОстаткиИзделий>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string НомерКвитанции, decimal ДатаКвитанции, string складId, string подСкладId = null)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= Common.GetDateTA(_context))
            {
                DateTime dateRegTA = Common.GetRegTA(_context);
                return await (from r in _context.Rg11049s
                              where r.Period == dateRegTA &&
                                r.Sp11042 == НомерКвитанции &&
                                r.Sp11043 == ДатаКвитанции &&
                                r.Sp11044 == складId &&
                                (string.IsNullOrEmpty(подСкладId) ? true : r.Sp11045 == подСкладId)
                              group r by new { r.Sp11045, r.Sp11046 } into gr
                              where gr.Sum(x => x.Sp11047) != 0
                              select new РегистрОстаткиИзделий
                              {
                                  НомерКвитанции = НомерКвитанции,
                                  ДатаКвитанции = ДатаКвитанции,
                                  СкладId = складId,
                                  ПодСкладId = gr.Key.Sp11045,
                                  МастерId = gr.Key.Sp11046,
                                  Количество = gr.Sum(x => x.Sp11047)
                              })
                              .ToListAsync();
            }
            else
            {
                DateTime startOfMonth = new DateTime(dateReg.Year, dateReg.Month, 1);
                DateTime previousRegPeriod = startOfMonth.AddMonths(-1);
                string PeriodStart = startOfMonth.ToString("yyyyMMdd");
                var h = dateReg.Hour;
                var m = dateReg.Minute;
                var s = dateReg.Second;
                var time = (h * 3600 + m * 60 + s) * 10000;
                var timestr = Common.Encode36(time).PadLeft(6);
                string PeriodEnd = dateReg.ToString("yyyyMMdd") + timestr + (string.IsNullOrEmpty(idDocDeadLine) ? "" : idDocDeadLine);
                var регистр = (from rg in _context.Rg11049s
                               where rg.Period == previousRegPeriod &&
                                 rg.Sp11042 == НомерКвитанции &&
                                 rg.Sp11043 == ДатаКвитанции &&
                                 rg.Sp11044 == складId &&
                                (string.IsNullOrEmpty(подСкладId) ? true : rg.Sp11045 == подСкладId)
                               select new
                               {
                                   ПодСкладId = rg.Sp11045,
                                   МастерId = rg.Sp11046,
                                   начОстаток = (int)(rg.Sp11047 * 100000),
                                   приход = 0,
                                   расход = 0
                               })
                              .Concat
                              (from ra in _context.Ra11049s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                 ra.Sp11042 == НомерКвитанции &&
                                 ra.Sp11043 == ДатаКвитанции &&
                                 ra.Sp11044 == складId &&
                                (string.IsNullOrEmpty(подСкладId) ? true : ra.Sp11045 == подСкладId)
                               select new
                               {
                                   ПодСкладId = ra.Sp11045,
                                   МастерId = ra.Sp11046,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp11047 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp11047 * 100000) : 0
                               });
                return await (from r in регистр
                              group r by new { r.ПодСкладId, r.МастерId } into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0
                              select new РегистрОстаткиИзделий
                              {
                                  НомерКвитанции = НомерКвитанции,
                                  ДатаКвитанции = ДатаКвитанции,
                                  СкладId = складId,
                                  ПодСкладId = gr.Key.ПодСкладId,
                                  МастерId = gr.Key.МастерId,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000
                              })
                              .ToListAsync();
            }
        }
        public async Task<List<РегистрОстаткиИзделий>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string НомерКвитанции, decimal ДатаКвитанции, List<string> разрешенныеСкладыIds)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= Common.GetDateTA(_context))
            {
                DateTime dateRegTA = Common.GetRegTA(_context);
                return await (from r in _context.Rg11049s
                              where r.Period == dateRegTA &&
                                r.Sp11042 == НомерКвитанции &&
                                r.Sp11043 == ДатаКвитанции &&
                                разрешенныеСкладыIds.Contains(r.Sp11044)
                              group r by new { r.Sp11044, r.Sp11045, r.Sp11046 } into gr
                              where gr.Sum(x => x.Sp11047) != 0
                              select new РегистрОстаткиИзделий
                              {
                                  НомерКвитанции = НомерКвитанции,
                                  ДатаКвитанции = ДатаКвитанции,
                                  СкладId = gr.Key.Sp11044,
                                  ПодСкладId = gr.Key.Sp11045,
                                  МастерId = gr.Key.Sp11046,
                                  Количество = gr.Sum(x => x.Sp11047)
                              })
                              .ToListAsync();
            }
            else
            {
                DateTime startOfMonth = new DateTime(dateReg.Year, dateReg.Month, 1);
                DateTime previousRegPeriod = startOfMonth.AddMonths(-1);
                string PeriodStart = startOfMonth.ToString("yyyyMMdd");
                var h = dateReg.Hour;
                var m = dateReg.Minute;
                var s = dateReg.Second;
                var time = (h * 3600 + m * 60 + s) * 10000;
                var timestr = Common.Encode36(time).PadLeft(6);
                string PeriodEnd = dateReg.ToString("yyyyMMdd") + timestr + (string.IsNullOrEmpty(idDocDeadLine) ? "" : idDocDeadLine);
                var регистр = (from rg in _context.Rg11049s
                               where rg.Period == previousRegPeriod &&
                                 rg.Sp11042 == НомерКвитанции &&
                                 rg.Sp11043 == ДатаКвитанции &&
                                 разрешенныеСкладыIds.Contains(rg.Sp11044)
                               select new
                               {
                                   СкладId = rg.Sp11044,
                                   ПодСкладId = rg.Sp11045,
                                   МастерId = rg.Sp11046,
                                   начОстаток = (int)(rg.Sp11047 * 100000),
                                   приход = 0,
                                   расход = 0
                               })
                              .Concat
                              (from ra in _context.Ra11049s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                 ra.Sp11042 == НомерКвитанции &&
                                 ra.Sp11043 == ДатаКвитанции &&
                                 разрешенныеСкладыIds.Contains(ra.Sp11044)
                               select new
                               {
                                   СкладId = ra.Sp11044,
                                   ПодСкладId = ra.Sp11045,
                                   МастерId = ra.Sp11046,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp11047 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp11047 * 100000) : 0
                               });
                return await (from r in регистр
                              group r by new { r.СкладId, r.ПодСкладId, r.МастерId } into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0
                              select new РегистрОстаткиИзделий
                              {
                                  НомерКвитанции = НомерКвитанции,
                                  ДатаКвитанции = ДатаКвитанции,
                                  СкладId = gr.Key.СкладId,
                                  ПодСкладId = gr.Key.ПодСкладId,
                                  МастерId = gr.Key.МастерId,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000
                              })
                              .ToListAsync();
            }
        }
    }
}
