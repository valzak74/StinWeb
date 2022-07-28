using System;
using System.Collections.Generic;

namespace StinClasses.Models
{
    public partial class VzTovar
    {
        public string Id { get; set; }
        public decimal? Rozn { get; set; }
        public decimal? RoznSp { get; set; }
        public decimal? Opt { get; set; }
        public decimal? OptSp { get; set; }
        public decimal? Zakup { get; set; }
    }
}
