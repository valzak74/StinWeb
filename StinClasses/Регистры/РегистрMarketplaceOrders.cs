using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using StinClasses.Models;

namespace StinClasses.Регистры
{
    public class РегистрMarketplaceOrders
    {
        public string ФирмаId { get; set; }
        public string OrderId { get; set; }
        public decimal Status { get; set; }
        public string НоменклатураMarketplaceId { get; set; }
        public string НоменклатураId { get; set; }
        public string WarehouseId { get; set; }
        public string PartnerWarehouseId { get; set; }
        public bool Delivery { get; set; }
        public decimal Количество { get; set; }
        public decimal Сумма { get; set; }
        public decimal СуммаСоСкидкой { get; set; }
    }
    public interface IРегистрMarketplaceOrders : IDisposable
    {
        Task<List<РегистрMarketplaceOrders>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string фирмаId, string orderId, List<string> номенклатураIds);
        Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
            string ФирмаId,
            string OrderId,
            decimal Status,
            string НоменклатураId,
            string НоменклатураMarketplaceId,
            string WarehouseId,
            string PartnerWarehouseId,
            bool Delivery,
            decimal Количество,
            decimal Сумма,
            decimal СуммаСоСкидкой,
            bool СнятСайтом
            );
    }
    public class Регистр_MarketplaceOrders : IРегистрMarketplaceOrders
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
        public Регистр_MarketplaceOrders(StinDbContext context)
        {
            _context = context;
        }
        public async Task<List<РегистрMarketplaceOrders>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string фирмаId, string orderId, List<string> номенклатураIds)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = DateTime.Now;
            if (dateReg >= _context.GetDateTA())
            {
                DateTime dateRegTA = _context.GetRegTA();
                return await (from r in _context.Rg14021s
                              where r.Period == dateRegTA &&
                                (string.IsNullOrEmpty(фирмаId) ? true : r.Sp14009 == фирмаId) &&
                                (string.IsNullOrEmpty(orderId) ? true : r.Sp14010 == orderId) &&
                                (((номенклатураIds != null) && (номенклатураIds.Count > 0)) ? номенклатураIds.Contains(r.Sp14012) : true)
                              group r by new { r.Sp14009, r.Sp14010, r.Sp14011, r.Sp14012, r.Sp14013, r.Sp14014, r.Sp14015, r.Sp14016 } into gr
                              where (gr.Sum(x => x.Sp14017) != 0) || (gr.Sum(x => x.Sp14018) != 0) || (gr.Sum(x => x.Sp14019) != 0)
                              select new РегистрMarketplaceOrders
                              {
                                  ФирмаId = gr.Key.Sp14009,
                                  OrderId = gr.Key.Sp14010,
                                  Status = gr.Key.Sp14011,
                                  НоменклатураId = gr.Key.Sp14012,
                                  НоменклатураMarketplaceId = gr.Key.Sp14013,
                                  WarehouseId = gr.Key.Sp14014,
                                  PartnerWarehouseId = gr.Key.Sp14015,
                                  Delivery = gr.Key.Sp14016 == 1,
                                  Количество = gr.Sum(x => x.Sp14017),
                                  Сумма = gr.Sum(x => x.Sp14018),
                                  СуммаСоСкидкой = gr.Sum(x => x.Sp14019)
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
                var регистр = (from rg in _context.Rg14021s
                               where rg.Period == previousRegPeriod &&
                                (string.IsNullOrEmpty(фирмаId) ? true : rg.Sp14009 == фирмаId) &&
                                (string.IsNullOrEmpty(orderId) ? true : rg.Sp14010 == orderId) &&
                                (((номенклатураIds != null) && (номенклатураIds.Count > 0)) ? номенклатураIds.Contains(rg.Sp14012) : true)
                               select new
                               {
                                   ФирмаId = rg.Sp14009,
                                   OrderId = rg.Sp14010,
                                   Status = rg.Sp14011,
                                   НоменклатураId = rg.Sp14012,
                                   НоменклатураMarketplaceId = rg.Sp14013,
                                   WarehouseId = rg.Sp14014,
                                   PartnerWarehouseId = rg.Sp14015,
                                   Delivery = rg.Sp14016 == 1,
                                   начОстаток = (int)(rg.Sp14017 * 100000),
                                   приход = 0,
                                   расход = 0,
                                   начСумма = (int)(rg.Sp14018 * 100),
                                   приходСумма = 0,
                                   расходСумма = 0,
                                   начСуммаСоСкидкой = (int)(rg.Sp14019 * 100),
                                   приходСуммаСоСкидкой = 0,
                                   расходСуммаСоСкидкой = 0,
                               })
                              .Concat
                              (from ra in _context.Ra14021s
                               join j in _context._1sjourns on ra.Iddoc equals j.Iddoc
                               where j.DateTimeIddoc.CompareTo(PeriodStart) >= 0 && (IncludeDeadLine ? j.DateTimeIddoc.CompareTo(PeriodEnd) <= 0 : j.DateTimeIddoc.CompareTo(PeriodEnd) < 0) &&
                                (string.IsNullOrEmpty(фирмаId) ? true : ra.Sp14009 == фирмаId) &&
                                (string.IsNullOrEmpty(orderId) ? true : ra.Sp14010 == orderId) &&
                                (((номенклатураIds != null) && (номенклатураIds.Count > 0)) ? номенклатураIds.Contains(ra.Sp14012) : true)
                               select new
                               {
                                   ФирмаId = ra.Sp14009,
                                   OrderId = ra.Sp14010,
                                   Status = ra.Sp14011,
                                   НоменклатураId = ra.Sp14012,
                                   НоменклатураMarketplaceId = ra.Sp14013,
                                   WarehouseId = ra.Sp14014,
                                   PartnerWarehouseId = ra.Sp14015,
                                   Delivery = ra.Sp14016 == 1,
                                   начОстаток = 0,
                                   приход = !ra.Debkred ? (int)(ra.Sp14017 * 100000) : 0,
                                   расход = ra.Debkred ? (int)(ra.Sp14017 * 100000) : 0,
                                   начСумма = 0,
                                   приходСумма = !ra.Debkred ? (int)(ra.Sp14018 * 100) : 0,
                                   расходСумма = ra.Debkred ? (int)(ra.Sp14018 * 100) : 0,
                                   начСуммаСоСкидкой = 0,
                                   приходСуммаСоСкидкой = !ra.Debkred ? (int)(ra.Sp14019 * 100) : 0,
                                   расходСуммаСоСкидкой = ra.Debkred ? (int)(ra.Sp14019 * 100) : 0,
                               });
                return await (from r in регистр
                              group r by new { r.ФирмаId, r.OrderId, r.Status, r.НоменклатураId, r.НоменклатураMarketplaceId, r.WarehouseId, r.PartnerWarehouseId, r.Delivery } into gr
                              where (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) != 0
                                || (gr.Sum(x => x.начСумма) + gr.Sum(x => x.приходСумма) - gr.Sum(x => x.расходСумма)) != 0
                                || (gr.Sum(x => x.начСуммаСоСкидкой) + gr.Sum(x => x.приходСуммаСоСкидкой) - gr.Sum(x => x.расходСуммаСоСкидкой)) != 0
                              select new РегистрMarketplaceOrders
                              {
                                  ФирмаId = gr.Key.ФирмаId,
                                  OrderId = gr.Key.OrderId,
                                  Status = gr.Key.Status,
                                  НоменклатураId = gr.Key.НоменклатураId,
                                  НоменклатураMarketplaceId = gr.Key.НоменклатураMarketplaceId,
                                  WarehouseId = gr.Key.WarehouseId,
                                  PartnerWarehouseId = gr.Key.PartnerWarehouseId,
                                  Delivery = gr.Key.Delivery,
                                  Количество = (gr.Sum(x => x.начОстаток) + gr.Sum(x => x.приход) - gr.Sum(x => x.расход)) / 100000,
                                  Сумма = (gr.Sum(x => x.начСумма) + gr.Sum(x => x.приходСумма) - gr.Sum(x => x.расходСумма)) / 100,
                                  СуммаСоСкидкой = (gr.Sum(x => x.начСуммаСоСкидкой) + gr.Sum(x => x.приходСуммаСоСкидкой) - gr.Sum(x => x.расходСуммаСоСкидкой)) / 100,
                              })
                              .ToListAsync();
            }
        }
        public async Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
            string ФирмаId,
            string OrderId,
            decimal Status,
            string НоменклатураId,
            string НоменклатураMarketplaceId,
            string WarehouseId,
            string PartnerWarehouseId,
            bool Delivery,
            decimal Количество,
            decimal Сумма,
            decimal СуммаСоСкидкой,
            bool СнятСайтом
            )
        {
            string startOfMonth = new DateTime(ДатаДок.Year, ДатаДок.Month, 1).ToShortDateString();
            await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA14021_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                "@ФирмаId,@OrderId,@Status,@НоменклатураId,@НоменклатураMarketplaceId,@WarehouseId,@PartnerWarehouseId,@Delivery,@Количество,@Сумма,@СуммаСоСкидкой,@СнятСайтом," +
                "@docDate,@CurPeriod,1,0",
                new SqlParameter("@num36", IdDoc),
                new SqlParameter("@ActNo", КоличествоДвижений),
                new SqlParameter("@DebetCredit", ДвижениеРасход),
                new SqlParameter("@ФирмаId", ФирмаId),
                new SqlParameter("@OrderId", OrderId),
                new SqlParameter("@Status", Status),
                new SqlParameter("@НоменклатураId", НоменклатураId),
                new SqlParameter("@НоменклатураMarketplaceId", НоменклатураMarketplaceId),
                new SqlParameter("@WarehouseId", WarehouseId),
                new SqlParameter("@PartnerWarehouseId", PartnerWarehouseId),
                new SqlParameter("@Delivery", Delivery ? 1 : 0),
                new SqlParameter("@Количество", Количество),
                new SqlParameter("@Сумма", Сумма),
                new SqlParameter("@СуммаСоСкидкой", СуммаСоСкидкой),
                new SqlParameter("@СнятСайтом", СнятСайтом ? 1 : 0),
                new SqlParameter("@docDate", ДатаДок.ToShortDateString()),
                new SqlParameter("@CurPeriod", startOfMonth));
            return true;
        }
    }
}
