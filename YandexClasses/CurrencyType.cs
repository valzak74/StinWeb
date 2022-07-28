using JsonExtensions;
using Newtonsoft.Json;

namespace YandexClasses
{
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum CurrencyType
    {
        RUR = 0,
        NotFound
    }
}
