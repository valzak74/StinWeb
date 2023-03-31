using StinClasses.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StinClasses.Регистры
{
    public class РегистрНаборНаСкладе
    {
        public string ФирмаId { get; set; }
        public string СкладId { get; set; }
        public string ПодСкладId { get; set; }
        public string ДоговорId { get; set; }
        public string НаборId { get; set; }
        public string НоменклатураId { get; set; }
        public decimal Количество { get; set; }
    }
    public interface IРегистрНаборНаСкладе : IDisposable
    {
        Task<List<РегистрНаборНаСкладе>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, string складId, string договорId, List<string> номенклатураIds, string наборId = null);
        Task<List<РегистрНаборНаСкладе>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, string складId, string договорId, List<string> номенклатураIds, List<string> наборIds);
        Task<List<string>> ПолучитьСписокАктивныхНаборовAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string orderId, bool onlyFinished);
        Task<Dictionary<string,decimal>> ПолучитьКоличествоНоменклатурыВНаборахAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, IEnumerable<string> номенклатураIds, string marketplaceId = "");
        Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
                    string ФирмаId,
                    string СкладId,
                    string ПодСкладId,
                    string ДоговорId,
                    string НаборId,
                    string НоменклатураId,
                    decimal Количество
                    );
    }
    public class Регистр_НаборНаСкладе : IРегистрНаборНаСкладе
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
        public Регистр_НаборНаСкладе(StinDbContext context)
        {
            _context = context;
        }
        public async Task<List<РегистрНаборНаСкладе>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, string складId, string договорId, List<string> номенклатураIds, string наборId = null)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= _context.GetDateTA())
            {
                DateTime dateRegTA = _context.GetRegTA();
                return await (from r in _context.Rg11973s
                              where r.Period == dateRegTA &&
                                r.Sp11967 == складId &&
                                r.Sp11969 == договорId &&
                                (string.IsNullOrEmpty(наборId) ? true : r.Sp11970 == наборId) &&
                                фирмаIds.Contains(r.Sp11966) &&
                                номенклатураIds.Contains(r.Sp11971)
                              group r by new { r.Sp11966, r.Sp11967, r.Sp11968, r.Sp11969, r.Sp11970, r.Sp11971 } into gr
                              where gr.Sum(x => x.Sp11972) != 0
                              select new РегистрНаборНаСкладе
                              {
                                  ФирмаId = gr.Key.Sp11966,
                                  СкладId = gr.Key.Sp11967,
                                  ПодСкладId = gr.Key.Sp11968,
                                  ДоговорId = gr.Key.Sp11969,
                                  НаборId = gr.Key.Sp11970,
                                  НоменклатураId = gr.Key.Sp11971,
                                  Количество = gr.Sum(x => x.Sp11972)
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
                var регистр = (from rg in _context.Rg11973s
                               where rg.Period == previousRegPeriod &&
                                    rg.Sp11967 == складId &&
                                    rg.Sp11969 == договорId &&
                                    (string.IsNullOrEmpty(наборId) ? true : rg.Sp11970 == наборId) &&
                                    фирмаIds.Contains(rg.Sp11966) &&
                                    номенклатураIds.Contains(rg.Sp11971)
                               select new
                               {
                                   ФирмаId = rg.Sp11966,
                                   СкладId = rg.Sp11967,
                                   ПодСкладId = rg.Sp11968,
                                   ДоговорId = rg.Sp11969,
                                   НаборId = rg.Sp11970,
                                   НоменклатураId = rg.Sp11971,
                                   начОстаток = (int)(rg.Sp11972 * 100000),
                                   приход = 0,
                                   расход = 0
                               })
                              .Concat
                              (from ra in _context.Ra11973s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                    ra.Sp11967 == складId &&
                                    ra.Sp11969 == договорId &&
                                    (string.IsNullOrEmpty(наборId) ? true : ra.Sp11970 == наборId) &&
                                    фирмаIds.Contains(ra.Sp11966) &&
                                    номенклатураIds.Contains(ra.Sp11971)
                               select new
                               {
                                   ФирмаId = ra.Sp11966,
                                   СкладId = ra.Sp11967,
                                   ПодСкладId = ra.Sp11968,
                                   ДоговорId = ra.Sp11969,
                                   НаборId = ra.Sp11970,
                                   НоменклатураId = ra.Sp11971,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp11972 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp11972 * 100000) : 0
                               });
                return await (from r in регистр
                              group r by new { r.ФирмаId, r.СкладId, r.ПодСкладId, r.НоменклатураId, r.ДоговорId, r.НаборId } into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0
                              select new РегистрНаборНаСкладе
                              {
                                  ФирмаId = gr.Key.ФирмаId,
                                  СкладId = gr.Key.СкладId,
                                  ПодСкладId = gr.Key.ПодСкладId,
                                  НоменклатураId = gr.Key.НоменклатураId,
                                  ДоговорId = gr.Key.ДоговорId,
                                  НаборId = gr.Key.НаборId,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000
                              })
                              .ToListAsync();
            }
        }
        public async Task<List<РегистрНаборНаСкладе>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, string складId, string договорId, List<string> номенклатураIds, List<string> наборIds)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= _context.GetDateTA())
            {
                DateTime dateRegTA = _context.GetRegTA();
                return await (from r in _context.Rg11973s
                              where r.Period == dateRegTA &&
                                r.Sp11967 == складId &&
                                r.Sp11969 == договорId &&
                                наборIds.Contains(r.Sp11970) &&
                                фирмаIds.Contains(r.Sp11966) &&
                                номенклатураIds.Contains(r.Sp11971)
                              group r by new { r.Sp11966, r.Sp11967, r.Sp11968, r.Sp11969, r.Sp11970, r.Sp11971 } into gr
                              where gr.Sum(x => x.Sp11972) != 0
                              select new РегистрНаборНаСкладе
                              {
                                  ФирмаId = gr.Key.Sp11966,
                                  СкладId = gr.Key.Sp11967,
                                  ПодСкладId = gr.Key.Sp11968,
                                  ДоговорId = gr.Key.Sp11969,
                                  НаборId = gr.Key.Sp11970,
                                  НоменклатураId = gr.Key.Sp11971,
                                  Количество = gr.Sum(x => x.Sp11972)
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
                var регистр = (from rg in _context.Rg11973s
                               where rg.Period == previousRegPeriod &&
                                    rg.Sp11967 == складId &&
                                    rg.Sp11969 == договорId &&
                                    наборIds.Contains(rg.Sp11970) &&
                                    фирмаIds.Contains(rg.Sp11966) &&
                                    номенклатураIds.Contains(rg.Sp11971)
                               select new
                               {
                                   ФирмаId = rg.Sp11966,
                                   СкладId = rg.Sp11967,
                                   ПодСкладId = rg.Sp11968,
                                   ДоговорId = rg.Sp11969,
                                   НаборId = rg.Sp11970,
                                   НоменклатураId = rg.Sp11971,
                                   начОстаток = (int)(rg.Sp11972 * 100000),
                                   приход = 0,
                                   расход = 0
                               })
                              .Concat
                              (from ra in _context.Ra11973s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                    ra.Sp11967 == складId &&
                                    ra.Sp11969 == договорId &&
                                    наборIds.Contains(ra.Sp11970) &&
                                    фирмаIds.Contains(ra.Sp11966) &&
                                    номенклатураIds.Contains(ra.Sp11971)
                               select new
                               {
                                   ФирмаId = ra.Sp11966,
                                   СкладId = ra.Sp11967,
                                   ПодСкладId = ra.Sp11968,
                                   ДоговорId = ra.Sp11969,
                                   НаборId = ra.Sp11970,
                                   НоменклатураId = ra.Sp11971,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp11972 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp11972 * 100000) : 0
                               });
                return await (from r in регистр
                              group r by new { r.ФирмаId, r.СкладId, r.ПодСкладId, r.НоменклатураId, r.ДоговорId, r.НаборId } into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0
                              select new РегистрНаборНаСкладе
                              {
                                  ФирмаId = gr.Key.ФирмаId,
                                  СкладId = gr.Key.СкладId,
                                  ПодСкладId = gr.Key.ПодСкладId,
                                  НоменклатураId = gr.Key.НоменклатураId,
                                  ДоговорId = gr.Key.ДоговорId,
                                  НаборId = gr.Key.НаборId,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000
                              })
                              .ToListAsync();
            }
        }
        public async Task<List<string>> ПолучитьСписокАктивныхНаборовAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string orderId, bool onlyFinished)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= _context.GetDateTA())
            {
                DateTime dateRegTA = _context.GetRegTA();
                return await (from r in _context.Rg11973s
                              join doc in _context.Dh11948s on r.Sp11970 equals doc.Iddoc
                              where r.Period == dateRegTA &&
                                (onlyFinished ? doc.Sp11938 == 1 : true) && //завершен
                                doc.Sp14003 == orderId
                              group r by r.Sp11970 into gr
                              where gr.Sum(x => x.Sp11972) != 0
                              select gr.Key
                              ).ToListAsync();
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
                var регистр = (from rg in _context.Rg11973s
                               join doc in _context.Dh11948s on rg.Sp11970 equals doc.Iddoc
                               where rg.Period == previousRegPeriod &&
                                    (onlyFinished ? doc.Sp11938 == 1 : true) && //завершен
                                    doc.Sp14003 == orderId
                               select new
                               {
                                   НаборId = rg.Sp11970,
                                   начОстаток = (int)(rg.Sp11972 * 100000),
                                   приход = 0,
                                   расход = 0
                               })
                              .Concat
                              (from ra in _context.Ra11973s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               join doc in _context.Dh11948s on ra.Sp11970 equals doc.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                    (onlyFinished ? doc.Sp11938 == 1 : true) && //завершен
                                    doc.Sp14003 == orderId
                               select new
                               {
                                   НаборId = ra.Sp11970,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp11972 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp11972 * 100000) : 0
                               });
                return await (from r in регистр
                              group r by r.НаборId into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0
                              select gr.Key
                              ).ToListAsync();
            }
        }
        public async Task<Dictionary<string, decimal>> ПолучитьКоличествоНоменклатурыВНаборахAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, IEnumerable<string> номенклатураIds, string marketplaceId = "")
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= _context.GetDateTA())
            {
                DateTime dateRegTA = _context.GetRegTA();
                return await (from r in _context.Rg11973s
                              join docH in _context.Dh11948s on r.Sp11970 equals docH.Iddoc
                              join doc in _context.Dt11948s on new { idDoc = r.Sp11970, nomId = r.Sp11971 } equals new { idDoc = doc.Iddoc, nomId = doc.Sp11941 }
                              join order in _context.Sc13994s on docH.Sp14003 equals order.Id into _order
                              from order in _order.DefaultIfEmpty()
                              where r.Period == dateRegTA &&
                                (string.IsNullOrEmpty(marketplaceId) ? true : order.Sp14038 == marketplaceId) &&
                                номенклатураIds.Contains(r.Sp11971)
                              group r by r.Sp11971 into gr
                              select new
                              {
                                  nomId = gr.Key,
                                  kol = gr.Sum(x => x.Sp11972)
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
                var регистр = (from rg in _context.Rg11973s
                               join docH in _context.Dh11948s on rg.Sp11970 equals docH.Iddoc
                               join doc in _context.Dt11948s on new { idDoc = rg.Sp11970, nomId = rg.Sp11971 } equals new { idDoc = doc.Iddoc, nomId = doc.Sp11941 }
                               join order in _context.Sc13994s on docH.Sp14003 equals order.Id into _order
                               from order in _order.DefaultIfEmpty()
                               where rg.Period == previousRegPeriod &&
                                (string.IsNullOrEmpty(marketplaceId) ? true : order.Sp14038 == marketplaceId) &&
                                номенклатураIds.Contains(rg.Sp11971)
                               select new
                               {
                                   НоменклатураId = rg.Sp11971,
                                   начОстаток = (int)(rg.Sp11972 * 100000),
                                   приход = 0,
                                   расход = 0
                               })
                              .Concat
                              (from ra in _context.Ra11973s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               join docH in _context.Dh11948s on ra.Sp11970 equals docH.Iddoc
                               join doc in _context.Dt11948s on new { idDoc = ra.Sp11970, nomId = ra.Sp11971 } equals new { idDoc = doc.Iddoc, nomId = doc.Sp11941 }
                               join order in _context.Sc13994s on docH.Sp14003 equals order.Id into _order
                               from order in _order.DefaultIfEmpty()
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                (string.IsNullOrEmpty(marketplaceId) ? true : order.Sp14038 == marketplaceId) &&
                                номенклатураIds.Contains(ra.Sp11971)
                               select new
                               {
                                   НоменклатураId = ra.Sp11971,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp11972 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp11972 * 100000) : 0
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
                    string СкладId,
                    string ПодСкладId,
                    string ДоговорId,
                    string НаборId,
                    string НоменклатураId,
                    decimal Количество
                    )
        {
            string startOfMonth = new DateTime(ДатаДок.Year, ДатаДок.Month, 1).ToShortDateString();
            await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA11973_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                "@ФирмаId,@СкладId,@ПодСкладId,@ДоговорId,@НаборId,@НоменклатураId,@Количество," +
                "@docDate,@CurPeriod,1,0",
                new SqlParameter("@num36", IdDoc),
                new SqlParameter("@ActNo", КоличествоДвижений),
                new SqlParameter("@DebetCredit", ДвижениеРасход),
                new SqlParameter("@ФирмаId", ФирмаId),
                new SqlParameter("@СкладId", СкладId),
                new SqlParameter("@ПодСкладId", ПодСкладId),
                new SqlParameter("@ДоговорId", ДоговорId),
                new SqlParameter("@НаборId", НаборId),
                new SqlParameter("@НоменклатураId", НоменклатураId),
                new SqlParameter("@Количество", Количество),
                new SqlParameter("@docDate", ДатаДок.ToShortDateString()),
                new SqlParameter("@CurPeriod", startOfMonth));
            return true;
        }
    }
}
