using StinClasses.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StinClasses.Регистры
{
    public class РегистрЗаявки
    {
        public string ФирмаId { get; set; }
        public string НоменклатураId { get; set; }
        public string ДоговорId { get; set; }
        public string ЗаявкаId { get; set; }
        public decimal Количество { get; set; }
        public decimal Стоимость { get; set; }
    }
    public interface IРегистрЗаявки : IDisposable
    {
        Task<List<РегистрЗаявки>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, string договорId, List<string> номенклатураIds, string заявкаId = null);
        Task<List<РегистрЗаявки>> ПолучитьОстаткиПоЗаявкамAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, string договорId, List<string> номенклатураIds, List<string> заявкаIds);
        Task<List<string>> ПолучитьСписокАктивныхЗаявокAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string orderId);
        Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
                    string ФирмаId,
                    string НоменклатураId,
                    string ДоговорId,
                    string ЗаявкаId,
                    decimal Количество,
                    decimal Стоимость
                    );
    }
    public class Регистр_Заявки : IРегистрЗаявки
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
        public Регистр_Заявки(StinDbContext context)
        {
            _context = context;
        }
        public async Task<List<РегистрЗаявки>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, string договорId, List<string> номенклатураIds, string заявкаId = null)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= _context.GetDateTA())
            {
                DateTime dateRegTA = _context.GetRegTA();
                return await (from r in _context.Rg4674s
                              where r.Period == dateRegTA &&
                                r.Sp4670 == договорId &&
                                (string.IsNullOrEmpty(заявкаId) ? true : r.Sp4671 == заявкаId) &&
                                фирмаIds.Contains(r.Sp4668) &&
                                (((номенклатураIds != null) && (номенклатураIds.Count > 0)) ? номенклатураIds.Contains(r.Sp4669) : true)
                              group r by new { r.Sp4668, r.Sp4669, r.Sp4670, r.Sp4671 } into gr
                              where gr.Sum(x => x.Sp4672) != 0 || gr.Sum(x => x.Sp4673) != 0
                              select new РегистрЗаявки
                              {
                                  ФирмаId = gr.Key.Sp4668,
                                  НоменклатураId = gr.Key.Sp4669,
                                  ДоговорId = gr.Key.Sp4670,
                                  ЗаявкаId = gr.Key.Sp4671,
                                  Количество = gr.Sum(x => x.Sp4672),
                                  Стоимость = gr.Sum(x => x.Sp4673)
                              })
                              .ToListAsync();
            }
            else
            {
                dateReg.GetDateTimeValuesForRegistry(idDocDeadLine, out DateTime previousRegPeriod, out string PeriodStart, out string PeriodEnd);
                var регистр = (from rg in _context.Rg4674s
                               where rg.Period == previousRegPeriod &&
                                    rg.Sp4670 == договорId &&
                                    (string.IsNullOrEmpty(заявкаId) ? true : rg.Sp4671 == заявкаId) &&
                                    фирмаIds.Contains(rg.Sp4668) &&
                                    (((номенклатураIds != null) && (номенклатураIds.Count > 0)) ? номенклатураIds.Contains(rg.Sp4669) : true)
                               select new
                               {
                                   ФирмаId = rg.Sp4668,
                                   НоменклатураId = rg.Sp4669,
                                   ДоговорId = rg.Sp4670,
                                   ЗаявкаId = rg.Sp4671,
                                   начОстаток = (int)(rg.Sp4672 * 100000),
                                   приход = 0,
                                   расход = 0,
                                   начСтоимость = (int)(rg.Sp4673 * 100),
                                   приходСтоимость = 0,
                                   расходСтоимость = 0,
                               })
                              .Concat
                              (from ra in _context.Ra4674s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                    ra.Sp4670 == договорId &&
                                    (string.IsNullOrEmpty(заявкаId) ? true : ra.Sp4671 == заявкаId) &&
                                    фирмаIds.Contains(ra.Sp4668) &&
                                    (((номенклатураIds != null) && (номенклатураIds.Count > 0)) ? номенклатураIds.Contains(ra.Sp4669) : true)
                               select new
                               {
                                   ФирмаId = ra.Sp4668,
                                   НоменклатураId = ra.Sp4669,
                                   ДоговорId = ra.Sp4670,
                                   ЗаявкаId = ra.Sp4671,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp4672 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp4672 * 100000) : 0,
                                   начСтоимость = 0,
                                   приходСтоимость = !ra.Debkred ? (int)(ra.Sp4673 * 100) : 0,
                                   расходСтоимость = ra.Debkred ? (int)(ra.Sp4673 * 100) : 0,
                               });
                return await (from r in регистр
                              group r by new { r.ФирмаId, r.НоменклатураId, r.ДоговорId, r.ЗаявкаId } into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0 ||
                                (gr.Sum(x => x.начСтоимость) + gr.Sum(x => x.приходСтоимость) - gr.Sum(x => x.расходСтоимость)) != 0
                              select new РегистрЗаявки
                              {
                                  ФирмаId = gr.Key.ФирмаId,
                                  НоменклатураId = gr.Key.НоменклатураId,
                                  ДоговорId = gr.Key.ДоговорId,
                                  ЗаявкаId = gr.Key.ЗаявкаId,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000,
                                  Стоимость = (gr.Sum(x => x.начСтоимость) + gr.Sum(x => x.приходСтоимость) - gr.Sum(x => x.расходСтоимость)) / 100
                              })
                              .ToListAsync();
            }
        }
        public async Task<List<РегистрЗаявки>> ПолучитьОстаткиПоЗаявкамAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, string договорId, List<string> номенклатураIds, List<string> заявкаIds)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= _context.GetDateTA())
            {
                DateTime dateRegTA = _context.GetRegTA();
                return await (from r in _context.Rg4674s
                              where r.Period == dateRegTA &&
                                r.Sp4670 == договорId &&
                                (((заявкаIds != null) && (заявкаIds.Count > 0)) ? заявкаIds.Contains(r.Sp4671) : true) &&
                                фирмаIds.Contains(r.Sp4668) &&
                                (((номенклатураIds != null) && (номенклатураIds.Count > 0)) ? номенклатураIds.Contains(r.Sp4669) : true)
                              group r by new { r.Sp4668, r.Sp4669, r.Sp4670, r.Sp4671 } into gr
                              where gr.Sum(x => x.Sp4672) != 0 || gr.Sum(x => x.Sp4673) != 0
                              select new РегистрЗаявки
                              {
                                  ФирмаId = gr.Key.Sp4668,
                                  НоменклатураId = gr.Key.Sp4669,
                                  ДоговорId = gr.Key.Sp4670,
                                  ЗаявкаId = gr.Key.Sp4671,
                                  Количество = gr.Sum(x => x.Sp4672),
                                  Стоимость = gr.Sum(x => x.Sp4673)
                              })
                              .ToListAsync();
            }
            else
            {
                dateReg.GetDateTimeValuesForRegistry(idDocDeadLine, out DateTime previousRegPeriod, out string PeriodStart, out string PeriodEnd);
                var регистр = (from rg in _context.Rg4674s
                               where rg.Period == previousRegPeriod &&
                                    rg.Sp4670 == договорId &&
                                    (((заявкаIds != null) && (заявкаIds.Count > 0)) ? заявкаIds.Contains(rg.Sp4671) : true) &&
                                    фирмаIds.Contains(rg.Sp4668) &&
                                    (((номенклатураIds != null) && (номенклатураIds.Count > 0)) ? номенклатураIds.Contains(rg.Sp4669) : true)
                               select new
                               {
                                   ФирмаId = rg.Sp4668,
                                   НоменклатураId = rg.Sp4669,
                                   ДоговорId = rg.Sp4670,
                                   ЗаявкаId = rg.Sp4671,
                                   начОстаток = (int)(rg.Sp4672 * 100000),
                                   приход = 0,
                                   расход = 0,
                                   начСтоимость = (int)(rg.Sp4673 * 100),
                                   приходСтоимость = 0,
                                   расходСтоимость = 0,
                               })
                              .Concat
                              (from ra in _context.Ra4674s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                    ra.Sp4670 == договорId &&
                                    (((заявкаIds != null) && (заявкаIds.Count > 0)) ? заявкаIds.Contains(ra.Sp4671) : true) &&
                                    фирмаIds.Contains(ra.Sp4668) &&
                                    (((номенклатураIds != null) && (номенклатураIds.Count > 0)) ? номенклатураIds.Contains(ra.Sp4669) : true)
                               select new
                               {
                                   ФирмаId = ra.Sp4668,
                                   НоменклатураId = ra.Sp4669,
                                   ДоговорId = ra.Sp4670,
                                   ЗаявкаId = ra.Sp4671,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp4672 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp4672 * 100000) : 0,
                                   начСтоимость = 0,
                                   приходСтоимость = !ra.Debkred ? (int)(ra.Sp4673 * 100) : 0,
                                   расходСтоимость = ra.Debkred ? (int)(ra.Sp4673 * 100) : 0,
                               });
                return await (from r in регистр
                              group r by new { r.ФирмаId, r.НоменклатураId, r.ДоговорId, r.ЗаявкаId } into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0 ||
                                (gr.Sum(x => x.начСтоимость) + gr.Sum(x => x.приходСтоимость) - gr.Sum(x => x.расходСтоимость)) != 0
                              select new РегистрЗаявки
                              {
                                  ФирмаId = gr.Key.ФирмаId,
                                  НоменклатураId = gr.Key.НоменклатураId,
                                  ДоговорId = gr.Key.ДоговорId,
                                  ЗаявкаId = gr.Key.ЗаявкаId,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000,
                                  Стоимость = (gr.Sum(x => x.начСтоимость) + gr.Sum(x => x.приходСтоимость) - gr.Sum(x => x.расходСтоимость)) / 100
                              })
                              .ToListAsync();
            }
        }
        public async Task<List<string>> ПолучитьСписокАктивныхЗаявокAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string orderId)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= _context.GetDateTA())
            {
                DateTime dateRegTA = _context.GetRegTA();
                return await (from r in _context.Rg4674s
                              join doc in _context.Dh2457s on r.Sp4671 equals doc.Iddoc
                              where r.Period == dateRegTA &&
                                doc.Sp13995 == orderId
                              group r by r.Sp4671 into gr
                              where gr.Sum(x => x.Sp4672) != 0 || gr.Sum(x => x.Sp4673) != 0
                              select gr.Key
                              ).ToListAsync();
            }
            else
            {
                dateReg.GetDateTimeValuesForRegistry(idDocDeadLine, out DateTime previousRegPeriod, out string PeriodStart, out string PeriodEnd);
                var регистр = (from rg in _context.Rg4674s
                               join doc in _context.Dh2457s on rg.Sp4671 equals doc.Iddoc
                               where rg.Period == previousRegPeriod &&
                                    doc.Sp13995 == orderId
                               select new
                               {
                                   ЗаявкаId = rg.Sp4671,
                                   начОстаток = (int)(rg.Sp4672 * 100000),
                                   приход = 0,
                                   расход = 0,
                                   начСтоимость = (int)(rg.Sp4673 * 100),
                                   приходСтоимость = 0,
                                   расходСтоимость = 0,
                               })
                              .Concat
                              (from ra in _context.Ra4674s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               join doc in _context.Dh2457s on ra.Sp4671 equals doc.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                    doc.Sp13995 == orderId 
                               select new
                               {
                                   ЗаявкаId = ra.Sp4671,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp4672 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp4672 * 100000) : 0,
                                   начСтоимость = 0,
                                   приходСтоимость = !ra.Debkred ? (int)(ra.Sp4673 * 100) : 0,
                                   расходСтоимость = ra.Debkred ? (int)(ra.Sp4673 * 100) : 0,
                               });
                return await (from r in регистр
                              group r by r.ЗаявкаId into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0 ||
                                (gr.Sum(x => x.начСтоимость) + gr.Sum(x => x.приходСтоимость) - gr.Sum(x => x.расходСтоимость)) != 0
                              select gr.Key
                              ).ToListAsync();
            }
        }
        public async Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
            string ФирмаId,
            string НоменклатураId,
            string ДоговорId,
            string ЗаявкаId,
            decimal Количество,
            decimal Стоимость
            )
        {
            string startOfMonth = new DateTime(ДатаДок.Year, ДатаДок.Month, 1).ToShortDateString();
            await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA4674_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                "@ФирмаId,@НоменклатураId,@ДоговорId,@ЗаявкаId,@Количество,@Стоимость," +
                "@docDate,@CurPeriod,1,0",
                new SqlParameter("@num36", IdDoc),
                new SqlParameter("@ActNo", КоличествоДвижений),
                new SqlParameter("@DebetCredit", ДвижениеРасход),
                new SqlParameter("@ФирмаId", ФирмаId),
                new SqlParameter("@НоменклатураId", НоменклатураId),
                new SqlParameter("@ДоговорId", ДоговорId),
                new SqlParameter("@ЗаявкаId", ЗаявкаId),
                new SqlParameter("@Количество", Количество),
                new SqlParameter("@Стоимость", Стоимость),
                new SqlParameter("@docDate", ДатаДок.ToShortDateString()),
                new SqlParameter("@CurPeriod", startOfMonth));
            return true;
        }
    }
}
