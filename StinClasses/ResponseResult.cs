using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace StinClasses
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ResponseResult
    {
        OK = 1,
        ERROR = 2
    }
}
