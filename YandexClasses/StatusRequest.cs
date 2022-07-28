using JsonExtensions;
using Newtonsoft.Json;
using System;

namespace YandexClasses
{
    public class StatusRequest
    {
        public OrderStatus Order { get; set; }
    }
    public class OrderStatus
    {
        public StatusYandex Status { get; set; }
        public SubStatusYandex Substatus { get; set; }
        public StatusDelivery Delivery { get; set; }
        public bool ShouldSerializeSubstatus()
        {
            return Substatus != SubStatusYandex.NotFound;
        }
        public bool ShouldSerializeDelivery()
        {
            return Delivery != null;
        }
    }
    public class StatusDelivery
    {
        public StatusDates Dates { get; set; }
    }
    public class StatusDates
    {
        [JsonConverter(typeof(DateFormatConverter), "dd-MM-yyyy HH:mm:ss")]
        public DateTime RealDeliveryDate { get; set; }
    }
}
