using JsonExtensions;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace YandexClasses
{
    public class SkuDetailsRequest
    {
        public List<string> ShopSkus { get; set; }
    }
    public class SkuDetailsResponse
    {
        public ResponseStatus Status { get; set; }
        public SkuDetailsResult Result { get; set; }
        public List<Error> Errors { get; set; }
    }
    public class SkuDetailsResult
    {
        public List<SkuDetails> ShopSkus { get; set; }
    }
    public class SkuDetails
    {
        public string ShopSku { get; set; }
        public long MarketSku { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public long CategoryId { get; set; }
        public string CategoryName { get; set; }
        public WeightDimensions WeightDimensions { get; set; }
        public List<Hiding> Hidings { get; set; }
        public List<Warehouse> Warehouses { get; set; }
        public List<Tariff> Tariffs { get; set; }
    }
    public class Tariff
    {
        public TariffType Type { get; set; }
        public double Percent { get; set; }
        public double Amount { get; set; }
    }
    public class Warehouse
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public List<Stock> Stocks { get; set; }
    }
    public class Stock
    {
        public StockType Type { get; set; }
        public double Count { get; set; }
    }
    public class Hiding
    {
        public string Type { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public string Comment { get; set; }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum TariffType
    {
        NotFound,
        AGENCY_COMMISSION = 1,
        FEE = 2,
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum StockType
    {
        NotFound,
        AVAILABLE = 1,
        DEFECT = 2,
        EXPIRED = 3,
        FIT = 4,
        FREEZE = 5,
        QUARANTINE = 6,
        SUGGEST = 7,
        TRANSIT = 8
    }
}
