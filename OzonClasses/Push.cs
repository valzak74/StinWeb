using JsonExtensions;
using Newtonsoft.Json;

namespace OzonClasses
{
    public class PushRequest
    {
        public PushType Message_type { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime? Time { get; set; }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum PushType
    {
        NotFound = 0,
        TYPE_PING = 1,
        TYPE_NEW_POSTING = 2,
        TYPE_POSTING_CANCELLED = 3,
        TYPE_STATE_CHANGED = 4,
        TYPE_CUTOFF_DATE_CHANGED = 5,
        TYPE_DELIVERY_DATE_CHANGED = 6,
        TYPE_CREATE_ITEM = 7,
        TYPE_UPDATE_ITEM = 8,
        TYPE_PRICE_INDEX_CHANGED = 9,
        TYPE_STOCKS_CHANGED = 10,
        TYPE_NEW_MESSAGE = 11,
        TYPE_UPDATE_MESSAGE = 12,
        TYPE_MESSAGE_READ = 13,
        TYPE_CHAT_CLOSED = 14
    }
    public class PushResponse
    {
        public bool? Result { get; set; }
        public string? Version { get; set; }
        public string? Name { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime? Time { get; set; }
        public PushError? Error { get; set; }
        public bool ShouldSerializeResult() => Result.HasValue;
        public bool ShouldSerializeVersion() => Version != null;
        public bool ShouldSerializeName() => Name != null;
        public bool ShouldSerializeTime() => Time.HasValue && Time.Value >= DateTime.MinValue;
        public bool ShouldSerializeError() => Error != null;
        public class PushError
        {
            public ErrorCode Code { get; set; }
            public string? Message { get; set; }
            public string? Details { get; set; }
            public PushError() => Code = ErrorCode.ERROR_UNKNOWN;
            [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
            public enum ErrorCode
            {
                NotFound = 0,
                ERROR_UNKNOWN = 1,
                ERROR_PARAMETER_VALUE_MISSED = 2,
                ERROR_REQUEST_DUPLICATED = 3
            }
        }
    }
}