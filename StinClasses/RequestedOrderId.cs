using JsonExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StinClasses
{
    public class RequestedOrderId
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public ReceiverPaymentType PaymentType { get; set; }
        public string ReceiverEmail { get; set; }
        public string ReceiverPhone { get; set; }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum ReceiverPaymentType
    {
        NotFound = -1,
        Наличными = 0,
        БанковскойКартой = 1
    }
}
