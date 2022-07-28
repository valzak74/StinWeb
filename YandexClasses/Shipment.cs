using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JsonExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace YandexClasses
{
    public class Shipment
    {
        public long Id { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "dd-MM-yyyy")]
        public DateTime ShipmentDate { get; set; }
        public List<Box> Boxes { get; set; }
    }
}
