using StinClasses.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StinClasses.Регистры
{
    public class РегистрЗаказыЗаявки
    {
        public string НоменклатураId { get; set; }
        public string ЗаявкаId { get; set; }
        public string ЗаказId { get; set; }
        public int НаСогласование { get; set; }
        public decimal Количество { get; set; }
    }
    public interface IРегистрЗаказыЗаявки : IDisposable
    {
        Task<List<РегистрЗаказыЗаявки>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string заказId, string заявкаId, List<string> номенклатураIds, int НаСогласование = -1);
        Task<List<РегистрЗаказыЗаявки>> ПолучитьОстаткиПоЗаявкамAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string заказId, List<string> заявкаIds, List<string> номенклатураIds, int НаСогласование = -1);
        Task<List<string>> ПолучитьСписокАктивныхЗаказовЗаявокAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string orderId);
        Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
                    string НоменклатураId,
                    string ЗаказId,
                    string ЗаявкаId,
                    int НаСогласование,
                    decimal Количество
                    );
    }
    public class Регистр_ЗаказыЗаявки : IРегистрЗаказыЗаявки
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
        public Регистр_ЗаказыЗаявки(StinDbContext context)
        {
            _context = context;
        }
        public async Task<List<РегистрЗаказыЗаявки>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string заказId, string заявкаId, List<string> номенклатураIds, int НаСогласование = -1)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= _context.GetDateTA())
            {
                DateTime dateRegTA = _context.GetRegTA();
                return await (from r in _context.Rg4667s
                              where r.Period == dateRegTA &&
                                (string.IsNullOrEmpty(заказId) ? true : r.Sp4665 == заказId) &&
                                (string.IsNullOrEmpty(заявкаId) ? true : r.Sp4664 == заявкаId) &&
                                (НаСогласование < 0 ? true : (int)r.Sp13435 == НаСогласование) &&
                                номенклатураIds.Contains(r.Sp4663)
                              group r by new { r.Sp4663, r.Sp4664, r.Sp4665, r.Sp13435 } into gr
                              where gr.Sum(x => x.Sp4666) != 0
                              select new РегистрЗаказыЗаявки
                              {
                                  НоменклатураId = gr.Key.Sp4663,
                                  ЗаказId = gr.Key.Sp4665,
                                  ЗаявкаId = gr.Key.Sp4664,
                                  НаСогласование = (int)gr.Key.Sp13435,
                                  Количество = gr.Sum(x => x.Sp4666),
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
                var регистр = (from rg in _context.Rg4667s
                               where rg.Period == previousRegPeriod &&
                                    (string.IsNullOrEmpty(заказId) ? true : rg.Sp4665 == заказId) &&
                                    (string.IsNullOrEmpty(заявкаId) ? true : rg.Sp4664 == заявкаId) &&
                                    (НаСогласование < 0 ? true : (int)rg.Sp13435 == НаСогласование) &&
                                    номенклатураIds.Contains(rg.Sp4663)
                               select new
                               {
                                   НоменклатураId = rg.Sp4663,
                                   ЗаказId = rg.Sp4665,
                                   ЗаявкаId = rg.Sp4664,
                                   НаСогласование = (int)rg.Sp13435,
                                   начОстаток = (int)(rg.Sp4666 * 100000),
                                   приход = 0,
                                   расход = 0,
                               })
                              .Concat
                              (from ra in _context.Ra4667s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                    (string.IsNullOrEmpty(заказId) ? true : ra.Sp4665 == заказId) &&
                                    (string.IsNullOrEmpty(заявкаId) ? true : ra.Sp4664 == заявкаId) &&
                                    (НаСогласование < 0 ? true : (int)ra.Sp13435 == НаСогласование) &&
                                    номенклатураIds.Contains(ra.Sp4663)
                               select new
                               {
                                   НоменклатураId = ra.Sp4663,
                                   ЗаказId = ra.Sp4665,
                                   ЗаявкаId = ra.Sp4664,
                                   НаСогласование = (int)ra.Sp13435,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp4666 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp4666 * 100000) : 0,
                               });
                return await (from r in регистр
                              group r by new { r.НоменклатураId, r.ЗаказId, r.ЗаявкаId, r.НаСогласование } into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0
                              select new РегистрЗаказыЗаявки
                              {
                                  НоменклатураId = gr.Key.НоменклатураId,
                                  ЗаказId = gr.Key.ЗаказId,
                                  ЗаявкаId = gr.Key.ЗаявкаId,
                                  НаСогласование = gr.Key.НаСогласование,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000,
                              })
                              .ToListAsync();
            }
        }
        public async Task<List<РегистрЗаказыЗаявки>> ПолучитьОстаткиПоЗаявкамAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string заказId, List<string> заявкаIds, List<string> номенклатураIds, int НаСогласование = -1)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= _context.GetDateTA())
            {
                DateTime dateRegTA = _context.GetRegTA();
                return await (from r in _context.Rg4667s
                              where r.Period == dateRegTA &&
                                (string.IsNullOrEmpty(заказId) ? true : r.Sp4665 == заказId) &&
                                (((заявкаIds != null) && (заявкаIds.Count > 0)) ? заявкаIds.Contains(r.Sp4664) : true) &&
                                (НаСогласование < 0 ? true : (int)r.Sp13435 == НаСогласование) &&
                                номенклатураIds.Contains(r.Sp4663)
                              group r by new { r.Sp4663, r.Sp4664, r.Sp4665, r.Sp13435 } into gr
                              where gr.Sum(x => x.Sp4666) != 0
                              select new РегистрЗаказыЗаявки
                              {
                                  НоменклатураId = gr.Key.Sp4663,
                                  ЗаказId = gr.Key.Sp4665,
                                  ЗаявкаId = gr.Key.Sp4664,
                                  НаСогласование = (int)gr.Key.Sp13435,
                                  Количество = gr.Sum(x => x.Sp4666),
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
                var регистр = (from rg in _context.Rg4667s
                               where rg.Period == previousRegPeriod &&
                                    (string.IsNullOrEmpty(заказId) ? true : rg.Sp4665 == заказId) &&
                                    (((заявкаIds != null) && (заявкаIds.Count > 0)) ? заявкаIds.Contains(rg.Sp4664) : true) &&
                                    (НаСогласование < 0 ? true : (int)rg.Sp13435 == НаСогласование) &&
                                    номенклатураIds.Contains(rg.Sp4663)
                               select new
                               {
                                   НоменклатураId = rg.Sp4663,
                                   ЗаказId = rg.Sp4665,
                                   ЗаявкаId = rg.Sp4664,
                                   НаСогласование = (int)rg.Sp13435,
                                   начОстаток = (int)(rg.Sp4666 * 100000),
                                   приход = 0,
                                   расход = 0,
                               })
                              .Concat
                              (from ra in _context.Ra4667s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                    (string.IsNullOrEmpty(заказId) ? true : ra.Sp4665 == заказId) &&
                                    (((заявкаIds != null) && (заявкаIds.Count > 0)) ? заявкаIds.Contains(ra.Sp4664) : true) &&
                                    (НаСогласование < 0 ? true : (int)ra.Sp13435 == НаСогласование) &&
                                    номенклатураIds.Contains(ra.Sp4663)
                               select new
                               {
                                   НоменклатураId = ra.Sp4663,
                                   ЗаказId = ra.Sp4665,
                                   ЗаявкаId = ra.Sp4664,
                                   НаСогласование = (int)ra.Sp13435,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp4666 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp4666 * 100000) : 0,
                               });
                return await (from r in регистр
                              group r by new { r.НоменклатураId, r.ЗаказId, r.ЗаявкаId, r.НаСогласование } into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0
                              select new РегистрЗаказыЗаявки
                              {
                                  НоменклатураId = gr.Key.НоменклатураId,
                                  ЗаказId = gr.Key.ЗаказId,
                                  ЗаявкаId = gr.Key.ЗаявкаId,
                                  НаСогласование = gr.Key.НаСогласование,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000,
                              })
                              .ToListAsync();
            }
        }
        public async Task<List<string>> ПолучитьСписокАктивныхЗаказовЗаявокAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string orderId)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= _context.GetDateTA())
            {
                DateTime dateRegTA = _context.GetRegTA();
                return await (from r in _context.Rg4667s
                              join doc in _context.Dh2457s on r.Sp4664 equals doc.Iddoc
                              where r.Period == dateRegTA &&
                                doc.Sp13995 == orderId
                              group r by r.Sp4664 into gr
                              where gr.Sum(x => x.Sp4666) != 0
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
                var регистр = (from rg in _context.Rg4667s
                               join doc in _context.Dh2457s on rg.Sp4664 equals doc.Iddoc
                               where rg.Period == previousRegPeriod &&
                                    doc.Sp13995 == orderId
                               select new
                               {
                                   ЗаявкаId = rg.Sp4664,
                                   начОстаток = (int)(rg.Sp4666 * 100000),
                                   приход = 0,
                                   расход = 0,
                               })
                              .Concat
                              (from ra in _context.Ra4667s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               join doc in _context.Dh2457s on ra.Sp4664 equals doc.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                    doc.Sp13995 == orderId
                               select new
                               {
                                   ЗаявкаId = ra.Sp4664,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp4666 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp4666 * 100000) : 0,
                               });
                return await (from r in регистр
                              group r by r.ЗаявкаId into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0 
                              select gr.Key
                              ).ToListAsync();
            }

        }
        public async Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
            string НоменклатураId,
            string ЗаказId,
            string ЗаявкаId,
            int НаСогласование,
            decimal Количество
            )
        {
            string startOfMonth = new DateTime(ДатаДок.Year, ДатаДок.Month, 1).ToShortDateString();
            await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA4667_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                "@НоменклатураId,@ЗаявкаId,@ЗаказId,@НаСогласование,@Количество," +
                "@docDate,@CurPeriod,1,0",
                new SqlParameter("@num36", IdDoc),
                new SqlParameter("@ActNo", КоличествоДвижений),
                new SqlParameter("@DebetCredit", ДвижениеРасход),
                new SqlParameter("@НоменклатураId", НоменклатураId),
                new SqlParameter("@ЗаявкаId", ЗаявкаId),
                new SqlParameter("@ЗаказId", ЗаказId),
                new SqlParameter("@НаСогласование", НаСогласование),
                new SqlParameter("@Количество", Количество),
                new SqlParameter("@docDate", ДатаДок.ToShortDateString()),
                new SqlParameter("@CurPeriod", startOfMonth));
            return true;
        }
    }
}
