using JsonExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YandexClasses
{
    public class OrderDetailsResponse
    {
        public ResponseStatus Status { get; set; }
        public List<Error> Errors { get; set; }
        public DetailOrder Order { get; set; }
    }
    public class OrderDetailListResponse
    {
        public ResponseStatus Status { get; set; }
        public List<Error> Errors { get; set; }
        public List<DetailOrder> Orders { get; set; }
        public Pager Pager { get; set; }
    }
    public class DetailOrder
    {
        public bool CancelRequested { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "dd-MM-yyyy HH:mm:ss")]
        public DateTime CreationDate { get; set; }
        public CurrencyType Currency { get; set; }
        public bool Fake { get; set; }
        public long Id { get; set; }
        public double ItemsTotal { get; set; }
        public PaymentType PaymentType { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public StatusYandex Status { get; set; }
        public SubStatusYandex SubStatus { get; set; }
        public TaxSystem TaxSystem { get; set; }
        public double Total { get; set; }
        public double SubsidyTotal { get; set; }
        public double DeliveryTotal { get; set; }
        public bool IsBooked { get; set; }
        public OrderDelivery Delivery { get; set; }
        public List<RequestedItem> Items { get; set; }
        public string Notes { get; set; }
    }
}
