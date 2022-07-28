using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using StinClasses.Models;

namespace StinClasses.Регистры
{
    public class РегистрОстаткиТМЦ
    {
        public string ФирмаId { get; set; }
        public string НоменклатураId { get; set; }
        public string СкладId { get; set; }
        public string ПодСкладId { get; set; }
        public decimal Количество { get; set; }
    }
    public interface IРегистрОстаткиТМЦ : IDisposable
    {
        Task<List<РегистрОстаткиТМЦ>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, List<string> номенклатураIds, string складId = null, string подСкладId = null);
        Task<List<РегистрОстаткиТМЦ>> ПолучитьОстаткиПоСпискуСкладовAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, List<string> номенклатураIds, List<string> складIds);
        Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
                    string ФирмаId,
                    string НоменклатураId,
                    string СкладId,
                    string ПодСкладId,
                    decimal ЦенаПрод,
                    decimal Количество,
                    decimal Внутреннее
                    );
    }
    public class Регистр_ОстаткиТМЦ : IРегистрОстаткиТМЦ
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
        public Регистр_ОстаткиТМЦ(StinDbContext context)
        {
            _context = context;
        }
        public async Task<List<РегистрОстаткиТМЦ>> ПолучитьОстаткиПоСпискуСкладовAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, List<string> номенклатураIds, List<string> складIds)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= _context.GetDateTA())
            {
                DateTime dateRegTA = _context.GetRegTA();
                return await (from r in _context.Rg405s
                              where r.Period == dateRegTA &&
                                складIds.Contains(r.Sp418) &&
                                фирмаIds.Contains(r.Sp4062) &&
                                номенклатураIds.Contains(r.Sp408)
                              group r by new { r.Sp4062, r.Sp408, r.Sp418, r.Sp9139 } into gr
                              where gr.Sum(x => x.Sp411) != 0
                              select new РегистрОстаткиТМЦ
                              {
                                  ФирмаId = gr.Key.Sp4062,
                                  НоменклатураId = gr.Key.Sp408,
                                  СкладId = gr.Key.Sp418,
                                  ПодСкладId = gr.Key.Sp9139,
                                  Количество = gr.Sum(x => x.Sp411),
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
                var регистр = (from rg in _context.Rg405s
                               where rg.Period == previousRegPeriod &&
                                    складIds.Contains(rg.Sp418) &&
                                    фирмаIds.Contains(rg.Sp4062) &&
                                    номенклатураIds.Contains(rg.Sp408)
                               select new
                               {
                                   ФирмаId = rg.Sp4062,
                                   НоменклатураId = rg.Sp408,
                                   СкладId = rg.Sp418,
                                   ПодСкладId = rg.Sp9139,
                                   начОстаток = (int)(rg.Sp411 * 100000),
                                   приход = 0,
                                   расход = 0,
                               })
                              .Concat
                              (from ra in _context.Ra405s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                    складIds.Contains(ra.Sp418) &&
                                    фирмаIds.Contains(ra.Sp4062) &&
                                    номенклатураIds.Contains(ra.Sp408)
                               select new
                               {
                                   ФирмаId = ra.Sp4062,
                                   НоменклатураId = ra.Sp408,
                                   СкладId = ra.Sp418,
                                   ПодСкладId = ra.Sp9139,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp411 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp411 * 100000) : 0,
                               });
                return await (from r in регистр
                              group r by new { r.ФирмаId, r.НоменклатураId, r.СкладId, r.ПодСкладId } into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0
                              select new РегистрОстаткиТМЦ
                              {
                                  ФирмаId = gr.Key.ФирмаId,
                                  НоменклатураId = gr.Key.НоменклатураId,
                                  СкладId = gr.Key.СкладId,
                                  ПодСкладId = gr.Key.ПодСкладId,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000,
                              })
                              .ToListAsync();
            }
        }
        public async Task<List<РегистрОстаткиТМЦ>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, List<string> номенклатураIds, string складId = null, string подСкладId = null)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= _context.GetDateTA())
            {
                DateTime dateRegTA = _context.GetRegTA();
                return await (from r in _context.Rg405s
                              where r.Period == dateRegTA &&
                                (string.IsNullOrEmpty(складId) ? true : r.Sp418 == складId) &&
                                (string.IsNullOrEmpty(подСкладId) ? true : r.Sp9139 == подСкладId) &&
                                фирмаIds.Contains(r.Sp4062) &&
                                номенклатураIds.Contains(r.Sp408)
                              group r by new { r.Sp4062, r.Sp408, r.Sp418, r.Sp9139 } into gr
                              where gr.Sum(x => x.Sp411) != 0
                              select new РегистрОстаткиТМЦ
                              {
                                  ФирмаId = gr.Key.Sp4062,
                                  НоменклатураId = gr.Key.Sp408,
                                  СкладId = gr.Key.Sp418,
                                  ПодСкладId = gr.Key.Sp9139,
                                  Количество = gr.Sum(x => x.Sp411),
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
                var регистр = (from rg in _context.Rg405s
                               where rg.Period == previousRegPeriod &&
                                    (string.IsNullOrEmpty(складId) ? true : rg.Sp418 == складId) &&
                                    (string.IsNullOrEmpty(подСкладId) ? true : rg.Sp9139 == подСкладId) &&
                                    фирмаIds.Contains(rg.Sp4062) &&
                                    номенклатураIds.Contains(rg.Sp408)
                               select new
                               {
                                   ФирмаId = rg.Sp4062,
                                   НоменклатураId = rg.Sp408,
                                   СкладId = rg.Sp418,
                                   ПодСкладId = rg.Sp9139,
                                   начОстаток = (int)(rg.Sp411 * 100000),
                                   приход = 0,
                                   расход = 0,
                               })
                              .Concat
                              (from ra in _context.Ra405s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                    (string.IsNullOrEmpty(складId) ? true : ra.Sp418 == складId) &&
                                    (string.IsNullOrEmpty(подСкладId) ? true : ra.Sp9139 == подСкладId) &&
                                    фирмаIds.Contains(ra.Sp4062) &&
                                    номенклатураIds.Contains(ra.Sp408)
                               select new
                               {
                                   ФирмаId = ra.Sp4062,
                                   НоменклатураId = ra.Sp408,
                                   СкладId = ra.Sp418,
                                   ПодСкладId = ra.Sp9139,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp411 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp411 * 100000) : 0,
                               });
                return await (from r in регистр
                              group r by new { r.ФирмаId, r.НоменклатураId, r.СкладId, r.ПодСкладId } into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0
                              select new РегистрОстаткиТМЦ
                              {
                                  ФирмаId = gr.Key.ФирмаId,
                                  НоменклатураId = gr.Key.НоменклатураId,
                                  СкладId = gr.Key.СкладId,
                                  ПодСкладId = gr.Key.ПодСкладId,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000,
                              })
                              .ToListAsync();
            }
        }
        public async Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
            string ФирмаId,
            string НоменклатураId,
            string СкладId,
            string ПодСкладId,
            decimal ЦенаПрод,
            decimal Количество,
            decimal Внутреннее
            )
        {
            string startOfMonth = new DateTime(ДатаДок.Year, ДатаДок.Month, 1).ToShortDateString();
            await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA405_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                "@ФирмаId,@НоменклатураId,@СкладId,@ЦенаПрод,@ПодСкладId,@Количество,@Внутреннее," +
                "@docDate,@CurPeriod,1,0",
                new SqlParameter("@num36", IdDoc),
                new SqlParameter("@ActNo", КоличествоДвижений),
                new SqlParameter("@DebetCredit", ДвижениеРасход),
                new SqlParameter("@ФирмаId", ФирмаId),
                new SqlParameter("@НоменклатураId", НоменклатураId),
                new SqlParameter("@СкладId", СкладId),
                new SqlParameter("@ЦенаПрод", ЦенаПрод),
                new SqlParameter("@ПодСкладId", ПодСкладId),
                new SqlParameter("@Количество", Количество),
                new SqlParameter("@Внутреннее", Внутреннее),
                new SqlParameter("@docDate", ДатаДок.ToShortDateString()),
                new SqlParameter("@CurPeriod", startOfMonth));
            return true;
        }
    }
}
