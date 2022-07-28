using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StinWeb.Models.Repository
{
    public interface IDataRepository<TEntity>
    {
        IEnumerable<TEntity> GetAll();
        TEntity Get(string id);
        //TDto GetDto(long id);
        //void Add(TEntity entity);
        //void Update(TEntity entityToUpdate, TEntity entity);
        //void Delete(TEntity entity);
    }
}
