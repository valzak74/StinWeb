using JsonExtensions;
using Newtonsoft.Json;

namespace WbClasses
{
    public class CardsListRequest
    {
        public CardsListRequest(int limit) => Settings = new RequestData(limit);
        public CardsListRequest(int limit, long nmId, DateTime updatedAt) => Settings = new RequestData(limit, nmId, updatedAt);
        public RequestData? Settings { get; set; }
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
                public bool Ascending { get; set; }
                public RequestSort() => Ascending = true;
            }
            public class RequestFilter
            {
                [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
                public string? TextSearch { get; set; }
                public int WithPhoto { get; set; }
                [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
                public List<long> TagIDs { get; set; }
                public bool AllowedCategoriesOnly { get; set; }
                [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
                public List<long> ObjectIDs { get; set; }
                [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
                public List<string> Brands { get; set; }
                [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
                public long ImtID { get; set; }
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
        public List<Card>? Cards { get; set; } 
        public ResponseCursor? Cursor { get; set; }
        public class Card
        {
            public long NmID { get; set; }
            public long ImtID { get; set; }
            public long SubjectID { get; set; }
            public string? VendorCode { get; set; }
            public string? SubjectName { get; set; }
            public string? Brand { get; set; }
            public string? Title { get; set; }
            public List<Photo>? Photos { get; set; }
            public string? Video { get; set; }
            public Dimension? Dimensions { get; set; }
            public List<Characteristic>? Characteristics { get; set; }
            public List<Size>? Sizes { get; set; }
            public List<Tag>? Tags { get; set; }
            [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
            public DateTime CreatedAt { get; set; }
            [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
            public DateTime UpdatedAt { get; set; }
            public class Photo
            {
                public string? Big { get; set; }
                public string? Small { get; set; }
            }
            public class Dimension
            {
                public long Length { get; set; }
                public long Width { get; set; }
                public long Height { get; set; }
            }
            public class Characteristic
            {
                public long Id { get; set; }
                public string? Name { get; set; }
                [JsonConverter(typeof(SingleObjectOrArrayJsonStringConverter))]

                public List<string>? Value { get; set; }
            }
            public class Size
            {
                public long ChrtID { get; set; }
                public string? TechSize { get; set; }
                public string? WbSize { get; set; }
                public List<string>? Skus { get; set; }
            }
            public class Tag
            {
                public long Id { get; set; }
                public string? Name { get; set; }
                public WbColor Color { get; set; }
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

    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum WbColor: byte
    {
        NotFound,
        D1CFD7,
        FEE0E0,
        ECDAFF,
        E4EAFF,
        DEF1DD,
        FFECC7,
    }
}
