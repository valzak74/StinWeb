using System;
using System.Collections.Generic;
using JsonExtensions;
using Newtonsoft.Json;

namespace YandexClasses
{
    public class OrderRequestEntry
    {
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
        public Buyer Buyer { get; set; }
        public OrderDelivery Delivery { get; set; }
        public List<RequestedItem> Items { get; set; }
        public string Notes { get; set; }
        public OrderRequestEntry()
        {
            Items = new List<RequestedItem>();
        }
    }
}
