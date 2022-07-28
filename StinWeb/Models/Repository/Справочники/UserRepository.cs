using System;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.Repository.Интерфейсы;
using StinWeb.Models.DataManager.Справочники;
using Microsoft.EntityFrameworkCore;
using StinClasses.Models;

namespace StinWeb.Models.Repository.Справочники
{
    public class UserRepository : IUser, IDisposable
    {
        private StinDbContext _context;
        private User FillUser(Sc30 entity)
        {
            if (entity != null)
                return new User
                {
                    RowId = entity.RowId,
                    Id = entity.Id,
                    Name = entity.Code.Trim(),
                    Password = entity.Sp13677.Trim(),
                    FullName = entity.Descr.Trim(),
                    Department = entity.Sp13679.Trim(),
                    Role = entity.Sp13678.Trim(),
                    ОсновнойСклад = new Склад { Id = entity.Sp873 },
                    ОсновнойПодСклад = new ПодСклад { Id = entity.Sp9855 },
                    ОсновнаяФирма = new Фирма { Id = entity.Sp4010 },
                    ОсновнаяКасса = new Касса { Id = entity.Sp2643 }
                };
            else
                return null;
        }
        public UserRepository(StinDbContext context)
        {
            this._context = context;
        }
        public IQueryable<User> GetUsers()
        {
            return from sc30 in _context.Sc30s
                   where sc30.Ismark == false
                   select FillUser(sc30);
        }
        public User GetUserByRowId(int RowId)
        {
            return FillUser(_context.Sc30s.Find(RowId));
        }
        public async Task<User> GetUserByRowIdAsync(int RowId)
        {
            return FillUser(await _context.Sc30s.FindAsync(RowId));
        }
        public User GetUserById(string Id)
        {
            return FillUser(_context.Sc30s.FirstOrDefault(x => x.Id == Id && x.Ismark == false));
        }
        public async Task<User> GetUserByIdAsync(string Id)
        {
            return FillUser(await _context.Sc30s.FirstOrDefaultAsync(x => x.Id == Id && x.Ismark == false));
        }
        public async Task<User> GetUserAsync(string name, string password)
        {
            return FillUser(await _context.Sc30s.FirstOrDefaultAsync(x => x.Code.Trim() == name && x.Sp13677.Trim() == password && x.Ismark == false));
        }
        public void InsertUser(User user)
        {
            //_context.Students.Add(student);
        }

        public void DeleteUser(int userRowId)
        {
            //Student student = context.Students.Find(studentID);
            //context.Students.Remove(student);
        }

        public void UpdateUser(User user)
        {
            //context.Entry(student).State = EntityState.Modified;
        }

        public void Save()
        {
            _context.SaveChanges();
        }
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
    }
}
