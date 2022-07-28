using StinClasses.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace StinClasses.Регистры
{
    public interface IРегистрПродажи : IDisposable
    {
        Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений,
            string ФирмаId,
            string ПокупательId,
            string ПоставщикId,
            string НоменклатураId,
            string СкладId,
            decimal Себестоимость,
            decimal ПродСтоимость,
            decimal Количество,
            decimal СебестоимостьВ,
            decimal ПродСтоимостьВ,
            decimal КоличествоВ,
            decimal СебестоимостьБезНдс,
            decimal СебестоимостьВБезНдс
            );
    }
    public class Регистр_Продажи : IРегистрПродажи
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
        public Регистр_Продажи(StinDbContext context)
        {
            _context = context;
        }
        public async Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений,
            string ФирмаId,
            string ПокупательId,
            string ПоставщикId,
            string НоменклатураId,
            string СкладId,
            decimal Себестоимость,
            decimal ПродСтоимость,
            decimal Количество,
            decimal СебестоимостьВ,
            decimal ПродСтоимостьВ,
            decimal КоличествоВ,
            decimal СебестоимостьБезНдс,
            decimal СебестоимостьВБезНдс
            )
        {
            string startOfMonth = new DateTime(ДатаДок.Year, ДатаДок.Month, 1).ToShortDateString();
            await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA2351_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                "@НоменклатураId,@ПокупательId,@ПоставщикId,@ФирмаId,@СкладId,@Себестоимость,@ПродСтоимость,@Количество," +
                "@СебестоимостьВ,@ПродСтоимостьВ,@КоличествоВ," +
                "@СебестоимостьБезНдс,@СебестоимостьВБезНдс," +
                "@docDate,@CurPeriod,1,0",
                new SqlParameter("@num36", IdDoc),
                new SqlParameter("@ActNo", КоличествоДвижений),
                new SqlParameter("@DebetCredit", false),
                new SqlParameter("@НоменклатураId", НоменклатураId),
                new SqlParameter("@ПокупательId", ПокупательId),
                new SqlParameter("@ПоставщикId", ПоставщикId),
                new SqlParameter("@ФирмаId", ФирмаId),
                new SqlParameter("@СкладId", СкладId),
                new SqlParameter("@Себестоимость", Себестоимость),
                new SqlParameter("@ПродСтоимость", ПродСтоимость),
                new SqlParameter("@Количество", Количество),
                new SqlParameter("@СебестоимостьВ", СебестоимостьВ),
                new SqlParameter("@ПродСтоимостьВ", ПродСтоимостьВ),
                new SqlParameter("@КоличествоВ", КоличествоВ),
                new SqlParameter("@СебестоимостьБезНдс", СебестоимостьБезНдс),
                new SqlParameter("@СебестоимостьВБезНдс", СебестоимостьВБезНдс),
                new SqlParameter("@docDate", ДатаДок.ToShortDateString()),
                new SqlParameter("@CurPeriod", startOfMonth));
            return true;
        }
    }
}
