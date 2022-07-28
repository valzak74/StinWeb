using JsonExtensions;
using Newtonsoft.Json;

namespace YandexClasses
{
    public class Promo
    {
        public string MarketPromoId { get; set; }
        public float Subsidy { get; set; }
        public PromoType Type { get; set; }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum PromoType
    {
        MARKET_COUPON = 1,
        MARKET_DEAL = 2,
        MARKET_COIN = 3,
        MARKET_PROMOCODE = 4,
        NotFound
    }
}
