using System;
using System.Collections.Generic;

namespace StinClasses.Models
{
    public partial class VzUpdatingStock
    {
        public int RowId { get; set; }
        public string MuId { get; set; }
        public bool Flag { get; set; }
        public bool Taken { get; set; }
        public bool IsError { get; set; }
        public DateTime Updated { get; set; }
    }
}
