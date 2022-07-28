using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using StinClasses.Models;
using StinClasses.Документы;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StinClasses.Регистры
{
    public class РегистрОстаткиДоставки
    {
        public string ФирмаId { get; set; }
        public string СкладКудаId { get; set; }
        public string НоменклатураId { get; set; }
        public string ДокПеремещенияId13 { get; set; }
        public bool ЭтоИзделие { get; set; }
        public decimal Количество { get; set; }
    }
    public interface IРегистрОстаткиДоставки : IDisposable
    {
        Task<List<РегистрОстаткиДоставки>> ПолучитьОстаткиПоЗаявкамAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> заявкаIds);
        Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
            string ФирмаId,
            string НоменклатураId,
            string СкладId,
            string ДокПеремещенияId13,
            bool ЭтоИзделие,
            decimal Количество,
            bool Внутреннее
            );
    }
    public class Регистр_ОстаткиДоставки : IРегистрОстаткиДоставки
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
        public Регистр_ОстаткиДоставки(StinDbContext context)
        {
            _context = context;
        }
        public async Task<List<РегистрОстаткиДоставки>> ПолучитьОстаткиПоЗаявкамAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> заявкаIds)
        {
            var перемещениеIds13 = заявкаIds.Select(x => Common.Encode36((int)ВидДокумента.ПеремещениеТМЦ).PadLeft(4) + x).ToList();
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= _context.GetDateTA())
            {
                DateTime dateRegTA = _context.GetRegTA();
                return await (from r in _context.Rg8696s
                              where r.Period == dateRegTA &&
                                перемещениеIds13.Contains(r.Sp8715)
                              group r by new { r.Sp8697, r.Sp8698, r.Sp8699, r.Sp8715, r.Sp11041 } into gr
                              where gr.Sum(x => x.Sp8701) != 0
                              select new РегистрОстаткиДоставки
                              {
                                  ФирмаId = gr.Key.Sp8697,
                                  НоменклатураId = gr.Key.Sp8698,
                                  СкладКудаId = gr.Key.Sp8699,
                                  ДокПеремещенияId13 = gr.Key.Sp8715,
                                  ЭтоИзделие = gr.Key.Sp11041 == 1,
                                  Количество = gr.Sum(x => x.Sp8701),
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
                var регистр = (from rg in _context.Rg8696s
                               where rg.Period == previousRegPeriod &&
                                    перемещениеIds13.Contains(rg.Sp8715)
                               select new
                               {
                                   ФирмаId = rg.Sp8697,
                                   НоменклатураId = rg.Sp8698,
                                   СкладКудаId = rg.Sp8699,
                                   ДокПеремещенияId13 = rg.Sp8715,
                                   ЭтоИзделие = rg.Sp11041 == 1,
                                   начОстаток = (int)(rg.Sp8701 * 100000),
                                   приход = 0,
                                   расход = 0,
                               })
                              .Concat
                              (from ra in _context.Ra8696s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                    перемещениеIds13.Contains(ra.Sp8715)
                               select new
                               {
                                   ФирмаId = ra.Sp8697,
                                   НоменклатураId = ra.Sp8698,
                                   СкладКудаId = ra.Sp8699,
                                   ДокПеремещенияId13 = ra.Sp8715,
                                   ЭтоИзделие = ra.Sp11041 == 1,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp8701 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp8701 * 100000) : 0,
                               });
                return await (from r in регистр
                              group r by new { r.ФирмаId, r.НоменклатураId, r.СкладКудаId, r.ДокПеремещенияId13, r.ЭтоИзделие } into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0 
                              select new РегистрОстаткиДоставки
                              {
                                  ФирмаId = gr.Key.ФирмаId,
                                  НоменклатураId = gr.Key.НоменклатураId,
                                  СкладКудаId = gr.Key.СкладКудаId,
                                  ДокПеремещенияId13 = gr.Key.ДокПеремещенияId13,
                                  ЭтоИзделие = gr.Key.ЭтоИзделие,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000,
                                   })
                              .ToListAsync();
            }
        }
        public async Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
            string ФирмаId,
            string НоменклатураId,
            string СкладId,
            string ДокПеремещенияId13,
            bool ЭтоИзделие,
            decimal Количество,
            bool Внутреннее
            )
        {
            string startOfMonth = new DateTime(ДатаДок.Year, ДатаДок.Month, 1).ToShortDateString();
            await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA8696_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                "@ФирмаId,@НоменклатураId,@СкладId,0,@ДокПеремещенияId13,@ЭтоИзделие,@Количество,@Внутреннее," +
                "@docDate,@CurPeriod,1,0",
                new SqlParameter("@num36", IdDoc),
                new SqlParameter("@ActNo", КоличествоДвижений),
                new SqlParameter("@DebetCredit", ДвижениеРасход),
                new SqlParameter("@ФирмаId", ФирмаId),
                new SqlParameter("@НоменклатураId", НоменклатураId),
                new SqlParameter("@СкладId", СкладId),
                new SqlParameter("@ДокПеремещенияId13", ДокПеремещенияId13),
                new SqlParameter("@ЭтоИзделие", ЭтоИзделие ? 1 : 0),
                new SqlParameter("@Количество", Количество),
                new SqlParameter("@Внутреннее", Внутреннее ? 1 : 0),
                new SqlParameter("@docDate", ДатаДок.ToShortDateString()),
                new SqlParameter("@CurPeriod", startOfMonth));
            return true;
        }
    }
}
