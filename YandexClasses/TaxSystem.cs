using JsonExtensions;
using Newtonsoft.Json;

namespace YandexClasses
{
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum TaxSystem
    {
        ECHN = 1,
        ENVD = 2,
        OSN = 3,
        PSN = 4,
        USN = 5,
        USN_MINUS_COST = 6,
        NotFound
    }
}
