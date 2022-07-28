using NonFactors.Mvc.Lookup;
using StinClasses.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StinWeb.Lookups
{
    public class TemplateQueryLookup<T> : ALookup<T> where T : class
    {
        private IQueryable<T> _models;
        private StinDbContext _context { get; }
        private List<string> _propertyNames;
        public TemplateQueryLookup(StinDbContext context, IQueryable<T> query, List<string> propertyNames = null, LookupFilter filter = null)
        {
            _context = context;
            _models = query;
            _propertyNames = propertyNames;
            FilterCase = LookupFilterCase.Lower;
            if (filter != null)
                Filter = filter;
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
        public override IQueryable<T> GetModels()
        {
            return _models;
        }
        public override Dictionary<String, String> FormData(T model)
        {
            Dictionary<String, String> data = base.FormData(model);

            if (_propertyNames != null)
            {
                string label = "";
                var properties = typeof(T).GetProperties().AsEnumerable();
                foreach (var property in _propertyNames)
                {
                    var prop = properties.FirstOrDefault(x => x.Name == property);
                    if (prop != null)
                    {
                        if (!string.IsNullOrEmpty(label))
                            label += " | ";
                        label += prop.GetValue(model).ToString();
                    }
                }
                data["Label"] = label.Trim();
            }

            return data;
        }
    }
}
