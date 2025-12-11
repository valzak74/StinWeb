using JsonExtensions;
using Newtonsoft.Json;

namespace WbClasses
{
    public class SuppliesList
    {
        public long Next { get; set; }
        public List<Supply>? Supplies { get; set; }
    }
    public class SupplyName
    {
        public string? Name { get; set; }
    }
    public class Supply
    {
        public string? Id { get; set; }
        public bool Done { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime? CreatedAt { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime? ClosedAt { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime? ScanDt { get; set; }
        public string? Name { get; set; }
        public bool? IsLargeCargo { get; set; }
        public long? DestinationOfficeId { get; set; }
    }
    public class AddToSupply
    {
        public List<long>? Orders { get; set; }
        public AddToSupply() => Orders = new List<long>();
        public AddToSupply(string order)
        {
            if (long.TryParse(order, out var result))
                Orders = new List<long> { result };
            else 
                Orders = new List<long>();
        }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum WbSupplyStatus
    {
        NotFound = -1,
        ACTIVE = 1,
        ON_DELIVERY = 2
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum WbSupplyBarcodeType
    {
        NotFound = -1,
        svg = 1,
        zplv = 2,
        zplh = 3,
        png = 4,
    }
}
