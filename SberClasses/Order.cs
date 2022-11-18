
using JsonExtensions;
using Newtonsoft.Json;
using System.Reflection.Metadata.Ecma335;

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
        public List<SberError>? Error { get; set; }
        public SberResponse() 
        {
            Data = new SberData();
            Meta= new Meta();
        }
        //public bool ShouldSerializeError() => Error != null;
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
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")] 
        public DateTime ShippingDate { get; set; }
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

}