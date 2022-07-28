using JsonExtensions;
using Newtonsoft.Json;
using System;

namespace YandexClasses
{
    public class StatusResponse
    {
        public OrderResponse Order { get; set; }
    }
    public class OrderResponse
    {
        public bool CancelRequested { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "dd-MM-yyyy HH:mm:ss")]
        public DateTime CreationDate { get; set; }
        public bool Fake { get; set; }
        public StatusYandex Status { get; set; }
        public SubStatusYandex Substatus { get; set; }
    }
}
