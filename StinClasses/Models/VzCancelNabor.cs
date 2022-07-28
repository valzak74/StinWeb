using System;
using System.Collections.Generic;

namespace StinClasses.Models
{
    public partial class VzCancelNabor
    {
        public string Iddoc { get; set; }
        public int DocType { get; set; }
        public string ZIdDoc { get; set; }
        public DateTime DocDate { get; set; }
        public decimal DocSumma { get; set; }

        public virtual VzZayavki ZIdDocNavigation { get; set; }
    }
}
