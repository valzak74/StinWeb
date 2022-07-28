using StinClasses.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace StinClasses.Регистры
{
    public interface IРегистрНакопСкидка : IDisposable
    {
        Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений,
                    string НомерId,
                    decimal Стоимость
                    );
    }
    public class Регистр_НакопСкидка : IРегистрНакопСкидка
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
        public Регистр_НакопСкидка(StinDbContext context)
        {
            _context = context;
        }
        public async Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений,
            string НомерId,
            decimal Стоимость
            )
        {
            string startOfMonth = new DateTime(ДатаДок.Year, ДатаДок.Month, 1).ToShortDateString();
            await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA8677_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                "@НомерId,@Стоимость,@СтоимостьВ," +
                "@docDate,@CurPeriod,1,0",
                new SqlParameter("@num36", IdDoc),
                new SqlParameter("@ActNo", КоличествоДвижений),
                new SqlParameter("@DebetCredit", Стоимость < 0 ? true : false),
                new SqlParameter("@НомерId", НомерId),
                new SqlParameter("@Стоимость", Стоимость < 0 ? 0 : Стоимость),
                new SqlParameter("@СтоимостьВ", Стоимость < 0 ? Стоимость : 0),
                new SqlParameter("@docDate", ДатаДок.ToShortDateString()),
                new SqlParameter("@CurPeriod", startOfMonth));
            return true;
        }
    }
}
