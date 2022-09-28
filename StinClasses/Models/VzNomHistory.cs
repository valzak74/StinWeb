using System;
using System.Collections.Generic;

namespace StinClasses.Models
{
    public partial class VzNomHistory
    {
        public int RowId { get; set; }
        public string NomId { get; set; }
        public string Code { get; set; }
        public DateTime ChDate { get; set; }
        public string Descr { get; set; }
        public string Vendor { get; set; }
        public string Barcode { get; set; }
        public decimal? Weight { get; set; }
        public decimal? WeightB { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public decimal? Length { get; set; }
        public decimal? Boxes { get; set; }
        public string Brend { get; set; }
        public string Params { get; set; }
        public decimal? Quantum { get; set; }
    }
}
