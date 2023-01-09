
using JsonExtensions;
using Newtonsoft.Json;

namespace SberClasses
{
    public class SberRequest
    {
        public Meta? Meta { get; set; }
        public SberData? Data { get; set; }
        public SberRequest() 
        {
            Meta = new Meta();
            Data = new SberData();
        }
        public SberRequest(string token)
        {
            Meta = new Meta();
            Data = new SberData { Token = token };
        }
    }
    public class SberResponse
    {
        public SberData? Data { get; set;}
        public Meta? Meta { get; set; }
        public int Success { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(SingleObjectOrArrayJsonConverter<SberError>))]
        public List<SberError>? Error { get; set; }
        public SberResponse() 
        {
            Data = new SberData();
            Meta= new Meta();
        }
    }
    public class SberResponseSingleError: SberResponse
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public new object? Error { get; set; }
    }
    public class SberError
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Code { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Message { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? ShipmentId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? ItemIndex { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? BoxCode { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Description { get; set; }
    }
    public class SberData
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Token { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? Result { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<SberShipment>? Shipments { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] 
        public int? MerchantId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? PrintAsPdf { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SberReason? Reason { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<SberStock>? Stocks { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<SberPrice>? Prices { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<object>? Warnings { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? SavedPrices { get; set; }
    }
    public class SberStock
    {
        public string? OfferId { get; set; }
        public int Quantity { get; set; }
    }
    public class SberPrice
    {
        public string? OfferId { get; set; }
        public decimal Price { get; set; }
        public bool IsDeleted { get; set; }
    }
    public class SberShipping
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime? ShippingDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? ShippingPoint { get; set; }
    }

    public class SberLabel
    {
        public string? DeliveryId { get; set; }
        public string? Region { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }
        public string? FullName { get; set; }
        public string? MerchantName { get; set; }
        public int MerchantId { get; set; }
        public string? ShipmentId { get; set; }
        private DateTime _shippingDate;
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime ShippingDate { get { return _shippingDate; } set { _shippingDate = value.ToUniversalTime().Date; } }
        public string? DeliveryType { get; set; }
        public string? LabelText { get; set; }
    }
    public class SberShipment
    {
        public string? ShipmentId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? OrderCode { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")] 
        public DateTime ShipmentDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] 
        public List<SberItem>? Items { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<SberBox>? Boxes { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? BoxCodes { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SberLabel? Label { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SberShipping? Shipping { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? FulfillmentMethod { get; set; }
    }

    public class SberItem
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? ItemIndex { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? GoodsId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? OfferId { get; set; } 
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? ItemName { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal? Price { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal? FinalPrice { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<Discount>? Discounts { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? Quantity { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? TaxRate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] 
        public bool? ReservationPerformed { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] 
        public bool? IsDigitalMarkRequired { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<SberBox>? Boxes { get; set; }
    }
    public class SberBox
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? BoxIndex { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] 
        public string? BoxCode { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] 
        public List<SberBoxParam>? Params { get; set; }
    }
    public class SberBoxParam
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsKgt { get; set; }
    }
    public class Discount
    {
        public string? DiscountType { get; set; }
        public string? DiscountDescription { get; set; }
        public decimal DiscountAmount { get; set; }
    }
    public class OrderListRequest
    {
        public OrderListData? Data { get; set; }
        public Meta? Meta { get; set; }
        public OrderListRequest()
        {
            Meta = new Meta();
            Data = new OrderListData();
        }
        public OrderListRequest(string token): this()
        {
            Data = new OrderListData { Token = token };
        }
    }
    public class OrderListData
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Token { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime? DateFrom { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime? DateTo { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? Count { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<SberStatus>? Statuses { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Shipments { get; set; }
    }
    public class OrderListResponse
    {
        public SberOrderListData? Data { get; set; }
        public Meta? Meta { get; set; }
        public int Success { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(SingleObjectOrArrayJsonConverter<SberError>))]
        public List<SberError>? Error { get; set; }
    }
    public class OrderListDetailResponse: OrderListResponse
    {
        public new SberOrderListDetailData? Data { get; set; }
    }
    public class SberOrderListDetailData: SberOrderListData
    {
        public new List<SberDetailOrder>? Shipments { get; set; }
    }
    public class SberOrderListData
    {
        public List<string>? Shipments { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(SingleObjectOrArrayJsonConverter<object>))]
        public List<object>? Warnings { get; set; }
    }
    public class SberDetailOrder
    {
        public string? ShipmentId { get; set; }
        public string? OrderCode { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime ConfirmedTimeLimit { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime PackingTimeLimit { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime ShippingTimeLimit { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime ShipmentDateFrom { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime ShipmentDateTo { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime PackingDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime ReserveExpirationDate { get; set; }
        public string? DeliveryId { get; set; }
        public bool ShipmentDateShift { get; set; }
        public bool ShipmentIsChangeable { get; set; }
        public string? CustomerFullName { get; set; }
        public string? CustomerAddress { get; set; }
        public string? ShippingPoint { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime CreationDate { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime DeliveryDate { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime DeliveryDateFrom { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime DeliveryDateTo { get; set; }
        public string? DeliveryMethodId { get; set; }
        public string? ServiceScheme { get; set; }
        public decimal DepositedAmount { get; set; }
        public List<SberDetailItem>? Items { get; set; }
    }
    public class SberDetailItem
    {
        public string? ItemIndex { get; set; }
        public SberStatus Status { get; set; }
        public SberSubStatus SubStatus { get; set; }
        public decimal? Price { get; set; }
        public decimal? FinalPrice { get; set; }
        public List<Discount>? Discounts { get; set; }
        public int? Quantity { get; set; }
        public string? OfferId { get; set; }
        public string? GoodsId { get; set; }
        public GoodsInfo? GoodsData { get; set; }
        public object? BoxIndex { get; set; } 
        public List<SberEvent>? Events { get; set; }
    }
    public class GoodsInfo
    {
        public string? Name { get; set; }
        public string? categoryName { get; set; }
    }
    public class SberEvent
    {
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime EventDate { get; set; }
        public string? EventName { get; set; }
        public string? EventValue { get; set; }

    }
    public class Meta
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? RequestId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? FromProxy { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Source { get; set; }
    }
    public enum SberReason
    {
        OUT_OF_STOCK = 0,
        INCORRECT_PRICE = 1,
        INCORRECT_PRODUCT = 2,
        INCORRECT_SPEC = 3,
        TWICE_ORDER = 4,
        NOT_TIME_FOR_SHIPPING = 5,
        FRAUD_ORDER = 6
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum SberStatus
    {
        NotFound = -1,
        NEW = 0,
        CONFIRMED = 1,
        PACKED = 2,
        PACKING_EXPIRED = 3,
        SHIPPED = 4,
        DELIVERED = 5,
        MERCHANT_CANCELED = 6,
        CUSTOMER_CANCELED = 7,
        PENDING_CONFIRMATION = 8,
        PENDING_PACKING = 9,
        PENDING_SHIPPING = 10,
        SHIPPING_EXPIRED = 11,
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum SberSubStatus
    {
        NotFound = -1,
        LATE_REJECT = 0,
        CONFIRMATION_REJECT = 1,
        CONFIRMATION_EXPIRED = 2,
        PACKING_EXPIRED = 3
    }
}