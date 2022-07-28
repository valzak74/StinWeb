using StinClasses.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StinClasses.Регистры
{
    public class РегистрПокупатели
    {
        public string ФирмаId { get; set; }
        public string ДоговорId { get; set; }
        public string ВидДолгаId { get; set; }
        public string КредДокументId13 { get; set; }
        public string DateTimeIdDoc { get; set; }
        public decimal СуммаВал { get; set; }
        public decimal СуммаУпр { get; set; }
        public decimal СуммаРуб { get; set; }
        public decimal Себестоимость { get; set; }
    }
    public interface IРегистрПокупатели : IDisposable
    {
        Task<List<РегистрПокупатели>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, List<string> договорIds);
        Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
            string ФирмаId,
            string ДоговорId,
            string ВидДолгаId,
            string ДокументId13,
            decimal Себестоимость,
            decimal ПродСтоимость,
            string КодОперации,
            string ДоговорКомитентаId,
            string ДокументОплатыId13
            );
    }
    public class Регистр_Покупатели : IРегистрПокупатели
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
        public Регистр_Покупатели(StinDbContext context)
        {
            _context = context;
        }
        public async Task<List<РегистрПокупатели>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, List<string> договорIds)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= _context.GetDateTA())
            {
                DateTime dateRegTA = _context.GetRegTA();
                return await (from r in _context.Rg4335s
                              join j in _context._1sjourns on r.Sp4326.Substring(4, 9) equals j.Iddoc into _j
                              from j in _j.DefaultIfEmpty()
                              where r.Period == dateRegTA &&
                                фирмаIds.Contains(r.Sp4322) &&
                                договорIds.Contains(r.Sp4323)
                              group r by new { r.Sp4322, r.Sp4323, r.Sp4325, r.Sp4326, j.DateTimeIddoc } into gr
                              where gr.Sum(x => x.Sp4327) != 0
                                || gr.Sum(x => x.Sp4328) != 0
                                || gr.Sum(x => x.Sp4329) != 0
                                || gr.Sum(x => x.Sp4331) != 0
                              select new РегистрПокупатели
                              {
                                  ФирмаId = gr.Key.Sp4322,
                                  ДоговорId = gr.Key.Sp4323,
                                  ВидДолгаId = gr.Key.Sp4325,
                                  КредДокументId13 = gr.Key.Sp4326,
                                  DateTimeIdDoc = gr.Key.DateTimeIddoc,
                                  СуммаВал = gr.Sum(x => x.Sp4327),
                                  СуммаУпр = gr.Sum(x => x.Sp4328),
                                  СуммаРуб = gr.Sum(x => x.Sp4329),
                                  Себестоимость = gr.Sum(x => x.Sp4331)
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
                var регистр = (from rg in _context.Rg4335s
                               join j in _context._1sjourns on rg.Sp4326.Substring(4, 9) equals j.Iddoc into _j
                               from j in _j.DefaultIfEmpty()
                               where rg.Period == previousRegPeriod &&
                                    фирмаIds.Contains(rg.Sp4322) &&
                                    договорIds.Contains(rg.Sp4323)
                               select new
                               {
                                   ФирмаId = rg.Sp4322,
                                   ДоговорId = rg.Sp4323,
                                   ВидДолгаId = rg.Sp4325,
                                   КредДокументId13 = rg.Sp4326,
                                   DateTimeIdDoc = j.DateTimeIddoc,
                                   начСуммаВал = (int)(rg.Sp4327 * 100),
                                   приходСуммаВал = 0,
                                   расходСуммаВал = 0,
                                   начСуммаУпр = (int)(rg.Sp4328 * 100),
                                   приходСуммаУпр = 0,
                                   расходСуммаУпр = 0,
                                   начСуммаРуб = (int)(rg.Sp4329 * 100),
                                   приходСуммаРуб = 0,
                                   расходСуммаРуб = 0,
                                   начСебестоимость = (int)(rg.Sp4331 * 100),
                                   приходСебестоимость = 0,
                                   расходСебестоимость = 0,
                               })
                              .Concat
                              (from ra in _context.Ra4335s
                               join jp in _context._1sjourns on ra.Sp4326.Substring(4, 9) equals jp.Iddoc into _j
                               from jp in _j.DefaultIfEmpty()
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                    фирмаIds.Contains(ra.Sp4322) &&
                                    договорIds.Contains(ra.Sp4323)
                               select new
                               {
                                   ФирмаId = ra.Sp4322,
                                   ДоговорId = ra.Sp4323,
                                   ВидДолгаId = ra.Sp4325,
                                   КредДокументId13 = ra.Sp4326,
                                   DateTimeIdDoc = jp.DateTimeIddoc,
                                   начСуммаВал = 0,
                                   приходСуммаВал = !ra.Debkred ? (int)(ra.Sp4327 * 100) : 0,
                                   расходСуммаВал = ra.Debkred ? (int)(ra.Sp4327 * 100) : 0,
                                   начСуммаУпр = 0,
                                   приходСуммаУпр = !ra.Debkred ? (int)(ra.Sp4328 * 100) : 0,
                                   расходСуммаУпр = ra.Debkred ? (int)(ra.Sp4328 * 100) : 0,
                                   начСуммаРуб = 0,
                                   приходСуммаРуб = !ra.Debkred ? (int)(ra.Sp4329 * 100) : 0,
                                   расходСуммаРуб = ra.Debkred ? (int)(ra.Sp4329 * 100) : 0,
                                   начСебестоимость = 0,
                                   приходСебестоимость = !ra.Debkred ? (int)(ra.Sp4331 * 100) : 0,
                                   расходСебестоимость = ra.Debkred ? (int)(ra.Sp4331 * 100) : 0,
                               });
                return await (from r in регистр
                              group r by new { r.ФирмаId, r.ДоговорId, r.ВидДолгаId, r.КредДокументId13, r.DateTimeIdDoc } into gr
                              where (gr.Sum(x => x.начСуммаВал) + gr.Sum(x => x.приходСуммаВал) - gr.Sum(x => x.расходСуммаВал)) != 0
                                || (gr.Sum(x => x.начСуммаУпр) + gr.Sum(x => x.приходСуммаУпр) - gr.Sum(x => x.расходСуммаУпр)) != 0
                                || (gr.Sum(x => x.начСуммаРуб) + gr.Sum(x => x.приходСуммаРуб) - gr.Sum(x => x.расходСуммаРуб)) != 0
                                || (gr.Sum(x => x.начСебестоимость) + gr.Sum(x => x.приходСебестоимость) - gr.Sum(x => x.расходСебестоимость)) != 0
                              select new РегистрПокупатели
                              {
                                  ФирмаId = gr.Key.ФирмаId,
                                  ДоговорId = gr.Key.ДоговорId,
                                  ВидДолгаId = gr.Key.ВидДолгаId,
                                  КредДокументId13 = gr.Key.КредДокументId13,
                                  DateTimeIdDoc = gr.Key.DateTimeIdDoc,
                                  СуммаВал = (gr.Sum(x => x.начСуммаВал) + gr.Sum(x => x.приходСуммаВал) - gr.Sum(x => x.расходСуммаВал)) / 100,
                                  СуммаУпр = (gr.Sum(x => x.начСуммаУпр) + gr.Sum(x => x.приходСуммаУпр) - gr.Sum(x => x.расходСуммаУпр)) / 100,
                                  СуммаРуб = (gr.Sum(x => x.начСуммаРуб) + gr.Sum(x => x.приходСуммаРуб) - gr.Sum(x => x.расходСуммаРуб)) / 100,
                                  Себестоимость = (gr.Sum(x => x.начСебестоимость) + gr.Sum(x => x.приходСебестоимость) - gr.Sum(x => x.расходСебестоимость)) / 100,
                              })
                              .ToListAsync();
            }
        }
        public async Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
            string ФирмаId,
            string ДоговорId,
            string ВидДолгаId,
            string ДокументId13,
            decimal Себестоимость,
            decimal ПродСтоимость,
            string КодОперации,
            string ДоговорКомитентаId,
            string ДокументОплатыId13
            )
        {
            string startOfMonth = new DateTime(ДатаДок.Year, ДатаДок.Month, 1).ToShortDateString();
            await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA4335_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                "@ФирмаId,@ДоговорId,@СтавкаНпId,@ВидДолга,@ДокументId13,@СуммаВал,@СуммаУпр,@СуммаРуб,@СуммаНП,@Себестоимость," +
                "@КодОперации,@ДоговорКомитентаId,@ДокументОплатыId13," +
                "@docDate,@CurPeriod,1,0",
                new SqlParameter("@num36", IdDoc),
                new SqlParameter("@ActNo", КоличествоДвижений),
                new SqlParameter("@DebetCredit", ДвижениеРасход),
                new SqlParameter("@ФирмаId", ФирмаId),
                new SqlParameter("@ДоговорId", ДоговорId),
                new SqlParameter("@СтавкаНпId", Common.СтавкаНПбезНалога),
                new SqlParameter("@ВидДолга", ВидДолгаId),
                new SqlParameter("@ДокументId13", ДокументId13),
                new SqlParameter("@СуммаВал", ПродСтоимость),
                new SqlParameter("@СуммаУпр", ПродСтоимость),
                new SqlParameter("@СуммаРуб", ПродСтоимость),
                new SqlParameter("@СуммаНП", Common.zero),
                new SqlParameter("@Себестоимость", Себестоимость),
                new SqlParameter("@КодОперации", КодОперации),
                new SqlParameter("@ДоговорКомитентаId", ДоговорКомитентаId),
                new SqlParameter("@ДокументОплатыId13", ДокументОплатыId13),
                new SqlParameter("@docDate", ДатаДок.ToShortDateString()),
                new SqlParameter("@CurPeriod", startOfMonth));
            return true;
        }
    }
}
