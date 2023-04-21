using StinClasses.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StinClasses.Регистры
{
    public class РегистрПартииНаличие
    {
        public string ФирмаId { get; set; }
        public string НоменклатураId { get; set; }
        public string СтатусПартииId { get; set; }
        public string ПартияId { get; set; }
        public DateTime ДатаПартии { get; set; }
        public decimal ЦенаПрод { get; set; }
        public string DateTimeIdDoc { get; set; }
        public decimal Количество { get; set; }
        public decimal СуммаУпр { get; set; }
        public decimal СуммаРуб { get; set; }
        public decimal СуммаБезНДС { get; set; }
    }
    public interface IРегистрПартииНаличие : IDisposable
    {
        Task<List<РегистрПартииНаличие>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, List<string> номенклатураIds, string статусПартииId, string партияId);
        Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
            short LineNo,
            string ФирмаId,
            string НоменклатураId,
            string СтатусПартииId,
            string ПартияId,
            DateTime ДатаПартии,
            decimal ЦенаПрод,
            decimal Количество,
            decimal СуммаУпр,
            decimal СуммаРуб,
            decimal СуммаБезНДС,
            string КодОперации,
            decimal ПродСтоимость,
            decimal Выручка
            );
    }
    public class Регистр_ПартииНаличие : IРегистрПартииНаличие
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
        public Регистр_ПартииНаличие(StinDbContext context)
        {
            _context = context;
        }
        public async Task<List<РегистрПартииНаличие>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, List<string> номенклатураIds, string статусПартииId, string партияId)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= _context.GetDateTA())
            {
                DateTime dateRegTA = _context.GetRegTA();
                return await (from r in _context.Rg328s
                              join p in _context.Sc214s on r.Sp341 equals p.Id into _p
                              from p in _p.DefaultIfEmpty()
                              join j in _context._1sjourns on p.Sp216.Substring(4, 9) equals j.Iddoc into _j
                              from j in _j.DefaultIfEmpty()
                              where r.Period == dateRegTA &&
                                (string.IsNullOrEmpty(статусПартииId) ? true : r.Sp340 == статусПартииId) &&
                                (string.IsNullOrEmpty(партияId) ? true : r.Sp341 == партияId) &&
                                фирмаIds.Contains(r.Sp4061) &&
                                номенклатураIds.Contains(r.Sp331)
                              group r by new { r.Sp4061, r.Sp331, r.Sp340, r.Sp341, r.Sp1554, j.DateTimeIddoc, r.Sp7404 } into gr
                              where gr.Sum(x => x.Sp342) != 0
                                || gr.Sum(x => x.Sp421) != 0
                                || gr.Sum(x => x.Sp343) != 0
                                || gr.Sum(x => x.Sp344) != 0
                              select new РегистрПартииНаличие
                              {
                                  ФирмаId = gr.Key.Sp4061,
                                  НоменклатураId = gr.Key.Sp331,
                                  СтатусПартииId = gr.Key.Sp340,
                                  ПартияId = gr.Key.Sp341,
                                  ДатаПартии = gr.Key.Sp1554,
                                  DateTimeIdDoc = gr.Key.DateTimeIddoc,
                                  ЦенаПрод = gr.Key.Sp7404,
                                  Количество = gr.Sum(x => x.Sp342),
                                  СуммаУпр = gr.Sum(x => x.Sp421),
                                  СуммаРуб = gr.Sum(x => x.Sp343),
                                  СуммаБезНДС = gr.Sum(x => x.Sp344)
                              })
                              .ToListAsync();
            }
            else
            {
                dateReg.GetDateTimeValuesForRegistry(idDocDeadLine, out DateTime previousRegPeriod, out string PeriodStart, out string PeriodEnd);
                var регистр = (from rg in _context.Rg328s
                               join p in _context.Sc214s on rg.Sp341 equals p.Id into _p
                               from p in _p.DefaultIfEmpty()
                               join j in _context._1sjourns on p.Sp216.Substring(4, 9) equals j.Iddoc into _j
                               from j in _j.DefaultIfEmpty()
                               where rg.Period == previousRegPeriod &&
                                    (string.IsNullOrEmpty(статусПартииId) ? true : rg.Sp340 == статусПартииId) &&
                                    (string.IsNullOrEmpty(партияId) ? true : rg.Sp341 == партияId) &&
                                    фирмаIds.Contains(rg.Sp4061) &&
                                    номенклатураIds.Contains(rg.Sp331)
                               select new
                               {
                                   ФирмаId = rg.Sp4061,
                                   НоменклатураId = rg.Sp331,
                                   СтатусПартииId = rg.Sp340,
                                   ПартияId = rg.Sp341,
                                   ДатаПартии = rg.Sp1554,
                                   DateTimeIdDoc = j.DateTimeIddoc,
                                   ЦенаПрод = rg.Sp7404,
                                   начОстаток = (int)(rg.Sp342 * 100000),
                                   приход = 0,
                                   расход = 0,
                                   начСуммаУпр = (int)(rg.Sp421 * 100),
                                   приходСуммаУпр = 0,
                                   расходСуммаУпр = 0,
                                   начСуммаРуб = (int)(rg.Sp343 * 100),
                                   приходСуммаРуб = 0,
                                   расходСуммаРуб = 0,
                                   начСуммаБезНДС = (int)(rg.Sp344 * 100),
                                   приходСуммаБезНДС = 0,
                                   расходСуммаБезНДС = 0,
                               })
                              .Concat
                              (from ra in _context.Ra328s
                               join p in _context.Sc214s on ra.Sp341 equals p.Id into _p
                               from p in _p.DefaultIfEmpty()
                               join jp in _context._1sjourns on p.Sp216.Substring(4, 9) equals jp.Iddoc into _j
                               from jp in _j.DefaultIfEmpty()
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                    (string.IsNullOrEmpty(статусПартииId) ? true : ra.Sp340 == статусПартииId) &&
                                    (string.IsNullOrEmpty(партияId) ? true : ra.Sp341 == партияId) &&
                                    фирмаIds.Contains(ra.Sp4061) &&
                                    номенклатураIds.Contains(ra.Sp331)
                               select new
                               {
                                   ФирмаId = ra.Sp4061,
                                   НоменклатураId = ra.Sp331,
                                   СтатусПартииId = ra.Sp340,
                                   ПартияId = ra.Sp341,
                                   ДатаПартии = ra.Sp1554,
                                   DateTimeIdDoc = jp.DateTimeIddoc,
                                   ЦенаПрод = ra.Sp7404,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp342 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp342 * 100000) : 0,
                                   начСуммаУпр = 0,
                                   приходСуммаУпр = !ra.Debkred ? (int)(ra.Sp421 * 100) : 0,
                                   расходСуммаУпр = ra.Debkred ? (int)(ra.Sp421 * 100) : 0,
                                   начСуммаРуб = 0,
                                   приходСуммаРуб = !ra.Debkred ? (int)(ra.Sp343 * 100) : 0,
                                   расходСуммаРуб = ra.Debkred ? (int)(ra.Sp343 * 100) : 0,
                                   начСуммаБезНДС = 0,
                                   приходСуммаБезНДС = !ra.Debkred ? (int)(ra.Sp344 * 100) : 0,
                                   расходСуммаБезНДС = ra.Debkred ? (int)(ra.Sp344 * 100) : 0,
                               });
                return await (from r in регистр
                              group r by new { r.ФирмаId, r.НоменклатураId, r.СтатусПартииId, r.ПартияId, r.ДатаПартии, r.DateTimeIdDoc, r.ЦенаПрод } into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0
                                || (gr.Sum(x => x.начСуммаУпр) + gr.Sum(x => x.приходСуммаУпр) - gr.Sum(x => x.расходСуммаУпр)) != 0
                                || (gr.Sum(x => x.начСуммаРуб) + gr.Sum(x => x.приходСуммаРуб) - gr.Sum(x => x.расходСуммаРуб)) != 0
                                || (gr.Sum(x => x.начСуммаБезНДС) + gr.Sum(x => x.приходСуммаБезНДС) - gr.Sum(x => x.расходСуммаБезНДС)) != 0
                              select new РегистрПартииНаличие
                              {
                                  ФирмаId = gr.Key.ФирмаId,
                                  НоменклатураId = gr.Key.НоменклатураId,
                                  СтатусПартииId = gr.Key.СтатусПартииId,
                                  ПартияId = gr.Key.ПартияId,
                                  ДатаПартии = gr.Key.ДатаПартии,
                                  DateTimeIdDoc = gr.Key.DateTimeIdDoc,
                                  ЦенаПрод = gr.Key.ЦенаПрод,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000,
                                  СуммаУпр = (gr.Sum(x => x.начСуммаУпр) + gr.Sum(x => x.приходСуммаУпр) - gr.Sum(x => x.расходСуммаУпр)) / 100,
                                  СуммаРуб = (gr.Sum(x => x.начСуммаРуб) + gr.Sum(x => x.приходСуммаРуб) - gr.Sum(x => x.расходСуммаРуб)) / 100,
                                  СуммаБезНДС = (gr.Sum(x => x.начСуммаБезНДС) + gr.Sum(x => x.приходСуммаБезНДС) - gr.Sum(x => x.расходСуммаБезНДС)) / 100,
                              })
                              .ToListAsync();
            }
        }
        public async Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
            short LineNo,
            string ФирмаId,
            string НоменклатураId,
            string СтатусПартииId,
            string ПартияId,
            DateTime ДатаПартии,
            decimal ЦенаПрод,
            decimal Количество,
            decimal СуммаУпр,
            decimal СуммаРуб,
            decimal СуммаБезНДС,
            string КодОперации,
            decimal ПродСтоимость,
            decimal Выручка
            )
        {
            string startOfMonth = new DateTime(ДатаДок.Year, ДатаДок.Month, 1).ToShortDateString();
            await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA328_WriteDocAct @num36,@LineNo,@ActNo,@DebetCredit," +
                "@ФирмаId,@МОЛ,@НоменклатураId,@СтатусПартииId,@ПартияId,@ДатаПартии,@ЦенаПрод,@ПодСклад,@Количество,@СуммаУпр,@СуммаРуб,@СуммаБезНДС,@КодОперации,@ПродСтоимость,@Выручка," +
                "@docDate,@CurPeriod,1,0",
                new SqlParameter("@num36", IdDoc),
                new SqlParameter("@LineNo", LineNo),
                new SqlParameter("@ActNo", КоличествоДвижений),
                new SqlParameter("@DebetCredit", ДвижениеРасход),
                new SqlParameter("@ФирмаId", ФирмаId),
                new SqlParameter("@МОЛ", Common.ПустоеЗначение),
                new SqlParameter("@НоменклатураId", НоменклатураId),
                new SqlParameter("@СтатусПартииId", СтатусПартииId),
                new SqlParameter("@ПартияId", ПартияId),
                new SqlParameter("@ДатаПартии", ДатаПартии),
                new SqlParameter("@ЦенаПрод", ЦенаПрод),
                new SqlParameter("@ПодСклад", Common.ПустоеЗначение),
                new SqlParameter("@Количество", Количество),
                new SqlParameter("@СуммаУпр", СуммаУпр),
                new SqlParameter("@СуммаРуб", СуммаРуб),
                new SqlParameter("@СуммаБезНДС", СуммаБезНДС),
                new SqlParameter("@КодОперации", КодОперации),
                new SqlParameter("@ПродСтоимость", ПродСтоимость),
                new SqlParameter("@Выручка", Выручка),
                new SqlParameter("@docDate", ДатаДок.ToShortDateString()),
                new SqlParameter("@CurPeriod", startOfMonth));
            return true;
        }
    }
}
