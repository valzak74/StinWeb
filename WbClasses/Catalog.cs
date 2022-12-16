using JsonExtensions;
using Newtonsoft.Json;

namespace WbClasses
{
    public class CardsListRequest
    {
        public CardsListRequest(int limit) => Sort = new RequestData(limit);
        public CardsListRequest(int limit, long nmId, DateTime updatedAt) => Sort = new RequestData(limit, nmId, updatedAt);
        public RequestData? Sort { get; set; }
        public class RequestData
        {
            public RequestData(int limit)
            {
                Cursor = new RequestCursor(limit);
                Filter= new RequestFilter();
            }
            public RequestData(int limit, long nmId, DateTime updatedAt)
            {
                Cursor = new RequestCursor(limit, nmId, updatedAt);
                Filter = new RequestFilter();
            }
            public RequestCursor? Cursor { get; set; }
            public RequestFilter? Filter { get; set; }
            public RequestSort? Sort { get; set;}
            public class RequestSort
            {
                public string? SortColumn { get; set; }
                public bool Ascending { get; set; }
                public RequestSort() => Ascending = true;
            }
            public class RequestFilter
            {
                [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
                public string? TextSearch { get; set; }
                public int WithPhoto { get; set; }
                public RequestFilter() => WithPhoto = -1;
            }
            public class RequestCursor
            {
                [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
                [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
                public DateTime? UpdatedAt { get; set; }
                [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
                public long? NmID { get; set; }
                public int Limit { get; set; }
                public RequestCursor() => Limit = 1000;
                public RequestCursor(int limit):this() => Limit = limit;
                public RequestCursor(int limit, long nmId, DateTime updatedAt):this()
                {
                    Limit = limit;
                    UpdatedAt = updatedAt;
                    NmID = nmId;
                }
            }
        }
    }
    public class CardListResponse: Response
    {
        public CardListData? Data { get; set; }
        public class CardListData
        {
            public List<Card>? Cards { get; set; } 
            public ResponseCursor? Cursor { get; set; }
            public class Card
            {
                public List<Size>? Sizes { get; set; }
                public List<string>? MediaFiles { get; set; }
                public List<string>? Colors { get; set; }
                [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
                public DateTime UpdatedAt { get; set; }
                public string? VendorCode { get; set; }
                public string? Brand { get; set; }
                public string? Object { get; set; }
                public long NmID { get; set; }
                public class Size
                {
                    public string? TechSize { get; set; }
                    public List<string>? Skus { get; set; }
                }
            }
            public class ResponseCursor
            {
                [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
                public DateTime UpdatedAt { get; set; }
                public long? NmID { get; set; }
                public int Total { get; set; }
            }
        }
    }
}
