using StinClasses.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StinClasses.Регистры
{
    public interface IРегистрКнигаПродаж : IDisposable
    {
        Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
                    string КредДокументId13,
                    string СтавкаНДСId,
                    string СтатусПартииId,
                    decimal СуммаНДС,
                    decimal СуммаРуб,
                    string КодОперации,
                    string ДокументОплатыId13
                    );
    }
    public class Регистр_КнигаПродаж : IРегистрКнигаПродаж
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
        public Регистр_КнигаПродаж(StinDbContext context)
        {
            _context = context;
        }
        private string ВидДолга(string СтатусПартииId)
        {
            if (СтатусПартииId == Common.СтатусПартии.Where(x => x.Value == "Товар (принятый)").Select(x => x.Key).FirstOrDefault())
                return "   5IW   "; //Долг за товара принятые в рознице
            else if (СтатусПартииId == Common.СтатусПартии.Where(x => x.Value == "Товар (в рознице)").Select(x => x.Key).FirstOrDefault())
                return "   4XV   ";  //Долг за товары в рознице
            else
                return "    B1   "; //Долг за товары
        }
        public async Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
            string КредДокументId13,
            string СтавкаНДСId,
            string СтатусПартииId,
            decimal СуммаНДС,
            decimal СуммаРуб,
            string КодОперации,
            string ДокументОплатыId13
            )
        {
            string startOfMonth = new DateTime(ДатаДок.Year, ДатаДок.Month, 1).ToShortDateString();
            await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA4343_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                "@КредДокументId13,@СтавкаНДСId,@ВидДолга,@СуммаНДС,@СуммаРуб,@СуммаНП," +
                "@КодОперации,@ДокументОплатыId13,@СтавкаНПId," +
                "@docDate,@CurPeriod,1,0",
                new SqlParameter("@num36", IdDoc),
                new SqlParameter("@ActNo", КоличествоДвижений),
                new SqlParameter("@DebetCredit", ДвижениеРасход),
                new SqlParameter("@КредДокументId13", КредДокументId13),
                new SqlParameter("@СтавкаНДСId", СтавкаНДСId),
                new SqlParameter("@ВидДолга", ВидДолга(СтатусПартииId)),
                new SqlParameter("@СуммаНДС", СуммаНДС),
                new SqlParameter("@СуммаРуб", СуммаРуб),
                new SqlParameter("@СуммаНП", Common.zero),
                new SqlParameter("@КодОперации", КодОперации),
                new SqlParameter("@ДокументОплатыId13", ДокументОплатыId13),
                new SqlParameter("@СтавкаНПId", Common.СтавкаНПбезНалога),
                new SqlParameter("@docDate", ДатаДок.ToShortDateString()),
                new SqlParameter("@CurPeriod", startOfMonth));
            return true;
        }
    }
}
