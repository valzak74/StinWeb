using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NonFactors.Mvc.Lookup;

namespace StinWeb.Lookups
{
    public class TemplateLookup<T> : ALookup<T> where T : class
    {
        public IList<T> Models { get; }
        public TemplateLookup(string name, string placeholder, string title, string url, List<string> additionalFilters = null)
        {
            Models = new List<T>();
            this.Filter.Rows = 15;
            //this.Filter.Offset = 22;
            this.Name = name;
            this.Placeholder = placeholder;
            this.Title = title;
            this.Url = url;
            this.AdditionalFilters.Clear();
            if (additionalFilters != null)
                this.AdditionalFilters = additionalFilters;
            //lookup.AdditionalFilters.Add("Add1");
            //lookup.AdditionalFilters.Add("Add2");
            //lookup.Filter.Order = LookupSortOrder.Desc;
        }
        public override IQueryable<T> GetModels()
        {
            return Models.AsQueryable();
        }
    }
}
