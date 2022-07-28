using Newtonsoft.Json;
using JsonExtensions;

namespace YandexClasses
{
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum PaymentMethod
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
        SBP = 9,
        NotFound
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum PaymentType
    {
        PREPAID = 0,
        POSTPAID = 1,
        NotFound
    }
}
