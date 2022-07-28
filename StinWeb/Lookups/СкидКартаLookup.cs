using System;
using System.Linq;
using NonFactors.Mvc.Lookup;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.Repository.Справочники;
using StinClasses.Models;

namespace StinWeb.Lookups
{
    public class СкидКартаLookup : ALookup<СкидКарта>
    {
        private КонтрагентRepository _контрагентRepository { get; }
        public СкидКартаLookup(StinDbContext context)
        {
            this._контрагентRepository = new КонтрагентRepository(context);
            FilterCase = LookupFilterCase.Lower;
        }
        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _контрагентRepository.Dispose();
                }
            }
            this.disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public override IQueryable<СкидКарта> GetModels()
        {
            return _контрагентRepository.ВсеСкидКарты();
        }
    }
}
