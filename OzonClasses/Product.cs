using JsonExtensions;
using Newtonsoft.Json;

namespace OzonClasses
{
    public class ProductInfoRequest
    {
        public string? Offer_id { get; set; }
        public long? Product_id { get; set; }
        public long? Sku { get; set; }
        public bool ShouldSerializeOffer_id()
        {
            return !string.IsNullOrEmpty(Offer_id);
        }
        public bool ShouldSerializeProduct_id()
        {
            return Product_id.HasValue && Product_id.Value > 0;
        }
        public bool ShouldSerializeSku()
        {
            return Sku.HasValue && Sku.Value > 0;
        }
    }
    public class ProductInfoResponse
    {
        public ProductInfoResult? Result { get; set; }
    }
    public class ProductInfoResult : ProductInfoItem
    {
        public bool Is_prepayment { get; set; }
        public bool Is_prepayment_allowed { get; set; }
        public double Volume_weight { get; set; }
        public List<Commission>? Commissions { get; set; }
    }
    public class Commission
    {
        public double DeliveryAmount { get; set; }
        public double MinValue { get; set; }
        public double Percent { get; set; }
        public double ReturnAmount { get; set; }
        public string? SaleSchema { get; set; }
        public double Value { get; set; }
    }
    public class ProductInfoListRequest
    {
        public List<string>? Offer_id { get; set; }
        public List<long>? Product_id { get; set; }
        public List<long>? Sku { get; set; }
    }
    public class ProductInfoListResponse
    {
        public ProductInfoListResult? Result { get; set; }
    }
    public class ProductInfoListResult
    {
        public List<ProductInfoItem>? Items { get; set; }
    }
    public class ProductInfoItem
    {
        public string? Barcode { get; set; }
        public string? Buybox_price { get; set; }
        public long? Category_id { get; set; }
        public string? Color_image { get; set; }
        [JsonProperty("Created_at", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime Created_at { get; set; }
        public long? Fbo_sku { get; set; }
        public long? Fbs_sku { get; set; }
        public long? Id { get; set; }
        public List<string>? Images { get; set; }
        public string? Primary_image { get; set; }
        public List<string>? Images360 { get; set; }
        public bool Is_kgt { get; set; }
        public string? Marketing_price { get; set; }
        public string? Min_ozon_price { get; set; }
        public string? Min_price { get; set; }
        public string? Name { get; set; }
        public string? Offer_id { get; set; }
        public string? Old_price { get; set; }
        public string? Premium_price { get; set; }
        public string? Price { get; set; }
        public string? Price_index { get; set; }
        public string? Recommended_price { get; set; }
        public ProductStatus? Status { get; set; }
        public List<ProductSource>? Sources { get; set; }
        public ProductStock? Stocks { get; set; }
        public string? Vat { get; set; }
        public VisibilityDetail? Visibility_details { get; set; }
        public bool? Visible { get; set; }
    }
    public class ProductListRequest
    {
        public ProductFilter? Filter { get; set; }
        public string? Last_id { get; set; }
        public long Limit { get; set; }
    }
    public class ProductListResponse
    {
        public ProductListResult? Result { get; set; }
    }

    public class ProductListResult
    {
        public List<Item>? Items { get; set; }
        public string? Last_id { get; set; }
        public int Total { get; set; }
    }

    public class ProductFilter
    {
        public List<string> Offer_id { get; set; }
        public List<long> Product_id { get; set; }
        public RequestFilterVisibility Visibility { get; set; }
        public ProductFilter()
        {
            Offer_id = new List<string>();
            Product_id = new List<long>();
        }
    }

    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum RequestFilterVisibility
    {
        NotFound = 0,
        ALL = 1,
        VISIBLE = 2,
        INVISIBLE = 3,
        EMPTY_STOCK = 4,
        NOT_MODERATED = 5,
        MODERATED = 6,
        DISABLED = 7,
        STATE_FAILED = 8,
        READY_TO_SUPPLY = 9,
        VALIDATION_STATE_PENDING = 10,
        VALIDATION_STATE_FAIL = 11,
        VALIDATION_STATE_SUCCESS = 12,
        TO_SUPPLY = 13,
        IN_SALE = 14,
        REMOVED_FROM_SALE = 15,
        BANNED = 16,
        OVERPRICED = 17,
        CRITICALLY_OVERPRICED = 18,
        EMPTY_BARCODE = 19,
        BARCODE_EXISTS = 20,
        QUARANTINE = 21,
        ARCHIVED = 22,
        OVERPRICED_WITH_STOCK = 23,
        PARTIAL_APPROVED = 24,
        IMAGE_ABSENT = 25,
        MODERATION_BLOCK = 26
    }
    public class ProductStatus
    {
        public string? State { get; set; }
        public string? State_failed { get; set; }
        public string? Moderate_status { get; set; }
        public List<string>? Decline_reasons { get; set; }
        public string? Validation_state { get; set; }
        public string? State_name { get; set; }
        public string? State_description { get; set; }
        public bool Is_failed { get; set; }
        public bool Is_created { get; set; }
        public string? State_tooltip { get; set; }
        public List<ProductError>? Item_errors { get; set; }
        [JsonProperty("State_updated_at", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime State_updated_at { get; set; }
    }
    public class ProductError
    {
        public string? Code { get; set; }
        public string? State { get; set; }
        public string? Level { get; set; }
        public string? Description { get; set; }
        public string? Field { get; set; }
        public long? Attribute_id { get; set; }
        public string? Attribute_name { get; set; }
    }
    public class ProductSource
    {
        public bool? Is_enabled { get; set; }
        public long? Sku { get; set; }
        public string? Source { get; set; }
    }
    public class ProductStock
    {
        public int? Coming { get; set; }
        public int? Present { get; set; }
        public int? Reserved { get; set; }
    }
    public class VisibilityDetail
    {
        public bool? Active_product { get; set; }
        public bool? Has_price { get; set; }
        public bool? Has_stock { get; set; }
    }
}