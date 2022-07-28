using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace YandexClasses
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FaultReason
    {
        OUT_OF_DATE = 0
    }
}
