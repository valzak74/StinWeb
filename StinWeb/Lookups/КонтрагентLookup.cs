using System;
using System.Linq;
using NonFactors.Mvc.Lookup;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.Repository.Справочники;
using StinWeb.Models.Repository.Интерфейсы;
using StinClasses.Models;

namespace StinWeb.Lookups
{
    public class КонтрагентLookup : ALookup<Контрагент>
    {
        private КонтрагентRepository _контрагентRepository { get; }
        public КонтрагентLookup(StinDbContext context)
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
        public override IQueryable<Контрагент> GetModels()
        {
            return _контрагентRepository.GetAll();
        }
    }
    public class МенеджерLookup : ALookup<Менеджер>
    {
        private КонтрагентRepository _контрагентRepository { get; }
        public МенеджерLookup(StinDbContext context)
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
        public override IQueryable<Менеджер> GetModels()
        {
            return _контрагентRepository.GetAllManagers();
        }
    }
    public class ГруппаКонтрагентовLookup : ALookup<ГруппаКонтрагентов>
    {
        private КонтрагентRepository _контрагентRepository { get; }
        public ГруппаКонтрагентовLookup(StinDbContext context)
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
        public override IQueryable<ГруппаКонтрагентов> GetModels()
        {
            return _контрагентRepository.GetAllCustomerGroups();
        }
    }
    public class ТелефонLookup : ALookup<Телефон>
    {
        private IКонтрагент _контрагентRepository { get; }
        public ТелефонLookup(StinDbContext context)
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
        public override IQueryable<Телефон> GetModels()
        {
            return _контрагентRepository.Телефоны();
        }
        public override IQueryable<Телефон> FilterByAdditionalFilters(IQueryable<Телефон> models)
        {
            if (!string.IsNullOrEmpty((string)Filter.AdditionalFilters["КонтрагентId"]))
                models = models.Where(model => model.КонтрагентId == Filter.AdditionalFilters["КонтрагентId"].ToString());

            return models; //base.FilterByAdditionalFilters(models); 
        }
    }
    public class EmailLookup : ALookup<Email>
    {
        private IКонтрагент _контрагентRepository { get; }
        public EmailLookup(StinDbContext context)
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
        public override IQueryable<Email> GetModels()
        {
            return _контрагентRepository.Emails();
        }
        public override IQueryable<Email> FilterByAdditionalFilters(IQueryable<Email> models)
        {
            if (!string.IsNullOrEmpty((string)Filter.AdditionalFilters["КонтрагентId"]))
                models = models.Where(model => model.КонтрагентId == Filter.AdditionalFilters["КонтрагентId"].ToString());

            return models; //base.FilterByAdditionalFilters(models); 
        }
    }

}
