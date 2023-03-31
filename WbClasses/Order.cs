using JsonExtensions;
using Newtonsoft.Json;

namespace WbClasses
{
    public class ChangeOrderStatusRequest
    {
        public string? OrderId { get; set; }
        public WbStatus Status { get; set; }
        public List<SgTin>? Sgtin { get; set; }
        public class SgTin
        {
            public string? Code { get; set; }
            public int Numerator { get; set; }
            public int Denominator { get; set; }
            public long Sid { get; set; }
        }
    }
    public class ChangeOrderStatusErrorResponse: Response
    {
        public object? Data { get; set; }
    }
    public class OrderList
    {
        //public int Total { get; set; }
        public List<Order>? Orders { get; set; }
    }
    public class Order
    {
        public long Id { get; set; }
        public string? Rid { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime CreatedAt { get; set; }
        public long WarehouseId { get; set; }
        public string? SupplyId { get; set; }
        public List<string>? PrioritySc { get; set; }
        public List<string>? Offices { get; set; }
        public WbAddressDetails? Address { get; set; }
        public WbUserInfo? User { get; set; }
        public List<string>? Skus { get; set; }
        decimal _price;
        public decimal Price { get => _price; set { _price = value / 100; } }
        decimal _convertedPrice;
        public decimal ConvertedPrice { get => _convertedPrice; set { _convertedPrice = value / 100; } }
        public int CurrencyCode { get; set; }
        public int ConvertedCurrencyCode { get; set; }
        public string? OrderUID { get; set; }
        public WbDeliveryType DeliveryType { get; set; }
        public long NmId { get; set; }
        public long ChrtId { get; set; }
        public string? Article { get; set; }
        public bool IsLargeCargo { get; set; }
        [JsonConverter(typeof(SingleObjectOrArrayJsonConverter<object>))]
        public List<object>? Meta { get; set; }
        public class WbAddressDetails
        {
            public string? Province { get; set; }
            public string? Area { get; set; }
            public string? City { get; set; }
            public string? Street { get; set; }
            public string? Home { get; set; }
            public string? Flat { get; set; }
            public string? Entrance { get; set; }
            public double longitude { get; set; }
            public double latitude { get; set; }
        }
        public class WbUserInfo
        {
            public string? Fio { get; set; }
            public string? Phone { get; set; }
        }
    }
    public class StickerRequest
    { 
        public List<long>? Orders { get; set; }
        public StickerRequest(List<long>? orderIds) => Orders = orderIds;
    }
    public class StickerResponse: Response
    {
        public List<WbBarcode>? Stickers { get; set; }
    }
    public class OrderStatusResponse
    {
        public List<OrderStatus>? Orders { get; set; }
        public class OrderStatus
        {
            public long Id { get; set; }
            public WbSupplierStatus SupplierStatus { get; set; }
            public WbStatus WbStatus { get; set; }
        }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum WbStatus
    {
        NotFound = -1,
        waiting = 0,
        sorted = 1,
        sold = 2,
        canceled = 3,
        canceled_by_client = 4,
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum WbSupplierStatus
    {
        NotFound = -1,
        [JsonProperty("new")]
        newOrder = 0,
        confirm = 1,
        complete = 2,
        cancel = 3,
        deliver = 4,
        receive = 5,
        reject = 6
}
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum WbDeliveryType
    {
        NotFound = -1,
        dbs = 1,
        fbs = 2
    }
}
