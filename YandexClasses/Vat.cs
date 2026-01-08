using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace YandexClasses
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Vat
    {
        NO_VAT = 1,
        VAT_0 = 2,
        VAT_10 = 3,
        VAT_10_110 = 4,
        VAT_18 = 5,
        VAT_18_118 = 6,
        VAT_20 = 7,
        VAT_20_120 = 8,
        VAT_22 = 9,
        VAT_22_122 = 10,
    }
}
