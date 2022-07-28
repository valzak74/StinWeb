using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager.Справочники;

namespace StinWeb.Models.Repository.Интерфейсы
{
    public interface IUser : IDisposable
    {
        IQueryable<User> GetUsers();
        User GetUserByRowId(int RowId);
        Task<User> GetUserByRowIdAsync(int RowId);
        User GetUserById(string Id);
        Task<User> GetUserByIdAsync(string Id);
        Task<User> GetUserAsync(string name, string password);
        void InsertUser(User user);
        void DeleteUser(int userRowId);
        void UpdateUser(User user);
        void Save();
    }
}
