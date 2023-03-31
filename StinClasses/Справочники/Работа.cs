using Microsoft.EntityFrameworkCore;
using StinClasses.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StinClasses.Справочники
{
    public class Работа
    {
        public string Id { get; set; }
        public string Наименование { get; set; }
        public string Артикул { get; set; }
        public string АртикулОригинал { get; set; }
    }
    public interface IРабота : IDisposable
    {
        Task<Работа> GetРаботаByIdAsync(string id);
    }
    public class РаботаEntity : IРабота
    {
        private StinDbContext _context;
        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            this.disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public РаботаEntity(StinDbContext context)
        {
            _context = context;
        }
        public async Task<Работа> GetРаботаByIdAsync(string id)
        {
            return await _context.Sc9875s
                .Where(x => x.Id == id)
                .Select(x => new Работа 
                { 
                    Id = x.Id,
                    Наименование = x.Descr.Trim(),
                    Артикул = x.Sp11503.Trim(),
                    АртикулОригинал = x.Sp12644.Trim(),
                }).SingleOrDefaultAsync();
        }
    }
}
