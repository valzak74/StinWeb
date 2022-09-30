using System;
using System.Collections.Generic;

namespace StinClasses.Models
{
    public partial class VzUpdatingPrice
    {
        public int RowId { get; set; }
        public string MuId { get; set; }
        public bool Flag { get; set; }
        public DateTime Updated { get; set; }
    }
}
