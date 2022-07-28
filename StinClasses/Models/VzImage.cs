using System;
using System.Collections.Generic;

namespace StinClasses.Models
{
    public partial class VzImage
    {
        public string Id { get; set; }
        public byte[] Image { get; set; }
        public string Dtype { get; set; }
        public int RowId { get; set; }
    }
}
