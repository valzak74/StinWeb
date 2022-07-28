using StinClasses.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StinClasses.Регистры
{
    public class РегистрЗаменаНоменклатуры
    {
        public string ФирмаId { get; set; }
        public string НоменклатураБухId { get; set; }
        public string НоменклатураОБId { get; set; }
        public decimal Количество { get; set; }
    }
    public interface IРегистрЗаменаНоменклатуры : IDisposable
    {
        Task<List<РегистрЗаменаНоменклатуры>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string фирмаId, List<string> номенклатураОбIds);
        Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
            string ФирмаId,
            string НоменклатураБухId,
            string НоменклатураОБId,
            decimal Количество
            );
    }
    public class Регистр_ЗаменаНоменклатуры : IРегистрЗаменаНоменклатуры
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
        public Регистр_ЗаменаНоменклатуры(StinDbContext context)
        {
            _context = context;
        }
        public async Task<List<РегистрЗаменаНоменклатуры>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string фирмаId, List<string> номенклатураОбIds)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= _context.GetDateTA())
            {
                DateTime dateRegTA = _context.GetRegTA();
                return await (from r in _context.Rg12351s
                              where r.Period == dateRegTA &&
                                (string.IsNullOrEmpty(фирмаId) ? true : r.Sp12347 == фирмаId) &&
                                номенклатураОбIds.Contains(r.Sp12349)
                              group r by new { r.Sp12347, r.Sp12348, r.Sp12349 } into gr
                              where gr.Sum(x => x.Sp12350) != 0
                              select new РегистрЗаменаНоменклатуры
                              {
                                  ФирмаId = gr.Key.Sp12347,
                                  НоменклатураБухId = gr.Key.Sp12348,
                                  НоменклатураОБId = gr.Key.Sp12349,
                                  Количество = gr.Sum(x => x.Sp12350),
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
                var регистр = (from rg in _context.Rg12351s
                               where rg.Period == previousRegPeriod &&
                                    (string.IsNullOrEmpty(фирмаId) ? true : rg.Sp12347 == фирмаId) &&
                                    номенклатураОбIds.Contains(rg.Sp12349)
                               select new
                               {
                                   ФирмаId = rg.Sp12347,
                                   НоменклатураБухId = rg.Sp12348,
                                   НоменклатураОБId = rg.Sp12349,
                                   начОстаток = (int)(rg.Sp12350 * 100000),
                                   приход = 0,
                                   расход = 0,
                               })
                              .Concat
                              (from ra in _context.Ra12351s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                    (string.IsNullOrEmpty(фирмаId) ? true : ra.Sp12347 == фирмаId) &&
                                    номенклатураОбIds.Contains(ra.Sp12349)
                               select new
                               {
                                   ФирмаId = ra.Sp12347,
                                   НоменклатураБухId = ra.Sp12348,
                                   НоменклатураОБId = ra.Sp12349,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp12350 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp12350 * 100000) : 0,
                               });
                return await (from r in регистр
                              group r by new { r.ФирмаId, r.НоменклатураБухId, r.НоменклатураОБId } into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0
                              select new РегистрЗаменаНоменклатуры
                              {
                                  ФирмаId = gr.Key.ФирмаId,
                                  НоменклатураБухId = gr.Key.НоменклатураБухId,
                                  НоменклатураОБId = gr.Key.НоменклатураОБId,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000,
                              })
                              .ToListAsync();
            }
        }
        public async Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
            string ФирмаId,
            string НоменклатураБухId,
            string НоменклатураОБId,
            decimal Количество
            )
        {
            string startOfMonth = new DateTime(ДатаДок.Year, ДатаДок.Month, 1).ToShortDateString();
            await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA12351_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                "@ФирмаId,@НоменклатураБухId,@НоменклатураОБId,@Количество," +
                "@docDate,@CurPeriod,1,0",
                new SqlParameter("@num36", IdDoc),
                new SqlParameter("@ActNo", КоличествоДвижений),
                new SqlParameter("@DebetCredit", ДвижениеРасход),
                new SqlParameter("@ФирмаId", ФирмаId),
                new SqlParameter("@НоменклатураБухId", НоменклатураБухId),
                new SqlParameter("@НоменклатураОБId", НоменклатураОБId),
                new SqlParameter("@Количество", Количество),
                new SqlParameter("@docDate", ДатаДок.ToShortDateString()),
                new SqlParameter("@CurPeriod", startOfMonth));
            return true;
        }
    }
}
