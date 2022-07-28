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
    public class РегистрПартииМастерской
    {
        public decimal Гарантия { get; set; }
        public string Номенклатура { get; set; }
        public string ЗавНомер { get; set; }
        public string СтатусПартии { get; set; }
        public string Контрагент { get; set; }
        public string СкладОткуда { get; set; }
        public DateTime ДатаПриема { get; set; }
        public string НомерКвитанции { get; set; }
        public decimal ДатаКвитанции { get; set; }
        public decimal Количество { get; set; }
    }
    public class Регистр_ПартииМастерской : IРегистр_ПартииМастерской
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
        public Регистр_ПартииМастерской(StinDbContext context)
        {
            this._context = context;
        }
        public async Task<List<РегистрПартииМастерской>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string НомерКвитанции, decimal ДатаКвитанции, string СтатусПартииId = null)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= Common.GetDateTA(_context))
            {
                DateTime dateRegTA = Common.GetRegTA(_context);
                return await (from r in _context.Rg9972s
                              where r.Period == dateRegTA &&
                                r.Sp9969 == НомерКвитанции &&
                                r.Sp10084 == ДатаКвитанции &&
                                (string.IsNullOrEmpty(СтатусПартииId) ? true : r.Sp9963 == СтатусПартииId)
                              group r by new { r.Sp9958, r.Sp9960, r.Sp9961, r.Sp9963, r.Sp9964, r.Sp10083, r.Sp9967 } into gr
                              where gr.Sum(x => x.Sp9970) != 0
                              select new РегистрПартииМастерской
                              {
                                  Гарантия = gr.Key.Sp9958,
                                  Номенклатура = gr.Key.Sp9960,
                                  ЗавНомер = gr.Key.Sp9961,
                                  СтатусПартии = gr.Key.Sp9963,
                                  Контрагент = gr.Key.Sp9964,
                                  СкладОткуда = gr.Key.Sp10083,
                                  ДатаПриема = gr.Key.Sp9967,
                                  НомерКвитанции = НомерКвитанции,
                                  ДатаКвитанции = ДатаКвитанции,
                                  Количество = gr.Sum(x => x.Sp9970)
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
                var регистр = (from rg in _context.Rg9972s
                               where rg.Period == previousRegPeriod &&
                                 rg.Sp9969 == НомерКвитанции &&
                                 rg.Sp10084 == ДатаКвитанции &&
                                 (string.IsNullOrEmpty(СтатусПартииId) ? true : rg.Sp9963 == СтатусПартииId)
                               select new
                               {
                                   Гарантия = rg.Sp9958,
                                   Номенклатура = rg.Sp9960,
                                   ЗавНомер = rg.Sp9961,
                                   СтатусПартии = rg.Sp9963,
                                   Контрагент = rg.Sp9964,
                                   СкладОткуда = rg.Sp10083,
                                   ДатаПриема = rg.Sp9967,
                                   начОстаток = (int)(rg.Sp9970 * 100000),
                                   приход = 0,
                                   расход = 0
                               })
                              .Concat
                              (from ra in _context.Ra9972s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                 ra.Sp9969 == НомерКвитанции &&
                                 ra.Sp10084 == ДатаКвитанции &&
                                 (string.IsNullOrEmpty(СтатусПартииId) ? true : ra.Sp9963 == СтатусПартииId)
                               select new
                               {
                                   Гарантия = ra.Sp9958,
                                   Номенклатура = ra.Sp9960,
                                   ЗавНомер = ra.Sp9961,
                                   СтатусПартии = ra.Sp9963,
                                   Контрагент = ra.Sp9964,
                                   СкладОткуда = ra.Sp10083,
                                   ДатаПриема = ra.Sp9967,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp9970 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp9970 * 100000) : 0
                               });
                return await (from r in регистр
                              group r by new { r.Гарантия, r.Номенклатура, r.ЗавНомер, r.СтатусПартии, r.Контрагент, r.СкладОткуда, r.ДатаПриема } into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0
                              select new РегистрПартииМастерской
                              {
                                  Гарантия = gr.Key.Гарантия,
                                  Номенклатура = gr.Key.Номенклатура,
                                  ЗавНомер = gr.Key.ЗавНомер,
                                  СтатусПартии = gr.Key.СтатусПартии,
                                  Контрагент = gr.Key.Контрагент,
                                  СкладОткуда = gr.Key.СкладОткуда,
                                  ДатаПриема = gr.Key.ДатаПриема,
                                  НомерКвитанции = НомерКвитанции,
                                  ДатаКвитанции = ДатаКвитанции,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000
                              })
                              .ToListAsync();
            }
        }
    }
}
