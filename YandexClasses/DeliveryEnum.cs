using Newtonsoft.Json;
using JsonExtensions;

namespace YandexClasses
{
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum DeliveryType
    {
        DELIVERY = 0,
        PICKUP = 1,
        POST = 2,
        DIGITAL = 3,
        NotFound
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum DeliveryLiftType
    {
        NOT_NEEDED = 0,
        MANUAL = 1,
        ELEVATOR = 2,
        CARGO_ELEVATOR = 3,
        FREE = 4,
        NotFound
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum DeliveryPartnerType
    {
        YANDEX_MARKET = 0,
        SHOP = 1,
        NotFound
    }
}
