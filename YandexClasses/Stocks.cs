using JsonExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace YandexClasses
{
    public class RequestStocks
    {
        public string WarehouseId { get; set; }
        public string PartnerWarehouseId { get; set; }
        public List<string> Skus { get; set; }
    }
    public class ResponseStocks
    {
        public List<SkuEntry> Skus { get; set; }
        public ResponseStocks()
        {
            Skus = new List<SkuEntry>();
        }
    }
    public class SkuEntry
    {
        public string Sku { get; set; }
        public string WarehouseId { get; set; }
        public List<SkuItem> Items { get; set; }
    }
    public class SkuItem
    {
        public ItemType Type { get; set; }
        public string Count { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime UpdatedAt { get; set; }
    }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ItemType
    {
        FIT = 1,
    }

}
