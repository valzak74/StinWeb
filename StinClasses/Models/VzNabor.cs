using System;
using System.Collections.Generic;

namespace StinClasses.Models
{
    public partial class VzNabor
    {
        public string IdDoc { get; set; }
        public string ZIdDoc { get; set; }
        public DateTime DocDate { get; set; }
        public decimal DocSumma { get; set; }

        public virtual VzZayavki ZIdDocNavigation { get; set; }
    }
}
