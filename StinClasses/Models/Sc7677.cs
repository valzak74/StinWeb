using System;
using System.Collections.Generic;

namespace StinClasses.Models
{
    public partial class Sc7677
    {
        public int RowId { get; set; }
        public string Id { get; set; }
        public string Parentid { get; set; }
        public string Code { get; set; }
        public string Descr { get; set; }
        public byte Isfolder { get; set; }
        public bool Ismark { get; set; }
        public int Verstamp { get; set; }
        public string Sp7667 { get; set; }
        public string Sp7668 { get; set; }
        public string Sp7669 { get; set; }
        public string Sp7665 { get; set; }
        public string Sp7666 { get; set; }
    }
}
