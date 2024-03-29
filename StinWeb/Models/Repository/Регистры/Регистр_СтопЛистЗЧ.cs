﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StinWeb.Models.Repository.Интерфейсы.Регистры;
using StinWeb.Models.DataManager;
using StinClasses.Models;

namespace StinWeb.Models.Repository.Регистры
{
    public class РегистрСтопЛистЗЧ
    {
        public string НоменклатураId { get; set; }
        public string СкладId { get; set; }
        public string НомерКвитанции { get; set; }
        public decimal ДатаКвитанции { get; set; }
        public decimal Гарантия { get; set; }
        public string ДокРезультатId { get; set; }
        public decimal Количество { get; set; }
    }
    public class Регистр_СтопЛистЗЧ : IРегистр_СтопЛистЗЧ
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
        public Регистр_СтопЛистЗЧ(StinDbContext context)
        {
            this._context = context;
        }
        public async Task<List<РегистрСтопЛистЗЧ>> ВыбратьДвиженияДокумента(string idDoc)
        {
            return await _context.Ra11055s
                .Where(x => x.Iddoc == idDoc)
                .Select(x => new РегистрСтопЛистЗЧ
                {
                    НоменклатураId = x.Sp11050,
                    СкладId = x.Sp11051,
                    НомерКвитанции = x.Sp11052,
                    ДатаКвитанции = x.Sp11053,
                    Гарантия = x.Sp11060,
                    ДокРезультатId = x.Sp11061,
                    Количество = x.Debkred ? -x.Sp11054 : x.Sp11054
                })
                .ToListAsync();
        }
        public async Task<List<РегистрСтопЛистЗЧ>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string НомерКвитанции, decimal ДатаКвитанции)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= Common.GetDateTA(_context))
            {
                DateTime dateRegTA = Common.GetRegTA(_context);
                return await (from r in _context.Rg11055s
                              where r.Period == dateRegTA &&
                                r.Sp11052 == НомерКвитанции &&
                                r.Sp11053 == ДатаКвитанции
                              group r by new { r.Sp11050, r.Sp11051, r.Sp11060, r.Sp11061 } into gr
                              where gr.Sum(x => x.Sp11054) != 0
                              select new РегистрСтопЛистЗЧ
                              {
                                  НоменклатураId = gr.Key.Sp11050,
                                  СкладId = gr.Key.Sp11051,
                                  НомерКвитанции = НомерКвитанции,
                                  ДатаКвитанции = ДатаКвитанции,
                                  Гарантия = gr.Key.Sp11060,
                                  ДокРезультатId = gr.Key.Sp11061,
                                  Количество = gr.Sum(x => x.Sp11054)
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
                var регистр = (from rg in _context.Rg11055s
                               where rg.Period == previousRegPeriod &&
                                 rg.Sp11052 == НомерКвитанции &&
                                 rg.Sp11053 == ДатаКвитанции
                               select new
                               {
                                   НоменклатураId = rg.Sp11050,
                                   СкладId = rg.Sp11051,
                                   Гарантия = rg.Sp11060,
                                   ДокРезультатId = rg.Sp11061,
                                   начОстаток = (int)(rg.Sp11054 * 100000),
                                   приход = 0,
                                   расход = 0
                               })
                              .Concat
                              (from ra in _context.Ra11055s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                 ra.Sp11052 == НомерКвитанции &&
                                 ra.Sp11053 == ДатаКвитанции
                               select new
                               {
                                   НоменклатураId = ra.Sp11050,
                                   СкладId = ra.Sp11051,
                                   Гарантия = ra.Sp11060,
                                   ДокРезультатId = ra.Sp11061,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp11054 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp11054 * 100000) : 0
                               });
                return await (from r in регистр
                              group r by new { r.НоменклатураId, r.СкладId, r.Гарантия, r.ДокРезультатId } into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0
                              select new РегистрСтопЛистЗЧ
                              {
                                  НоменклатураId = gr.Key.НоменклатураId,
                                  СкладId = gr.Key.СкладId,
                                  НомерКвитанции = НомерКвитанции,
                                  ДатаКвитанции = ДатаКвитанции,
                                  Гарантия = gr.Key.Гарантия,
                                  ДокРезультатId = gr.Key.ДокРезультатId,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000
                              })
                              .ToListAsync();
            }
        }
    }
}
