using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using StinClasses.Models;

namespace StinClasses.Регистры
{
    public class РегистрСпросОстатки
    {
        public string ФирмаId { get; set; }
        public string СкладId { get; set; }
        public string КонтрагентId { get; set; }
        public string НоменклатураId { get; set; }
        public decimal Количество { get; set; }
    }
    public interface IРегистрСпросОстатки : IDisposable
    {
        Task<List<РегистрСпросОстатки>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string фирмаId, string складId, string контрагентId);
        Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
            string НоменклатураId,
            string СкладId,
            string КонтрагентId,
            decimal Количество,
            decimal Стоимость
            );
        Task<bool> ВыполнитьДвижениеОстаткиAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
            string ФирмаId,
            string НоменклатураId,
            string СкладId,
            string КонтрагентId,
            decimal Количество
            );
    }
    public class Регистр_СпросОстатки : IРегистрСпросОстатки
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
        public Регистр_СпросОстатки(StinDbContext context)
        {
            _context = context;
        }
        public async Task<List<РегистрСпросОстатки>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string фирмаId, string складId, string контрагентId)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= _context.GetDateTA())
            {
                DateTime dateRegTA = _context.GetRegTA();
                return await (from r in _context.Rg12815s
                              where r.Period == dateRegTA &&
                                (string.IsNullOrEmpty(складId) ? true : r.Sp12813 == складId) &&
                                (string.IsNullOrEmpty(фирмаId) ? true : r.Sp12818 == фирмаId) &&
                                (string.IsNullOrEmpty(контрагентId) ? true : r.Sp12812 == контрагентId)
                              group r by new { r.Sp12818, r.Sp12813, r.Sp12812, r.Sp12811 } into gr
                              where gr.Sum(x => x.Sp12814) != 0
                              select new РегистрСпросОстатки
                              {
                                  ФирмаId = gr.Key.Sp12818,
                                  НоменклатураId = gr.Key.Sp12811,
                                  СкладId = gr.Key.Sp12813,
                                  КонтрагентId = gr.Key.Sp12812,
                                  Количество = gr.Sum(x => x.Sp12814),
                              })
                              .ToListAsync();
            }
            else
            {
                dateReg.GetDateTimeValuesForRegistry(idDocDeadLine, out DateTime previousRegPeriod, out string PeriodStart, out string PeriodEnd);
                var регистр = (from rg in _context.Rg12815s
                               where rg.Period == previousRegPeriod &&
                                (string.IsNullOrEmpty(складId) ? true : rg.Sp12813 == складId) &&
                                (string.IsNullOrEmpty(фирмаId) ? true : rg.Sp12818 == фирмаId) &&
                                (string.IsNullOrEmpty(контрагентId) ? true : rg.Sp12812 == контрагентId)
                               select new
                               {
                                   ФирмаId = rg.Sp12818,
                                   НоменклатураId = rg.Sp12811,
                                   СкладId = rg.Sp12813,
                                   КонтрагентId = rg.Sp12812,
                                   начОстаток = (int)(rg.Sp12814 * 100000),
                                   приход = 0,
                                   расход = 0,
                               })
                              .Concat
                              (from ra in _context.Ra12815s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                (string.IsNullOrEmpty(складId) ? true : ra.Sp12813 == складId) &&
                                (string.IsNullOrEmpty(фирмаId) ? true : ra.Sp12818 == фирмаId) &&
                                (string.IsNullOrEmpty(контрагентId) ? true : ra.Sp12812 == контрагентId)
                               select new
                               {
                                   ФирмаId = ra.Sp12818,
                                   НоменклатураId = ra.Sp12811,
                                   СкладId = ra.Sp12813,
                                   КонтрагентId = ra.Sp12812,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp12814 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp12814 * 100000) : 0,
                               });
                return await (from r in регистр
                              group r by new { r.ФирмаId, r.НоменклатураId, r.СкладId, r.КонтрагентId } into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0
                              select new РегистрСпросОстатки
                              {
                                  ФирмаId = gr.Key.ФирмаId,
                                  НоменклатураId = gr.Key.НоменклатураId,
                                  СкладId = gr.Key.СкладId,
                                  КонтрагентId = gr.Key.КонтрагентId,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000,
                              })
                              .ToListAsync();
            }
        }
        public async Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
            string НоменклатураId,
            string СкладId,
            string КонтрагентId,
            decimal Количество,
            decimal Стоимость
            )
        {
            string startOfMonth = new DateTime(ДатаДок.Year, ДатаДок.Month, 1).ToShortDateString();
            await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA12791_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                "@Номенклатура,@Покупатель,@Склад,@Количество,@Стоимость," +
                "@docDate,@CurPeriod,1,0",
                new SqlParameter("@num36", IdDoc),
                new SqlParameter("@ActNo", КоличествоДвижений),
                new SqlParameter("@DebetCredit", ДвижениеРасход),
                new SqlParameter("@Номенклатура", НоменклатураId),
                new SqlParameter("@Покупатель", КонтрагентId),
                new SqlParameter("@Склад", СкладId),
                new SqlParameter("@Количество", Количество),
                new SqlParameter("@Стоимость", Стоимость),
                new SqlParameter("@docDate", ДатаДок.ToShortDateString()),
                new SqlParameter("@CurPeriod", startOfMonth));
            return true;
        }
        public async Task<bool> ВыполнитьДвижениеОстаткиAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
            string ФирмаId,
            string НоменклатураId,
            string СкладId,
            string КонтрагентId,
            decimal Количество
            )
        {
            string startOfMonth = new DateTime(ДатаДок.Year, ДатаДок.Month, 1).ToShortDateString();
            await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA12815_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                "@Номенклатура,@Покупатель,@Склад,@Фирма,@Количество," +
                "@docDate,@CurPeriod,1,0",
                new SqlParameter("@num36", IdDoc),
                new SqlParameter("@ActNo", КоличествоДвижений),
                new SqlParameter("@DebetCredit", ДвижениеРасход),
                new SqlParameter("@Номенклатура", НоменклатураId),
                new SqlParameter("@Покупатель", КонтрагентId),
                new SqlParameter("@Склад", СкладId),
                new SqlParameter("@Фирма", ФирмаId),
                new SqlParameter("@Количество", Количество),
                new SqlParameter("@docDate", ДатаДок.ToShortDateString()),
                new SqlParameter("@CurPeriod", startOfMonth));
            return true;
        }
    }
}
