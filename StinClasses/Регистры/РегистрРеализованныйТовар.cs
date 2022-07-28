using StinClasses.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace StinClasses.Регистры
{
    public interface IРегистрРеализованныйТовар : IDisposable
    {
        Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
                    string ФирмаId,
                    string ДоговорId,
                    string НоменклатураId,
                    string ПартияId,
                    string ДокПродажиId13,
                    decimal Количество,
                    decimal ПродСтоимость,
                    decimal Вознаграждение
                    );
    }
    public class Регистр_РеализованныйТовар : IРегистрРеализованныйТовар
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
        public Регистр_РеализованныйТовар(StinDbContext context)
        {
            _context = context;
        }
        public async Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
            string ФирмаId,
            string ДоговорId,
            string НоменклатураId,
            string ПартияId,
            string ДокПродажиId13,
            decimal Количество,
            decimal ПродСтоимость,
            decimal Вознаграждение
            )
        {
            string startOfMonth = new DateTime(ДатаДок.Year, ДатаДок.Month, 1).ToShortDateString();
            await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA438_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                "@ФирмаId,@ДоговорId,@НоменклатураId,@ПартияId,@ДокПродажиId13,@Количество," +
                "@ПродСтоимость,@Вознаграждение," +
                "@docDate,@CurPeriod,1,0",
                new SqlParameter("@num36", IdDoc),
                new SqlParameter("@ActNo", КоличествоДвижений),
                new SqlParameter("@DebetCredit", ДвижениеРасход),
                new SqlParameter("@ФирмаId", ФирмаId),
                new SqlParameter("@ДоговорId", ДоговорId),
                new SqlParameter("@НоменклатураId", НоменклатураId),
                new SqlParameter("@ПартияId", ПартияId),
                new SqlParameter("@ДокПродажиId13", ДокПродажиId13),
                new SqlParameter("@Количество", Количество),
                new SqlParameter("@ПродСтоимость", ПродСтоимость),
                new SqlParameter("@Вознаграждение", Вознаграждение),
                new SqlParameter("@docDate", ДатаДок.ToShortDateString()),
                new SqlParameter("@CurPeriod", startOfMonth));
            return true;
        }
    }
}
