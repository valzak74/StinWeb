using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StinClasses.Models;

namespace StinClasses.Справочники
{
    public class Кладовщик
    {
        public string Id { get; set; }
        public string Наименование { get; set; }
        public Склад Склад { get; set; }
        public bool Уволен { get; set; }
    }
    public interface IКладовщик : IDisposable
    {
        Task<Кладовщик> GetКладовщикByIdAsync(string Id);
    }
    public class КладовщикEntity : IКладовщик
    {
        private StinDbContext _context;
        private IСклад _склад;
        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _склад.Dispose();
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
        public КладовщикEntity(StinDbContext context)
        {
            _context = context;
            _склад = new СкладEntity(context);
        }
        public async Task<Кладовщик> GetКладовщикByIdAsync(string Id)
        {
            var entity = await _context.Sc12558s.FirstOrDefaultAsync(x => x.Id == Id && !x.Ismark);
            if (entity != null)
            {
                return new Кладовщик
                {
                    Id = entity.Id,
                    Наименование = entity.Descr.Trim(),
                    Склад = entity.Sp12555 != Common.ПустоеЗначение ? await _склад.GetEntityByIdAsync(entity.Sp12555) : null,
                    Уволен = entity.Sp12556 == 1
                };
            }
            return null;
        }
    }
}
