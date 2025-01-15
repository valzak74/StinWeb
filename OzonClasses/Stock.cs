using JsonExtensions;
using Newtonsoft.Json;

namespace OzonClasses
{
    public class OzonStockRequest
    {
        public List<StockRequest> Stocks { get; set; }
        public OzonStockRequest()
        {
            Stocks = new List<StockRequest>();
        }
    }
    public class StockRequest
    {
        public string? Offer_id { get; set; }
        public long Product_id { get; set; }
        public long Stock { get; set; }
        public long Warehouse_id { get; set; }
        public bool ShouldSerializeWarehouse_id() => Warehouse_id > 0;
    }
    public class OzonStockResponse
    {
        public List<StockResult>? Result { get; set; }
    }
    public class StockResult
    {
        public List<ErrorResponse>? Errors { get; set; }
        public string? Offer_id { get; set; }
        public long Product_id { get; set; }
        public bool Updated { get; set; }
        public long Warehouse_id { get; set; }
    }
    public class StockInfoRequest
    {
        public StockFilter? Filter { get; set;}
        public string? Last_id { get; set; }
        public long Limit { get; set; }
        public StockInfoRequest() 
        {
            Filter = new StockFilter() { Visibility = Visibility.ALL };
            Limit = 1000;
        }
        public StockInfoRequest(List<string> offerIds)
        {
            Filter = new StockFilter() { Offer_id = offerIds, Visibility = Visibility.ALL };
            Limit = 1000;
        }
        public class StockFilter
        {
            public List<string>? Offer_id { get; set; }
            public List<long>? Product_id { get; set; }
            public Visibility Visibility { get; set; }
        }
        [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
        public enum Visibility
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
    }
    public class StockInfoResponse
    {
        public string? Cursor { get; set; }
        public List<StockInfoItem>? Items { get; set; }
    }
    public class StockInfoResult
    {
        public List<StockInfoItem>? Items { get; set; }
        public string? Last_id { get; set; }
        public int Total { get; set; }
    }
    public class StockInfoItem
    {
        public string? Offer_id { get; set; }
        public long Product_id { get; set; }
        public List<StockInfo>? Stocks { get; set; }
        public class StockInfo
        {
            public int Present { get; set; }
            public int Reserved { get; set; }
            public StockType? Type { get; set; }
        }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum StockType
    {
        NotFound = 0,
        fbo = 1,
        fbs = 2,
        crossborder = 3,
    }
}
