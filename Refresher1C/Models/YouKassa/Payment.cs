using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Refresher1C.Models.YouKassa
{
    public class Payment
    {
        public string Id { get; set; }
        public PaymentStatus Status { get; set; }
        public bool Paid { get; set; }
        public DateTime Created_At { get; set; }
        public string Description { get; set; }
        public ReceiptRegistrationStatus? Receipt_Registration { get; set; }
        public DateTime? Captured_At { get; set; }
        public DateTime? Expires_At { get; set; }
        public PaymentMethod Payment_Method { get; set; }
        public bool? Test { get; set; }
        public Amount Amount { get; set; }
        public Amount Income_Amount { get; set; }
        public Amount Refunded_Amount { get; set; }
        public CancellationDetails Cancellation_Details { get; set; }
        public AuthorizationDetails Authorization_Details { get; set; }
        public Payment()
        {

        }
    }
}
