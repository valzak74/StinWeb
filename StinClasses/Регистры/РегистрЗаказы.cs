using Microsoft.EntityFrameworkCore;
using StinClasses.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StinClasses.Регистры
{
    public class РегистрЗаказы
    {
        public string ФирмаId { get; set; }
        public string НоменклатураId { get; set; }
        public string ДоговорId { get; set; }
        public string ЗаказId { get; set; }
        public int ТипЗаказа { get; set; }
        public decimal Количество { get; set; }
        public decimal Сумма { get; set; }
    }
    public interface IРегистрЗаказы : IDisposable
    {
        Task<List<РегистрЗаказы>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string заказId, List<string> номенклатураIds, int типЗаказа = -1);
    }
    public class Регистр_Заказы : IРегистрЗаказы
    {
        private StinDbContext _context;
        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public Регистр_Заказы(StinDbContext context)
        {
            _context = context;
        }
        public async Task<List<РегистрЗаказы>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string заказId, List<string> номенклатураIds, int типЗаказа = -1)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= _context.GetDateTA())
            {
                DateTime dateRegTA = _context.GetRegTA();
                return await (from r in _context.Rg464s
                              where r.Period == dateRegTA &&
                                (string.IsNullOrEmpty(заказId) ? true : r.Sp4470 == заказId) &&
                                (типЗаказа < 0 ? true : (int)r.Sp13166 == типЗаказа) &&
                                номенклатураIds.Contains(r.Sp466)
                              group r by new { r.Sp4467, r.Sp466, r.Sp470, r.Sp4470, r.Sp13166 } into gr
                              where gr.Sum(x => x.Sp4471) + gr.Sum(x => x.Sp4472) != 0
                              select new РегистрЗаказы
                              {
                                  ФирмаId = gr.Key.Sp4467,
                                  НоменклатураId = gr.Key.Sp466,
                                  ДоговорId = gr.Key.Sp470,
                                  ЗаказId = gr.Key.Sp4470,
                                  ТипЗаказа = (int)gr.Key.Sp13166,
                                  Количество = gr.Sum(x => x.Sp4471),
                                  Сумма = gr.Sum(x => x.Sp4472)
                              })
                              .ToListAsync();
            }
            else
            {
                dateReg.GetDateTimeValuesForRegistry(idDocDeadLine, out DateTime previousRegPeriod, out string PeriodStart, out string PeriodEnd);
                var регистр = (from rg in _context.Rg464s
                               where rg.Period == previousRegPeriod &&
                                    (string.IsNullOrEmpty(заказId) ? true : rg.Sp4470 == заказId) &&
                                    (типЗаказа < 0 ? true : (int)rg.Sp13166 == типЗаказа) &&
                                    номенклатураIds.Contains(rg.Sp466)
                               select new
                               {
                                   ФирмаId = rg.Sp4467,
                                   НоменклатураId = rg.Sp466,
                                   ДоговорId = rg.Sp470,
                                   ЗаказId = rg.Sp4470,
                                   ТипЗаказа = (int)rg.Sp13166,
                                   начОстаток = (int)(rg.Sp4471 * 100000),
                                   приход = 0,
                                   расход = 0,
                                   начСумма = (int)(rg.Sp4472 * 100),
                                   приходСумма = 0,
                                   расходСумма = 0,
                               })
                              .Concat
                              (from ra in _context.Ra464s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                    (string.IsNullOrEmpty(заказId) ? true : ra.Sp4470 == заказId) &&
                                    (типЗаказа < 0 ? true : (int)ra.Sp13166 == типЗаказа) &&
                                    номенклатураIds.Contains(ra.Sp466)
                               select new
                               {
                                   ФирмаId = ra.Sp4467,
                                   НоменклатураId = ra.Sp466,
                                   ДоговорId = ra.Sp470,
                                   ЗаказId = ra.Sp4470,
                                   ТипЗаказа = (int)ra.Sp13166,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp4471 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp4471 * 100000) : 0,
                                   начСумма = 0,
                                   приходСумма = !ra.Debkred ? (int)(ra.Sp4472 * 100) : 0,
                                   расходСумма = ra.Debkred ? (int)(ra.Sp4472 * 100) : 0,
                               });
                return await (from r in регистр
                              group r by new { r.ФирмаId, r.НоменклатураId, r.ДоговорId, r.ЗаказId, r.ТипЗаказа } into gr
                              where ((gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0) ||
                                ((gr.Sum(x => x.начСумма) + gr.Sum(x => x.приходСумма) - gr.Sum(x => x.расходСумма)) != 0)
                              select new РегистрЗаказы
                              {
                                  ФирмаId = gr.Key.ФирмаId,
                                  НоменклатураId = gr.Key.НоменклатураId,
                                  ДоговорId = gr.Key.ДоговорId,
                                  ЗаказId = gr.Key.ЗаказId,
                                  ТипЗаказа = gr.Key.ТипЗаказа,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000,
                                  Сумма = (gr.Sum(x => x.начСумма) + gr.Sum(x => x.приходСумма) - gr.Sum(x => x.расходСумма)) / 100,
                              })
                              .ToListAsync();
            }
        }
    }
}
