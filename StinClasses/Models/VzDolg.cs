using System;
using System.Collections.Generic;

namespace StinClasses.Models
{
    public partial class VzDolg
    {
        public string IdDoc { get; set; }
        public string DocName { get; set; }
        public string DocNo { get; set; }
        public DateTime DocDate { get; set; }
        public DateTime DateOplata { get; set; }
        public decimal Cost { get; set; }
        public decimal SumDolg { get; set; }
        public int Flag { get; set; }
    }
}
