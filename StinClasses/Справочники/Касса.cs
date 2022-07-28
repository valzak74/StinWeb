using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StinClasses.Models;

namespace StinClasses.Справочники
{
    public class Касса
    {
        public string Id { get; set; }
        public string Наименование { get; set; }
    }
    public interface IКасса : IDisposable
    {
        Task<Касса> GetYouKassaAsync();
    }
    public class КассаEntity : IКасса
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
        public КассаEntity(StinDbContext context)
        {
            _context = context;
        }
        public async Task<Касса> GetYouKassaAsync()
        {
            string РежимOnline = Common.РежимыККТ.Where(s => s.Value == "OnLine").Select(t => t.Key).FirstOrDefault();
            return await _context.Sc1809s
                .Where(x => x.Sp3356 == РежимOnline)
                .Select(x => new Касса
                {
                    Id = x.Id,
                    Наименование = x.Descr.Trim()
                })
                .FirstOrDefaultAsync();
        }
    }
}
