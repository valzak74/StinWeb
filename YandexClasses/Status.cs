using JsonExtensions;
using Newtonsoft.Json;

namespace YandexClasses
{
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum StatusYandex
    {
        NotFound = -100,
        UNPAID = -1,
        RESERVED = 0,
        PROCESSING = 1,
        DELIVERY = 2,
        SHIPPED = 3,
        PICKUP = 4,
        DELIVERED = 5,
        CANCELLED = 9
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum SubStatusYandex
    {
        NotFound = -10,
        STARTED = 0,
        READY_TO_SHIP = 1,
        SHOP_FAILED = 2,
        ANTIFRAUD = 3,
        DELIVERY_SERVICE_UNDELIVERED = 4,
        PENDING_EXPIRED = 5,
        PROCESSING_EXPIRED = 6,
        REPLACING_ORDER = 7,
        RESERVATION_EXPIRED = 8,
        SHIPPED = 9,
        PICKUP_EXPIRED = 10,
        DELIVERY_SERVICE_RECEIVED = 11,
        DELIVERY_SERVICE_DELIVERED = 12,
        USER_CHANGED_MIND = 13,
        USER_UNREACHABLE = 14,
        PICKUP_SERVICE_RECEIVED = 15
    }
}
