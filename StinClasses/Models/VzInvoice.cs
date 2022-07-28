using System;
using System.Collections.Generic;

namespace StinClasses.Models
{
    public partial class VzInvoice
    {
        public string IdDoc { get; set; }
        public string ZIdDoc { get; set; }
        public string DocType { get; set; }
        public DateTime DocDate { get; set; }
        public decimal DocSumma { get; set; }
        public string DocCustomer { get; set; }
        public string DocManager { get; set; }
        public string DocCustomerName { get; set; }
        public string DocManagerName { get; set; }

        public virtual VzZayavki ZIdDocNavigation { get; set; }
    }
}
