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
    public class РегистрОстаткиДоставки
    {
        public string ФирмаId { get; set; }
        public string НоменклатураId { get; set; }
        public string СкладId { get; set; }
        public decimal ЦенаПрод { get; set; }
        public string ДокПеремещенияId13 { get; set; }
        public decimal ЭтоИзделие { get; set; }
        public decimal Количество { get; set; }
    }
    public class Регистр_ОстаткиДоставки : IРегистр_ОстаткиДоставки
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
        public Регистр_ОстаткиДоставки(StinDbContext context)
        {
            this._context = context;
        }
        public async Task<List<РегистрОстаткиДоставки>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string складId, string докОснованиеId13, List<string> номенклатураIds)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= Common.GetDateTA(_context))
            {
                DateTime dateRegTA = Common.GetRegTA(_context);
                return await (from r in _context.Rg8696s
                              where r.Period == dateRegTA &&
                                r.Sp8715 == докОснованиеId13 &&
                                r.Sp8699 == складId &&
                                номенклатураIds.Contains(r.Sp8698)
                              group r by new { r.Sp8697, r.Sp8698, r.Sp8700, r.Sp11041 } into gr
                              where gr.Sum(x => x.Sp8701) != 0
                              select new РегистрОстаткиДоставки
                              {
                                  ФирмаId = gr.Key.Sp8697,
                                  НоменклатураId = gr.Key.Sp8698,
                                  СкладId = складId,
                                  ЦенаПрод = gr.Key.Sp8700,
                                  ДокПеремещенияId13 = докОснованиеId13,
                                  ЭтоИзделие = gr.Key.Sp11041,
                                  Количество = gr.Sum(x => x.Sp8701)
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
                var регистр = (from rg in _context.Rg8696s
                               where rg.Period == previousRegPeriod &&
                                    rg.Sp8715 == докОснованиеId13 &&
                                    rg.Sp8699 == складId &&
                                    номенклатураIds.Contains(rg.Sp8698)
                               select new
                               {
                                   ФирмаId = rg.Sp8697,
                                   НоменклатураId = rg.Sp8698,
                                   ЦенаПрод = rg.Sp8700,
                                   ЭтоИзделие = rg.Sp11041,
                                   начОстаток = (int)(rg.Sp8701 * 100000),
                                   приход = 0,
                                   расход = 0
                               })
                              .Concat
                              (from ra in _context.Ra8696s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                    ra.Sp8715 == докОснованиеId13 &&
                                    ra.Sp8699 == складId &&
                                    номенклатураIds.Contains(ra.Sp8698)
                               select new
                               {
                                   ФирмаId = ra.Sp8697,
                                   НоменклатураId = ra.Sp8698,
                                   ЦенаПрод = ra.Sp8700,
                                   ЭтоИзделие = ra.Sp11041,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp8701 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp8701 * 100000) : 0
                               });
                return await (from r in регистр
                              group r by new { r.ФирмаId, r.НоменклатураId, r.ЦенаПрод, r.ЭтоИзделие } into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0
                              select new РегистрОстаткиДоставки
                              {
                                  ФирмаId = gr.Key.ФирмаId,
                                  НоменклатураId = gr.Key.НоменклатураId,
                                  СкладId = складId,
                                  ЦенаПрод = gr.Key.ЦенаПрод,
                                  ДокПеремещенияId13 = докОснованиеId13,
                                  ЭтоИзделие = gr.Key.ЭтоИзделие,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000
                              })
                              .ToListAsync();
            }
        }
    }

}
