using System;
using System.Collections.Generic;

namespace StinClasses.Models
{
    public partial class VzTovarImage
    {
        public int RowId { get; set; }
        public string Id { get; set; }
        public string Filename { get; set; }
        public string Url { get; set; }
        public byte[] Photo { get; set; }
        public string Extension { get; set; }
        public bool IsMain { get; set; }
    }
}
