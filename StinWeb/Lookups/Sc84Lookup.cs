using System;
using System.Collections.Generic;
using System.Linq;
using NonFactors.Mvc.Lookup;
using StinClasses.Models;

namespace StinWeb.Lookups
{
    public class Sc84Lookup : ALookup<Sc84>
    {
        private StinDbContext Context { get; }

        public Sc84Lookup(StinDbContext context)
        {
            Context = context;
            FilterCase = LookupFilterCase.Lower;
        }

        public override IQueryable<Sc84> GetModels()
        {
            return Context.Set<Sc84>();
        }
        public override Dictionary<String, String> FormData(Sc84 model)
        {
            Dictionary<String, String> data = base.FormData(model);
            data["Label"] = "(" + model.Sp85.Trim() + ") " + model.Descr.Trim();

            return data;
        }
    }
}
