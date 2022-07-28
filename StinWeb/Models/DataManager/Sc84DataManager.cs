using System.Collections.Generic;
using System.Linq;
using StinWeb.Models.Repository;
using StinClasses.Models;

namespace StinWeb.Models.DataManager
{
    public class Sc84DataManager: IDataRepository<Sc84>
    {
        readonly StinDbContext _DbContext;

        public Sc84DataManager(StinDbContext dbContext)
        {
            _DbContext = dbContext;
        }

        public IEnumerable<Sc84> GetAll()
        {
            return _DbContext.Sc84s
                //.Include(author => author.AuthorContact)
                .ToList();
        }

        public Sc84 Get(string id)
        {
            var sc84 = _DbContext.Sc84s
                .SingleOrDefault(b => b.Id == id);

            return sc84;
        }
        //public void Add(Sc84 entity)
        //{
        //    _DbContext.Sc84.Add(entity);
        //    _DbContext.SaveChanges();
        //}
        //public void Delete(Sc84 entity)
        //{
        //    _DbContext.Remove(entity);
        //    _DbContext.SaveChanges();
        //}
    }
}
