using System;
using System.Collections.Generic;

namespace StinClasses.Models
{
    public partial class VzOrderBinary
    {
        public int RowId { get; set; }
        public string Id { get; set; }
        public byte[] Binary { get; set; }
        public string Extension { get; set; }
    }
}
