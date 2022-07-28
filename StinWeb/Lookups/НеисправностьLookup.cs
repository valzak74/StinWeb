using System;
using System.Linq;
using NonFactors.Mvc.Lookup;
using StinWeb.Models.DataManager.Справочники;
using StinClasses.Models;

namespace StinWeb.Lookups
{
    public class НеисправностьLookup : ALookup<Неисправность>
    {
        private StinDbContext _context { get; }
        public НеисправностьLookup(StinDbContext context)
        {
            _context = context;
            FilterCase = LookupFilterCase.Lower;
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
        public override IQueryable<Неисправность> GetModels()
        {
            IQueryable<Неисправность> table = from sc9866 in _context.Sc9866s
                                              where sc9866.Ismark == false && sc9866.Descr.Trim() != ""
                                              orderby sc9866.Descr
                                              select new Неисправность
                                              {
                                                  Id = sc9866.Id,
                                                  Наименование = sc9866.Descr.Trim()
                                              };
            return table;
        }
    }
    public class ПриложенныйДокументLookup : ALookup<ПриложенныйДокумент>
    {
        private StinDbContext _context { get; }
        public ПриложенныйДокументLookup(StinDbContext context)
        {
            _context = context;
            FilterCase = LookupFilterCase.Lower;
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
        public override IQueryable<ПриложенныйДокумент> GetModels()
        {
            IQueryable<ПриложенныйДокумент> table = from sc13750 in _context.Sc13750s
                                                    where sc13750.Ismark == false && sc13750.Descr.Trim() != ""
                                                    orderby sc13750.Descr
                                                    select new ПриложенныйДокумент
                                                    {
                                                        Id = sc13750.Id,
                                                        Наименование = sc13750.Descr.Trim(),
                                                        ФлагГарантии = sc13750.Sp13748
                                                    };
            return table;
        }
    }
}
