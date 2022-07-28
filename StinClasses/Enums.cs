using JsonExtensions;
using Newtonsoft.Json;

namespace StinClasses
{
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum StinDeliveryType
    {
        DELIVERY = 0,
        PICKUP = 1,
        POST = 2,
        DIGITAL = 3,
        NotFound
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum StinDeliveryPartnerType
    {
        YANDEX_MARKET = 0,
        SHOP = 1,
        OZON_LOGISTIC = 2,
        ALIEXPRESS_LOGISTIC = 3,
        NotFound
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum StinPaymentMethod
    {
        YANDEX = 0,
        APPLE_PAY = 1,
        GOOGLE_PAY = 2,
        CREDIT = 3,
        TINKOFF_CREDIT = 4,
        EXTERNAL_CERTIFICATE = 5,
        CARD_ON_DELIVERY = 6,
        CASH_ON_DELIVERY = 7,
        TINKOFF_INSTALLMENTS = 8,
        NotFound
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum StinPaymentType
    {
        PREPAID = 0,
        POSTPAID = 1,
        NotFound
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum StinOrderStatus
    {
        NotFound = -100,
        UNPAID = -1,
        RESERVED = 0,
        PROCESSING = 1,
        DELIVERY = 2,
        SHIPPED = 3,
        PICKUP = 4,
        DELIVERED = 5,
        CANCELLED = 9,
        ARBITRATION = 13
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum StinOrderSubStatus
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
