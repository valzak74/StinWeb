using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using StinClasses.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StinClasses.Регистры
{
    public class РегистрПартииОтданные
    {
        public string ФирмаId { get; set; }
        public string ДоговорId { get; set; }
        public string НоменклатураId { get; set; }
        public string СтатусПартииId { get; set; }
        public string ПартияId { get; set; }
        public string IdDoc13 { get; set; }
        public decimal Количество { get; set; }
        public decimal СуммаУпр { get; set; }
        public decimal СуммаРуб { get; set; }
        public decimal СуммаБезНДС { get; set; }
        public decimal ПродСтоимость { get; set; }
    }
    public interface IРегистрПартииОтданные : IDisposable
    {
        Task<List<РегистрПартииОтданные>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, 
            List<string> фирмаIds, List<string> номенклатураIds, string договорId, string докПередачиId13 = "");
        Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
            short LineNo,
            string ФирмаId,
            string ДоговорId,
            string НоменклатураId,
            string СтатусПартииId,
            string ПартияId,
            string ДокПередачиId13,
            decimal Количество,
            decimal СуммаУпр,
            decimal СуммаРуб,
            decimal СуммаБезНДС,
            decimal ПродСтоимость,
            string КодОперации,
            decimal Выручка
            );
    }
    public class Регистр_ПартииОтданные : IРегистрПартииОтданные
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
        public Регистр_ПартииОтданные(StinDbContext context)
        {
            _context = context;
        }
        public async Task<List<РегистрПартииОтданные>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine,
            List<string> фирмаIds, List<string> номенклатураIds, string договорId, string докПередачиId13 = "")
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= _context.GetDateTA())
            {
                DateTime dateRegTA = _context.GetRegTA();
                return await (from r in _context.Rg351s
                              where r.Period == dateRegTA &&
                                (string.IsNullOrEmpty(договорId) ? true : (r.Sp365 == договорId)) &&
                                (string.IsNullOrEmpty(докПередачиId13) ? true : (r.Sp364 == докПередачиId13)) &&
                                фирмаIds.Contains(r.Sp4063) &&
                                (номенклатураIds != null ? номенклатураIds.Contains(r.Sp354) : true)
                              group r by new { r.Sp4063, r.Sp365, r.Sp354, r.Sp355, r.Sp356, r.Sp364 } into gr
                              where gr.Sum(x => x.Sp357) != 0
                                || gr.Sum(x => x.Sp422) != 0
                                || gr.Sum(x => x.Sp423) != 0
                                || gr.Sum(x => x.Sp746) != 0
                                || gr.Sum(x => x.Sp3539) != 0
                              select new РегистрПартииОтданные
                              {
                                  ФирмаId = gr.Key.Sp4063,
                                  ДоговорId = gr.Key.Sp365,
                                  НоменклатураId = gr.Key.Sp354,
                                  СтатусПартииId = gr.Key.Sp355,
                                  ПартияId = gr.Key.Sp356,
                                  IdDoc13 = gr.Key.Sp364,
                                  Количество = gr.Sum(x => x.Sp357),
                                  СуммаУпр = gr.Sum(x => x.Sp422),
                                  СуммаРуб = gr.Sum(x => x.Sp423),
                                  СуммаБезНДС = gr.Sum(x => x.Sp746),
                                  ПродСтоимость = gr.Sum(x => x.Sp3539),
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
                var регистр = (from rg in _context.Rg351s
                               where rg.Period == previousRegPeriod &&
                                    (string.IsNullOrEmpty(договорId) ? true : rg.Sp365 == договорId) &&
                                    (string.IsNullOrEmpty(докПередачиId13) ? true : rg.Sp364 == докПередачиId13) &&
                                    фирмаIds.Contains(rg.Sp4063) &&
                                    (номенклатураIds != null ? номенклатураIds.Contains(rg.Sp354) : true)
                               select new
                               {
                                   ФирмаId = rg.Sp4063,
                                   ДоговорId = rg.Sp365,
                                   НоменклатураId = rg.Sp354,
                                   СтатусПартииId = rg.Sp355,
                                   ПартияId = rg.Sp356,
                                   IdDoc13 = rg.Sp364,
                                   начОстаток = (int)(rg.Sp357 * 100000),
                                   приход = 0,
                                   расход = 0,
                                   начСуммаУпр = (int)(rg.Sp422 * 100),
                                   приходСуммаУпр = 0,
                                   расходСуммаУпр = 0,
                                   начСуммаРуб = (int)(rg.Sp423 * 100),
                                   приходСуммаРуб = 0,
                                   расходСуммаРуб = 0,
                                   начСуммаБезНДС = (int)(rg.Sp746 * 100),
                                   приходСуммаБезНДС = 0,
                                   расходСуммаБезНДС = 0,
                                   начПродСтоимость = (int)(rg.Sp3539 * 100),
                                   приходПродСтоимость = 0,
                                   расходПродСтоимость = 0,
                               })
                              .Concat
                              (from ra in _context.Ra351s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                    (string.IsNullOrEmpty(договорId) ? true : ra.Sp365 == договорId) &&
                                    (string.IsNullOrEmpty(докПередачиId13) ? true : ra.Sp364 == докПередачиId13) &&
                                    фирмаIds.Contains(ra.Sp4063) &&
                                    (номенклатураIds != null ? номенклатураIds.Contains(ra.Sp354) : true)
                               select new
                               {
                                   ФирмаId = ra.Sp4063,
                                   ДоговорId = ra.Sp365,
                                   НоменклатураId = ra.Sp354,
                                   СтатусПартииId = ra.Sp355,
                                   ПартияId = ra.Sp356,
                                   IdDoc13 = ra.Sp364,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp357 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp357 * 100000) : 0,
                                   начСуммаУпр = 0,
                                   приходСуммаУпр = !ra.Debkred ? (int)(ra.Sp422 * 100) : 0,
                                   расходСуммаУпр = ra.Debkred ? (int)(ra.Sp422 * 100) : 0,
                                   начСуммаРуб = 0,
                                   приходСуммаРуб = !ra.Debkred ? (int)(ra.Sp423 * 100) : 0,
                                   расходСуммаРуб = ra.Debkred ? (int)(ra.Sp423 * 100) : 0,
                                   начСуммаБезНДС = 0,
                                   приходСуммаБезНДС = !ra.Debkred ? (int)(ra.Sp746 * 100) : 0,
                                   расходСуммаБезНДС = ra.Debkred ? (int)(ra.Sp746 * 100) : 0,
                                   начПродСтоимость = 0,
                                   приходПродСтоимость = !ra.Debkred ? (int)(ra.Sp3539 * 100) : 0,
                                   расходПродСтоимость = ra.Debkred ? (int)(ra.Sp3539 * 100) : 0,
                               });
                return await (from r in регистр
                              group r by new { r.ФирмаId, r.ДоговорId, r.НоменклатураId, r.СтатусПартииId, r.ПартияId, r.IdDoc13 } into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0
                                || (gr.Sum(x => x.начСуммаУпр) + gr.Sum(x => x.приходСуммаУпр) - gr.Sum(x => x.расходСуммаУпр)) != 0
                                || (gr.Sum(x => x.начСуммаРуб) + gr.Sum(x => x.приходСуммаРуб) - gr.Sum(x => x.расходСуммаРуб)) != 0
                                || (gr.Sum(x => x.начСуммаБезНДС) + gr.Sum(x => x.приходСуммаБезНДС) - gr.Sum(x => x.расходСуммаБезНДС)) != 0
                                || (gr.Sum(x => x.начПродСтоимость) + gr.Sum(x => x.приходПродСтоимость) - gr.Sum(x => x.расходПродСтоимость)) != 0
                              select new РегистрПартииОтданные
                              {
                                  ФирмаId = gr.Key.ФирмаId,
                                  ДоговорId = gr.Key.ДоговорId,
                                  НоменклатураId = gr.Key.НоменклатураId,
                                  СтатусПартииId = gr.Key.СтатусПартииId,
                                  ПартияId = gr.Key.ПартияId,
                                  IdDoc13 = gr.Key.IdDoc13,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000,
                                  СуммаУпр = (gr.Sum(x => x.начСуммаУпр) + gr.Sum(x => x.приходСуммаУпр) - gr.Sum(x => x.расходСуммаУпр)) / 100,
                                  СуммаРуб = (gr.Sum(x => x.начСуммаРуб) + gr.Sum(x => x.приходСуммаРуб) - gr.Sum(x => x.расходСуммаРуб)) / 100,
                                  СуммаБезНДС = (gr.Sum(x => x.начСуммаБезНДС) + gr.Sum(x => x.приходСуммаБезНДС) - gr.Sum(x => x.расходСуммаБезНДС)) / 100,
                                  ПродСтоимость = (gr.Sum(x => x.начПродСтоимость) + gr.Sum(x => x.приходПродСтоимость) - gr.Sum(x => x.расходПродСтоимость)) / 100,
                              })
                              .ToListAsync();
            }
        }
        public async Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
            short LineNo,
            string ФирмаId,
            string ДоговорId,
            string НоменклатураId,
            string СтатусПартииId,
            string ПартияId,
            string ДокПередачиId13,
            decimal Количество,
            decimal СуммаУпр,
            decimal СуммаРуб,
            decimal СуммаБезНДС,
            decimal ПродСтоимость,
            string КодОперации,
            decimal Выручка
            )
        {
            string startOfMonth = new DateTime(ДатаДок.Year, ДатаДок.Month, 1).ToShortDateString();
            await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA351_WriteDocAct @num36,@LineNo,@ActNo,@DebetCredit," +
                "@ФирмаId,@ДоговорId,@НоменклатураId,@СтатусПартииId,@ПартияId,@ДокПередачиId13,@Количество,@СуммаУпр,@СуммаРуб,@СуммаБезНДС,@ПродСтоимость,@КодОперации,@Выручка," +
                "@docDate,@CurPeriod,1,0",
                new SqlParameter("@num36", IdDoc),
                new SqlParameter("@LineNo", LineNo),
                new SqlParameter("@ActNo", КоличествоДвижений),
                new SqlParameter("@DebetCredit", ДвижениеРасход),
                new SqlParameter("@ФирмаId", ФирмаId),
                new SqlParameter("@ДоговорId", ДоговорId),
                new SqlParameter("@НоменклатураId", НоменклатураId),
                new SqlParameter("@СтатусПартииId", СтатусПартииId),
                new SqlParameter("@ПартияId", ПартияId),
                new SqlParameter("@ДокПередачиId13", ДокПередачиId13),
                new SqlParameter("@Количество", Количество),
                new SqlParameter("@СуммаУпр", СуммаУпр),
                new SqlParameter("@СуммаРуб", СуммаРуб),
                new SqlParameter("@СуммаБезНДС", СуммаБезНДС),
                new SqlParameter("@ПродСтоимость", ПродСтоимость),
                new SqlParameter("@КодОперации", КодОперации),
                new SqlParameter("@Выручка", Выручка),
                new SqlParameter("@docDate", ДатаДок.ToShortDateString()),
                new SqlParameter("@CurPeriod", startOfMonth));
            return true;
        }

    }
}
