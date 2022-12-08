using JsonExtensions;
using Newtonsoft.Json;

namespace WbClasses
{
    public class SuppliesList
    {
        public List<Supply>? Supplies { get; set; }
    }
    public class Supply
    {
        public string? SupplyId { get; set; }
    }
    public class AddToSupply
    {
        public List<string>? Orders { get; set; }
        public AddToSupply() => Orders = new List<string>();
        public AddToSupply(List<string> orders) => Orders = orders;
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
        pdf = 1,
        svg = 2
    }
}
