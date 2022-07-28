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
    public class РегистрРаботыНаИзделиях
    {
        public string НомерКвитанции { get; set; }
        public decimal ДатаКвитанции { get; set; }
        public string ИзделиеId { get; set; }
        public string ЗаводскойНомер { get; set; }
        public string МастерId { get; set; }
        public string РаботаId { get; set; }
        public decimal ФлагБензо { get; set; }
        public decimal ДопРаботы { get; set; }
        public decimal Количество { get; set; }
        public decimal Сумма { get; set; }
        public decimal СуммаЗавода { get; set; }
    }
    public class Регистр_РаботыНаИзделиях : IРегистр_РаботыНаИзделиях
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
        public Регистр_РаботыНаИзделиях(StinDbContext context)
        {
            this._context = context;
        }
        public async Task<List<РегистрРаботыНаИзделиях>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string НомерКвитанции, decimal ДатаКвитанции)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= Common.GetDateTA(_context))
            {
                DateTime dateRegTA = Common.GetRegTA(_context);
                return await (from r in _context.Rg9989s
                              where r.Period == dateRegTA &&
                                r.Sp9982 == НомерКвитанции &&
                                r.Sp10088 == ДатаКвитанции 
                              group r by new { r.Sp9983, r.Sp9984, r.Sp9985, r.Sp9986, r.Sp10565, r.Sp11293 } into gr
                              where gr.Sum(x => x.Sp10771) != 0
                              select new РегистрРаботыНаИзделиях
                              {
                                  НомерКвитанции = НомерКвитанции,
                                  ДатаКвитанции = ДатаКвитанции,
                                  ИзделиеId = gr.Key.Sp9983,
                                  ЗаводскойНомер = gr.Key.Sp9984,
                                  МастерId = gr.Key.Sp9985,
                                  РаботаId = gr.Key.Sp9986,
                                  ФлагБензо = gr.Key.Sp10565,
                                  ДопРаботы = gr.Key.Sp11293,
                                  Количество = gr.Sum(x => x.Sp10771),
                                  Сумма = gr.Sum(x => x.Sp9987),
                                  СуммаЗавода = gr.Sum(x => x.Sp9988)
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
                var регистр = (from rg in _context.Rg9989s
                               where rg.Period == previousRegPeriod &&
                                 rg.Sp9982 == НомерКвитанции &&
                                 rg.Sp10088 == ДатаКвитанции
                               select new
                               {
                                   ИзделиеId = rg.Sp9983,
                                   ЗаводскойНомер = rg.Sp9984,
                                   МастерId = rg.Sp9985,
                                   РаботаId = rg.Sp9986,
                                   ФлагБензо = rg.Sp10565,
                                   ДопРаботы = rg.Sp11293,
                                   начОстаток = (int)(rg.Sp10771 * 100000),
                                   приход = 0,
                                   расход = 0,
                                   начСумма = (int)(rg.Sp9987 * 100),
                                   приходСумма = 0,
                                   расходСумма = 0,
                                   начСуммаЗавода = (int)(rg.Sp9988 * 100),
                                   приходСуммаЗавода = 0,
                                   расходСуммаЗавода = 0
                               })
                              .Concat
                              (from ra in _context.Ra9989s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                 ra.Sp9982 == НомерКвитанции &&
                                 ra.Sp10088 == ДатаКвитанции
                               select new
                               {
                                   ИзделиеId = ra.Sp9983,
                                   ЗаводскойНомер = ra.Sp9984,
                                   МастерId = ra.Sp9985,
                                   РаботаId = ra.Sp9986,
                                   ФлагБензо = ra.Sp10565,
                                   ДопРаботы = ra.Sp11293,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp10771 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp10771 * 100000) : 0,
                                   начСумма = 0,
                                   приходСумма = !ra.Debkred ? (int)(ra.Sp9987 * 100000) : 0,
                                   расходСумма = ra.Debkred ? (int)(ra.Sp9987 * 100000) : 0,
                                   начСуммаЗавода = 0,
                                   приходСуммаЗавода = !ra.Debkred ? (int)(ra.Sp9988 * 100000) : 0,
                                   расходСуммаЗавода = ra.Debkred ? (int)(ra.Sp9988 * 100000) : 0,
                               });
                return await (from r in регистр
                              group r by new { r.ИзделиеId, r.ЗаводскойНомер, r.МастерId, r.РаботаId, r.ФлагБензо, r.ДопРаботы } into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0
                              select new РегистрРаботыНаИзделиях
                              {
                                  НомерКвитанции = НомерКвитанции,
                                  ДатаКвитанции = ДатаКвитанции,
                                  ИзделиеId = gr.Key.ИзделиеId,
                                  ЗаводскойНомер = gr.Key.ЗаводскойНомер,
                                  МастерId = gr.Key.МастерId,
                                  РаботаId = gr.Key.РаботаId,
                                  ФлагБензо = gr.Key.ФлагБензо,
                                  ДопРаботы = gr.Key.ДопРаботы,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000,
                                  Сумма = (gr.Sum(x => x.начСумма) + gr.Sum(x => x.приходСумма) - gr.Sum(x => x.расходСумма)) / 100,
                                  СуммаЗавода = (gr.Sum(x => x.начСуммаЗавода) + gr.Sum(x => x.приходСуммаЗавода) - gr.Sum(x => x.расходСуммаЗавода)) / 100
                              })
                              .ToListAsync();
            }
        }
    }
}
