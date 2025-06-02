using JsonExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace YandexClasses
{
    public class StatsOrdersRequest
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
        public DateTime? DateFrom {  get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
        public DateTime? DateTo { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
        public DateTime? UpdateFrom { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
        public DateTime? UpdateTo { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<long> Orders { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<StatusYandex> Statuses { get; set; }
        public bool HasCis { get; set; }
    }
    public class StatsOrdersResponse
    {
        public ResponseStatus Status { get; set; }
        public List<Error> Errors { get; set; }
        public OrderStatsResult Result { get; set; }
    }
    public class OrderStatsResult
    {
        public List<OrdersStatsOrder> Orders { get; set; }
        public Paging Paging { get; set; }
    }
    public class OrdersStatsOrder
    {
        public long Id { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
        public DateTime? CreationDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:ss.fffzzz")]
        public DateTime? StatusUpdateDate { get; set; }
        public StatusYandex Status { get; set; }
        public string PartnerOrderId { get; set; }
        public PaymentType PaymentType { get; set; }
        public DeliveryRegion DeliveryRegion { get; set; }
        public List<OrderStatsItem> Items { get; set; }
        public List<OrderStatsItem> InitialItems { get; set; }
        public List<OrderStatsPayment> Payments { get; set; }
        public List<OrderStatsCommission> Commissions { get; set; }
    }
    public class OrderStatsCommission
    {
        public OrderStatsCommissionType Type { get; set; }
        public decimal Actual { get; set; }
    }
    public class OrderStatsPayment
    {
        public int Id { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
        public DateTime? Date { get; set; }
        public OrderStatsPaymentType Type { get; set; }
        public PriceTypeYandex Source { get; set; }
        public decimal Total { get; set; }
        public PaymentOrder PaymentOrder { get; set; }
    }
    public class PaymentOrder
    {
        public string Id { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
        public DateTime? Date { get; set; }
    }
    public class OrderStatsItem
    {
        public string OfferName { get; set; }
        public long MarketSku { get; set; }
        public string ShopSku { get; set; }
        public int Count { get; set; }
        public List<OrderStatsPrice> Prices { get; set; }
        public OrderStatsWarehouse Warehouse { get; set; }
        public List<OrderStatsDetail> Details { get; set; }
        public List<string> CisList { get; set; }
        public int InitialCount { get; set; }
        public int BidFee { get; set; }
    }
    public class OrderStatsDetail
    {
        public ItemStatus ItemStatus { get; set; }
        public int ItemCount { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
        public DateTime? UpdateDate { get; set; }
        public StockType StockType { get; set; }
    }
    public class OrderStatsWarehouse
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class OrderStatsPrice
    {
        public PriceTypeYandex Type { get; set; }
        public decimal CostPerItem { get; set; }
        public decimal Total { get; set; }
    }
    public class DeliveryRegion
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum PriceTypeYandex 
    {
        NotFound = -1,
        BUYER = 0,
        CASHBACK = 1,
        MARKETPLACE = 2,
        SPASIBO = 3
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum ItemStatus
    {
        NotFound = -1,
        REJECTED = 0,
        RETURNED = 1,
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum OrderStatsPaymentType
    {
        NotFound = -1,
        PAYMENT = 0,
        REFUND = 1,
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum OrderStatsCommissionType
    {
        NotFound = -1,
        AGENCY = 0,
        FEE = 1,
        FULFILLMENT = 2
    }
}
