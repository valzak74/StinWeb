using System;
using System.Collections.Generic;
using System.Linq;
using NonFactors.Mvc.Lookup;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.Repository.Справочники;
using StinWeb.Models.Repository.Интерфейсы;
using StinClasses.Models;

namespace StinWeb.Lookups
{
    public class НоменклатураLookup : ALookup<Номенклатура>
    {
        private IНоменклатура _номенклатураRepository;
        public НоменклатураLookup(StinDbContext context)
        {
            this._номенклатураRepository = new НоменклатураRepository(context);
            FilterCase = LookupFilterCase.Lower;
        }
        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _номенклатураRepository.Dispose();
                }
            }
            this.disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public override IQueryable<Номенклатура> GetModels()
        {
            return _номенклатураRepository.GetAllWithBrend();
        }
        public override IQueryable<Номенклатура> FilterByAdditionalFilters(IQueryable<Номенклатура> models)
        {
            РежимВыбора? режим = (РежимВыбора?)Filter.AdditionalFilters["РежимВыбора"];
            if (режим != null || режим != РежимВыбора.Общий)
            {
                if (режим == РежимВыбора.ПоМастерской)
                    models = models.Where(model => model.Производитель.EndsWith("запчасти") || model.Производитель.EndsWith("запчасти*"));
                else
                    models = models.Where(model => !(model.Производитель.EndsWith("запчасти") || model.Производитель.EndsWith("запчасти*")));
            }
            if (!string.IsNullOrEmpty((string)Filter.AdditionalFilters["Производитель"]))
                models = models.Where(model => model.Производитель.StartsWith(Filter.AdditionalFilters["Производитель"].ToString()));
            if (!string.IsNullOrEmpty((string)Filter.AdditionalFilters["Артикул"]))
                models = models.Where(model => model.Артикул.StartsWith(Filter.AdditionalFilters["Артикул"].ToString()));

            return models; //base.FilterByAdditionalFilters(models); 
        }
        public override Dictionary<String, String> FormData(Номенклатура model)
        {
            Dictionary<String, String> data = base.FormData(model);
            data["Label"] = "| " + model.Артикул + " | " + model.Производитель + " | " + model.Наименование;

            return data;
        }
    }
    public class ПроизводительLookup : ALookup<Производитель>
    {
        private IНоменклатура _номенклатураRepository { get; }
        public ПроизводительLookup(StinDbContext context)
        {
            this._номенклатураRepository = new НоменклатураRepository(context);
            FilterCase = LookupFilterCase.Lower;
        }
        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _номенклатураRepository.Dispose();
                }
            }
            this.disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public override IQueryable<Производитель> GetModels()
        {
            return _номенклатураRepository.GetAllBrends();
        }
    }

}
