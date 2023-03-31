using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using StinClasses.Models;
using System;
using System.Threading.Tasks;

namespace StinClasses.Регистры
{
    public interface IРегистрРаботаКладовщика : IDisposable
    {
        Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений,
            string КладовщикId,
            decimal КолДокументов,
            decimal КолСтрокДокументов,
            decimal КолЕдиницНоменклатуры
            );
    }
    public class Регистр_РаботаКладовщика : IРегистрРаботаКладовщика
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
        public Регистр_РаботаКладовщика(StinDbContext context)
        {
            _context = context;
        }
        public async Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений,
            string КладовщикId,
            decimal КолДокументов,
            decimal КолСтрокДокументов,
            decimal КолЕдиницНоменклатуры
            )
        {
            string startOfMonth = new DateTime(ДатаДок.Year, ДатаДок.Month, 1).ToShortDateString();
            await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA12566_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                "@КладовщикId,@КолДокументов,@КолСтрокДокументов,@КолЕдиницНоменклатуры," +
                "@docDate,@CurPeriod,1,0",
                new SqlParameter("@num36", IdDoc),
                new SqlParameter("@ActNo", КоличествоДвижений),
                new SqlParameter("@DebetCredit", false),
                new SqlParameter("@КладовщикId", КладовщикId),
                new SqlParameter("@КолДокументов", КолДокументов),
                new SqlParameter("@КолСтрокДокументов", КолСтрокДокументов),
                new SqlParameter("@КолЕдиницНоменклатуры", КолЕдиницНоменклатуры),
                new SqlParameter("@docDate", ДатаДок.ToShortDateString()),
                new SqlParameter("@CurPeriod", startOfMonth));
            return true;
        }
    }
}
