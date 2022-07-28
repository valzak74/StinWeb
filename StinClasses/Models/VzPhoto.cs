using System;
using System.Collections.Generic;

namespace StinClasses.Models
{
    public partial class VzPhoto
    {
        public int RowId { get; set; }
        public string Id { get; set; }
        public byte[] Photo { get; set; }
        public string Extension { get; set; }
    }
}
