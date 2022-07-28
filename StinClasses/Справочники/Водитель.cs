using Microsoft.EntityFrameworkCore;
using StinClasses.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StinClasses.Справочники
{
    public class Водитель
    {
        public string Id { get; set; }
        public string Наименование { get; set; }
        public string ФИО { get; set; }
        public string ВодительскоеУдостоверение { get; set; }
    }
    public interface IВодитель : IDisposable
    {
        Task<Водитель> GetВодительByIdAsync(string Id);
    }
    public class ВодительEntity : IВодитель
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
        public ВодительEntity(StinDbContext context)
        {
            _context = context;
        }
        public async Task<Водитель> GetВодительByIdAsync(string Id)
        {
            var entity = await _context.Sc10253s.FirstOrDefaultAsync(x => x.Id == Id && !x.Ismark);
            if (entity != null)
            {
                return new Водитель
                {
                    Id = entity.Id,
                    Наименование = entity.Descr.Trim(),
                    ФИО = entity.Sp10251.Trim(),
                    ВодительскоеУдостоверение = entity.Sp11813.Trim()
                };
            }
            return null;
        }
    }
}
