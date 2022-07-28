using System;
using System.Collections.Generic;

namespace StinClasses.Models
{
    public partial class VzZayavki
    {
        public VzZayavki()
        {
            VzCancelNabors = new HashSet<VzCancelNabor>();
            VzCancelZayavkis = new HashSet<VzCancelZayavki>();
            VzInvoices = new HashSet<VzInvoice>();
            VzNabors = new HashSet<VzNabor>();
            VzPayments = new HashSet<VzPayment>();
            VzProdagis = new HashSet<VzProdagi>();
        }

        public string IdDoc { get; set; }
        public string ZIdDoc { get; set; }
        public string ZNo { get; set; }
        public DateTime? ZDate { get; set; }
        public DateTime DocDate { get; set; }
        public string DocCustomer { get; set; }
        public string DocManager { get; set; }
        public string DocCustomerName { get; set; }
        public string DocManagerName { get; set; }

        public virtual ICollection<VzCancelNabor> VzCancelNabors { get; set; }
        public virtual ICollection<VzCancelZayavki> VzCancelZayavkis { get; set; }
        public virtual ICollection<VzInvoice> VzInvoices { get; set; }
        public virtual ICollection<VzNabor> VzNabors { get; set; }
        public virtual ICollection<VzPayment> VzPayments { get; set; }
        public virtual ICollection<VzProdagi> VzProdagis { get; set; }
    }
}
