using JsonExtensions;
using Newtonsoft.Json;

namespace AtolOnlineClasses
{
    public class SellResponse
    {
        [JsonConverter(typeof(DateFormatConverter), "dd.MM.yyyy HH:mm:ss")]
        public DateTime timestamp { get; set; }
        public string uuid { get; set; }
        public SellResponseStatus status { get; set; }
        public AtolError error { get; set; }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum SellResponseStatus
    {
        NotFound = -1,
        fail = 0,
        wait = 1
    }
}
