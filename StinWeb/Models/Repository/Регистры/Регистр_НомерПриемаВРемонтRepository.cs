using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StinWeb.Models.DataManager;
using StinWeb.Models.Repository.Интерфейсы.Регистры;
using StinClasses.Models;

namespace StinWeb.Models.Repository.Регистры
{
    public class РегистрНомерПриемаВРемонт
    {
        public string НомерКвитанции { get; set; }
        public decimal ДатаКвитанции { get; set; }
        public string ПриемВРемонтId { get; set; }
        public bool Претензия { get; set; }
        public decimal Количество { get; set; }
    }
    public class Регистр_НомерПриемаВРемонтRepository : IРегистр_НомерПриемаВРемонт
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
        public Регистр_НомерПриемаВРемонтRepository(StinDbContext context)
        {
            this._context = context;
        }
        public async Task<string> ПолучитьДокументIdAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string НомерКвитанции, decimal ДатаКвитанции)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= Common.GetDateTA(_context))
            {
                DateTime dateRegTA = Common.GetRegTA(_context);
                return await (from номерПриемаВРемонт in _context.Rg10471s
                              where номерПриемаВРемонт.Period == dateRegTA &&
                                номерПриемаВРемонт.Sp10467 == НомерКвитанции &&
                                номерПриемаВРемонт.Sp10468 == ДатаКвитанции
                              group номерПриемаВРемонт by номерПриемаВРемонт.Sp10469 into gr
                              where gr.Sum(x => x.Sp10470) > 0
                              select gr.Key)
                                .FirstOrDefaultAsync();
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
                var регистр = (from rg in _context.Rg10471s
                               where rg.Period == previousRegPeriod &&
                                 rg.Sp10467 == НомерКвитанции &&
                                 rg.Sp10468 == ДатаКвитанции
                               select new
                               {
                                   doc = rg.Sp10469,
                                   начОстаток = (int)(rg.Sp10470 * 100000),
                                   приход = 0,
                                   расход = 0
                               })
                              .Concat
                              (from ra in _context.Ra10471s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                 ra.Sp10467 == НомерКвитанции &&
                                 ra.Sp10468 == ДатаКвитанции
                               select new
                               {
                                   doc = ra.Sp10469,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp10470 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp10470 * 100000) : 0
                               });
                return await (from r in регистр
                              group r by r.doc into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) > 0
                              select gr.Key)
                        .FirstOrDefaultAsync();
            }
        }
        public async Task<List<РегистрНомерПриемаВРемонт>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string НомерКвитанции, decimal ДатаКвитанции)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= Common.GetDateTA(_context))
            {
                DateTime dateRegTA = Common.GetRegTA(_context);
                return await (from r in _context.Rg10471s
                              where r.Period == dateRegTA &&
                                r.Sp10467 == НомерКвитанции &&
                                r.Sp10468 == ДатаКвитанции
                              group r by new { r.Sp10469, r.Sp13775 } into gr
                              where gr.Sum(x => x.Sp10470) != 0
                              select new РегистрНомерПриемаВРемонт
                              {
                                  НомерКвитанции = НомерКвитанции,
                                  ДатаКвитанции = ДатаКвитанции,
                                  ПриемВРемонтId = gr.Key.Sp10469,
                                  Претензия = gr.Key.Sp13775 == 1,
                                  Количество = gr.Sum(x => x.Sp10470)
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
                var регистр = (from rg in _context.Rg10471s
                               where rg.Period == previousRegPeriod &&
                                 rg.Sp10467 == НомерКвитанции &&
                                 rg.Sp10468 == ДатаКвитанции
                               select new
                               {
                                   ПриемВРемонтId = rg.Sp10469,
                                   Претензия = rg.Sp13775 == 1,
                                   начОстаток = (int)(rg.Sp10470 * 100000),
                                   приход = 0,
                                   расход = 0
                               })
                              .Concat
                              (from ra in _context.Ra10471s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                 ra.Sp10467 == НомерКвитанции &&
                                 ra.Sp10468 == ДатаКвитанции
                               select new
                               {
                                   ПриемВРемонтId = ra.Sp10469,
                                   Претензия = ra.Sp13775 == 1,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp10470 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp10470 * 100000) : 0
                               });
                return await (from r in регистр
                              group r by new { r.ПриемВРемонтId, r.Претензия } into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0
                              select new РегистрНомерПриемаВРемонт
                              {
                                  НомерКвитанции = НомерКвитанции,
                                  ДатаКвитанции = ДатаКвитанции,
                                  ПриемВРемонтId = gr.Key.ПриемВРемонтId,
                                  Претензия = gr.Key.Претензия,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000
                              })
                              .ToListAsync();
            }
        }
    }
}
