using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StinClasses.Models;

namespace StinClasses.Справочники
{
    public class Пользователь
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public string Role { get; set; }
    }
    public interface IПользователь : IDisposable
    {
        Пользователь GetUserById(string Id);
        Task<Пользователь> GetUserByIdAsync(string Id);
        Task<Пользователь> GetUserByRowIdAsync(int RowId);
        string Постфикс(string userId);
    }
    public class ПользовательEntity : IПользователь
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
        public ПользовательEntity(StinDbContext context)
        {
            _context = context;
        }
        Пользователь Map(Sc30 entity)
        {
            if (entity == null)
                return null;
            return new Пользователь
            {
                Id = entity.Id,
                Name = entity.Code.Trim(),
                Password = entity.Sp13677.Trim(),
                FullName = entity.Descr.Trim(),
                Department = entity.Sp13679.Trim(),
                Role = entity.Sp13678.Trim(),
            };
        }
        public Пользователь GetUserById(string Id)
        {
            var entity = _context.Sc30s.Where(x => x.Id == Id && !x.Ismark).SingleOrDefault();
            return Map(entity);
        }
        public async Task<Пользователь> GetUserByIdAsync(string Id)
        {
            var entity = await _context.Sc30s.Where(x => x.Id == Id && !x.Ismark).SingleOrDefaultAsync();
            return Map(entity);
        }
        public async Task<Пользователь> GetUserByRowIdAsync(int RowId)
        {
            return await _context.Sc30s.Where(x => x.RowId == RowId).Select(entity => new Пользователь
            {
                Id = entity.Id,
                Name = entity.Code.Trim(),
                Password = entity.Sp13677.Trim(),
                FullName = entity.Descr.Trim(),
                Department = entity.Sp13679.Trim(),
                Role = entity.Sp13678.Trim(),
            }).FirstOrDefaultAsync();
        }
        public string Постфикс(string userId)
        {
            var p = (from u in _context.Sc30s
                     join f in _context.Sc9506s on u.Sp11726 equals f.Id
                     where u.Id == userId
                     select new
                     {
                         Постфикс = f.Sp12274.Trim(),
                         ПостфиксАльт = f.Sp12926.Trim()
                     }).FirstOrDefault();
            if (p == null)
                return "1";
            else if (p.ПостфиксАльт.Length > 0)
                return p.ПостфиксАльт;
            else if (p.Постфикс.Length > 0)
                return p.Постфикс;
            else
                return "1";
        }
    }
}
