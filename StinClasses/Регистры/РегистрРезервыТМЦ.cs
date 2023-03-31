using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using StinClasses.Models;

namespace StinClasses.Регистры
{
    public class РегистрРезервыТМЦ
    {
        public string ФирмаId { get; set; }
        public string НоменклатураId { get; set; }
        public string СкладId { get; set; }
        public string ДоговорId { get; set; }
        public string ЗаявкаId { get; set; }
        public decimal Количество { get; set; }
    }
    public interface IРегистрРезервыТМЦ : IDisposable
    {
        Task<List<РегистрРезервыТМЦ>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, string договорId, List<string> номенклатураIds, List<string> складIds = null, string заявкаId = null);
        Task<List<РегистрРезервыТМЦ>> ПолучитьОстаткиПоЗаявкамAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, string договорId, List<string> номенклатураIds, List<string> заявкаIds, List<string> складIds = null);
        Task<Dictionary<string, decimal>> ПолучитьКоличествоНоменклатурыВРезервахAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, IEnumerable<string> номенклатураIds, string marketplaceId = "");
        Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
                    string ФирмаId,
                    string НоменклатураId,
                    string СкладId,
                    string ДоговорId,
                    string ЗаявкаId,
                    decimal Количество
                    );
    }
    public class Регистр_РезервыТМЦ : IРегистрРезервыТМЦ
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
        public Регистр_РезервыТМЦ(StinDbContext context)
        {
            _context = context;
        }
        public async Task<List<РегистрРезервыТМЦ>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, string договорId, List<string> номенклатураIds, List<string> складIds = null, string заявкаId = null)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= _context.GetDateTA())
            {
                DateTime dateRegTA = _context.GetRegTA();
                return await (from r in _context.Rg4480s
                              where r.Period == dateRegTA &&
                                (складIds != null ? true : складIds.Contains(r.Sp4476)) &&
                                (string.IsNullOrEmpty(договорId) ? true : r.Sp4519 == договорId) &&
                                (string.IsNullOrEmpty(заявкаId) ? true : r.Sp4762 == заявкаId) &&
                                фирмаIds.Contains(r.Sp4475) &&
                                (((номенклатураIds != null) && (номенклатураIds.Count > 0)) ? номенклатураIds.Contains(r.Sp4477) : true)
                              group r by new { r.Sp4475, r.Sp4477, r.Sp4476, r.Sp4762, r.Sp4519 } into gr
                              where gr.Sum(x => x.Sp4479) != 0
                              select new РегистрРезервыТМЦ
                              {
                                  ФирмаId = gr.Key.Sp4475,
                                  НоменклатураId = gr.Key.Sp4477,
                                  СкладId = gr.Key.Sp4476,
                                  ДоговорId = gr.Key.Sp4519,
                                  ЗаявкаId = gr.Key.Sp4762,
                                  Количество = gr.Sum(x => x.Sp4479),
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
                var регистр = (from rg in _context.Rg4480s
                               where rg.Period == previousRegPeriod &&
                                    (складIds != null ? true : складIds.Contains(rg.Sp4476)) &&
                                    (string.IsNullOrEmpty(договорId) ? true : rg.Sp4519 == договорId) &&
                                    (string.IsNullOrEmpty(заявкаId) ? true : rg.Sp4762 == заявкаId) &&
                                    фирмаIds.Contains(rg.Sp4475) &&
                                    (((номенклатураIds != null) && (номенклатураIds.Count > 0)) ? номенклатураIds.Contains(rg.Sp4477) : true)
                               select new
                               {
                                   ФирмаId = rg.Sp4475,
                                   НоменклатураId = rg.Sp4477,
                                   СкладId = rg.Sp4476,
                                   ДоговорId = rg.Sp4519,
                                   ЗаявкаId = rg.Sp4762,
                                   начОстаток = (int)(rg.Sp4479 * 100000),
                                   приход = 0,
                                   расход = 0,
                               })
                              .Concat
                              (from ra in _context.Ra4480s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                    (складIds != null ? true : складIds.Contains(ra.Sp4476)) &&
                                    (string.IsNullOrEmpty(договорId) ? true : ra.Sp4519 == договорId) &&
                                    (string.IsNullOrEmpty(заявкаId) ? true : ra.Sp4762 == заявкаId) &&
                                    фирмаIds.Contains(ra.Sp4475) &&
                                    (((номенклатураIds != null) && (номенклатураIds.Count > 0)) ? номенклатураIds.Contains(ra.Sp4477) : true)
                               select new
                               {
                                   ФирмаId = ra.Sp4475,
                                   НоменклатураId = ra.Sp4477,
                                   СкладId = ra.Sp4476,
                                   ДоговорId = ra.Sp4519,
                                   ЗаявкаId = ra.Sp4762,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp4479 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp4479 * 100000) : 0,
                               });
                return await (from r in регистр
                              group r by new { r.ФирмаId, r.НоменклатураId, r.СкладId, r.ДоговорId, r.ЗаявкаId } into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0
                              select new РегистрРезервыТМЦ
                              {
                                  ФирмаId = gr.Key.ФирмаId,
                                  НоменклатураId = gr.Key.НоменклатураId,
                                  СкладId = gr.Key.СкладId,
                                  ДоговорId = gr.Key.ДоговорId,
                                  ЗаявкаId = gr.Key.ЗаявкаId,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000,
                              })
                              .ToListAsync();
            }
        }
        public async Task<List<РегистрРезервыТМЦ>> ПолучитьОстаткиПоЗаявкамAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, string договорId, List<string> номенклатураIds, List<string> заявкаIds, List<string> складIds = null)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= _context.GetDateTA())
            {
                DateTime dateRegTA = _context.GetRegTA();
                return await (from r in _context.Rg4480s
                              where r.Period == dateRegTA &&
                                (складIds != null ? true : складIds.Contains(r.Sp4476)) &&
                                (string.IsNullOrEmpty(договорId) ? true : r.Sp4519 == договорId) &&
                                (((заявкаIds != null) && (заявкаIds.Count > 0)) ? заявкаIds.Contains(r.Sp4762) : true) &&
                                фирмаIds.Contains(r.Sp4475) &&
                                (((номенклатураIds != null) && (номенклатураIds.Count > 0)) ? номенклатураIds.Contains(r.Sp4477) : true)
                              group r by new { r.Sp4475, r.Sp4477, r.Sp4476, r.Sp4762, r.Sp4519 } into gr
                              where gr.Sum(x => x.Sp4479) != 0
                              select new РегистрРезервыТМЦ
                              {
                                  ФирмаId = gr.Key.Sp4475,
                                  НоменклатураId = gr.Key.Sp4477,
                                  СкладId = gr.Key.Sp4476,
                                  ДоговорId = gr.Key.Sp4519,
                                  ЗаявкаId = gr.Key.Sp4762,
                                  Количество = gr.Sum(x => x.Sp4479),
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
                var регистр = (from rg in _context.Rg4480s
                               where rg.Period == previousRegPeriod &&
                                    (складIds != null ? true : складIds.Contains(rg.Sp4476)) &&
                                    (string.IsNullOrEmpty(договорId) ? true : rg.Sp4519 == договорId) &&
                                    (((заявкаIds != null) && (заявкаIds.Count > 0)) ? заявкаIds.Contains(rg.Sp4762) : true) &&
                                    фирмаIds.Contains(rg.Sp4475) &&
                                    (((номенклатураIds != null) && (номенклатураIds.Count > 0)) ? номенклатураIds.Contains(rg.Sp4477) : true)
                               select new
                               {
                                   ФирмаId = rg.Sp4475,
                                   НоменклатураId = rg.Sp4477,
                                   СкладId = rg.Sp4476,
                                   ДоговорId = rg.Sp4519,
                                   ЗаявкаId = rg.Sp4762,
                                   начОстаток = (int)(rg.Sp4479 * 100000),
                                   приход = 0,
                                   расход = 0,
                               })
                              .Concat
                              (from ra in _context.Ra4480s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                    (складIds != null ? true : складIds.Contains(ra.Sp4476)) &&
                                    (string.IsNullOrEmpty(договорId) ? true : ra.Sp4519 == договорId) &&
                                    (((заявкаIds != null) && (заявкаIds.Count > 0)) ? заявкаIds.Contains(ra.Sp4762) : true) &&
                                    фирмаIds.Contains(ra.Sp4475) &&
                                    (((номенклатураIds != null) && (номенклатураIds.Count > 0)) ? номенклатураIds.Contains(ra.Sp4477) : true)
                               select new
                               {
                                   ФирмаId = ra.Sp4475,
                                   НоменклатураId = ra.Sp4477,
                                   СкладId = ra.Sp4476,
                                   ДоговорId = ra.Sp4519,
                                   ЗаявкаId = ra.Sp4762,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp4479 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp4479 * 100000) : 0,
                               });
                return await (from r in регистр
                              group r by new { r.ФирмаId, r.НоменклатураId, r.СкладId, r.ДоговорId, r.ЗаявкаId } into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0
                              select new РегистрРезервыТМЦ
                              {
                                  ФирмаId = gr.Key.ФирмаId,
                                  НоменклатураId = gr.Key.НоменклатураId,
                                  СкладId = gr.Key.СкладId,
                                  ДоговорId = gr.Key.ДоговорId,
                                  ЗаявкаId = gr.Key.ЗаявкаId,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000,
                              })
                              .ToListAsync();
            }
        }
        public async Task<Dictionary<string, decimal>> ПолучитьКоличествоНоменклатурыВРезервахAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, IEnumerable<string> номенклатураIds, string marketplaceId = "")
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= _context.GetDateTA())
            {
                DateTime dateRegTA = _context.GetRegTA();
                return await (from r in _context.Rg4480s
                              join doc in _context.Dh2457s on r.Sp4762 equals doc.Iddoc
                              join order in _context.Sc13994s on doc.Sp13995 equals order.Id into _order
                              from order in _order.DefaultIfEmpty()
                              where r.Period == dateRegTA &&
                                (string.IsNullOrEmpty(marketplaceId) ? true : order.Sp14038 == marketplaceId) &&
                                номенклатураIds.Contains(r.Sp4477)
                              group r by r.Sp4477 into gr
                              select new
                              {
                                  nomId = gr.Key,
                                  kol = gr.Sum(x => x.Sp4479)
                              }).ToDictionaryAsync(k => k.nomId, v => v.kol);
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
                var регистр = (from rg in _context.Rg4480s
                               join doc in _context.Dh2457s on rg.Sp4762 equals doc.Iddoc
                               join order in _context.Sc13994s on doc.Sp13995 equals order.Id into _order
                               from order in _order.DefaultIfEmpty()
                               where rg.Period == previousRegPeriod &&
                                (string.IsNullOrEmpty(marketplaceId) ? true : order.Sp14038 == marketplaceId) &&
                                номенклатураIds.Contains(rg.Sp4477)
                               select new
                               {
                                   НоменклатураId = rg.Sp4477,
                                   начОстаток = (int)(rg.Sp4479 * 100000),
                                   приход = 0,
                                   расход = 0
                               })
                              .Concat
                              (from ra in _context.Ra4480s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               join doc in _context.Dh2457s on ra.Sp4762 equals doc.Iddoc
                               join order in _context.Sc13994s on doc.Sp13995 equals order.Id into _order
                               from order in _order.DefaultIfEmpty()
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                (string.IsNullOrEmpty(marketplaceId) ? true : order.Sp14038 == marketplaceId) &&
                                номенклатураIds.Contains(ra.Sp4477)
                               select new
                               {
                                   НоменклатураId = ra.Sp4477,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp4479 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp4479 * 100000) : 0
                               });
                return await (from r in регистр
                              group r by r.НоменклатураId into gr
                              select new
                              {
                                  nomId = gr.Key,
                                  kol = (decimal)(gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000
                              }).ToDictionaryAsync(k => k.nomId, v => v.kol);
            }
        }
        public async Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
            string ФирмаId,
            string НоменклатураId,
            string СкладId,
            string ДоговорId,
            string ЗаявкаId,
            decimal Количество
            )
        {
            string startOfMonth = new DateTime(ДатаДок.Year, ДатаДок.Month, 1).ToShortDateString();
            await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA4480_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                "@ФирмаId,@НоменклатураId,@СкладId,@ДоговорId,@ЗаявкаId,@Количество," +
                "@docDate,@CurPeriod,1,0",
                new SqlParameter("@num36", IdDoc),
                new SqlParameter("@ActNo", КоличествоДвижений),
                new SqlParameter("@DebetCredit", ДвижениеРасход),
                new SqlParameter("@ФирмаId", ФирмаId),
                new SqlParameter("@НоменклатураId", НоменклатураId),
                new SqlParameter("@СкладId", СкладId),
                new SqlParameter("@ДоговорId", ДоговорId),
                new SqlParameter("@ЗаявкаId", ЗаявкаId),
                new SqlParameter("@Количество", Количество),
                new SqlParameter("@docDate", ДатаДок.ToShortDateString()),
                new SqlParameter("@CurPeriod", startOfMonth));
            return true;
        }
    }
}
