using System;
using System.Collections.Generic;

namespace StinClasses.Models
{
    public partial class Sc10548
    {
        public int RowId { get; set; }
        public string Id { get; set; }
        public string Parentid { get; set; }
        public string Code { get; set; }
        public string Descr { get; set; }
        public string Parentext { get; set; }
        public byte Isfolder { get; set; }
        public bool Ismark { get; set; }
        public int Verstamp { get; set; }
    }
}
