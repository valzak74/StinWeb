using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using StinClasses.Models;
using System;
using System.Threading.Tasks;

namespace StinClasses.Регистры
{
    public interface IРегистрКасса : IDisposable
    {
        Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
                    string ФирмаId,
                    string КассаId,
                    decimal Сумма,
                    string КодОперации,
                    string ДдсId
                    );
    }
    public class Регистр_Касса : IРегистрКасса
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
        public Регистр_Касса(StinDbContext context)
        {
            _context = context;
        }
        public async Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
            string ФирмаId,
            string КассаId,
            decimal Сумма,
            string КодОперации,
            string ДдсId
            )
        {
            string startOfMonth = new DateTime(ДатаДок.Year, ДатаДок.Month, 1).ToShortDateString();
            await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA635_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                "@ФирмаId,@КассаId,@ВалютаId,@СуммаВал,@СуммаУпр,@СуммаРуб,@КодОперации,@ДдсId," +
                "@docDate,@CurPeriod,1,0",
                new SqlParameter("@num36", IdDoc),
                new SqlParameter("@ActNo", КоличествоДвижений),
                new SqlParameter("@DebetCredit", ДвижениеРасход),
                new SqlParameter("@ФирмаId", ФирмаId),
                new SqlParameter("@КассаId", КассаId),
                new SqlParameter("@ВалютаId", Common.ВалютаРубль),
                new SqlParameter("@СуммаВал", Сумма),
                new SqlParameter("@СуммаУпр", Сумма),
                new SqlParameter("@СуммаРуб", Сумма),
                new SqlParameter("@КодОперации", КодОперации),
                new SqlParameter("@ДдсId", ДдсId),
                new SqlParameter("@docDate", ДатаДок.ToShortDateString()),
                new SqlParameter("@CurPeriod", startOfMonth));
            return true;
        }
    }
}
