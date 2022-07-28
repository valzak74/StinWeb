using JsonExtensions;
using Newtonsoft.Json;
using System;

namespace YandexClasses
{
    public class OrderResponseEntry
    {
        public bool Accepted { get; set; }
        public string Id { get; set; }
        public FaultReason Reason { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "dd-MM-yyyy")]
        public DateTime ShipmentDate { get; set; }
        public bool ShouldSerializeId()
        {
            return Accepted;
        }
        public bool ShouldSerializeReason()
        {
            return !Accepted;
        }
        public bool ShouldSerializeShipmentDate()
        {
            return ShipmentDate > DateTime.MinValue;
        }
    }
}
