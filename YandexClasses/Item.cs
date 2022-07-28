using System.Collections.Generic;
using Newtonsoft.Json;

namespace YandexClasses
{
    public class RequestedItem
    {
        public long Id { get; set; }
        public long FeedId { get; set; }
        public string OfferId { get; set; }
        public string OfferName { get; set; }
        public string FeedCategoryId { get; set; }
        public long FulfilmentShopId { get; set; }
        public int Count { get; set; }
        public long WarehouseId { get; set; }
        public string PartnerWarehouseId { get; set; }
        public double Price { get; set; }
        [JsonProperty("buyer-price")]
        public double BuyerPrice { get; set; }
        public double Subsidy { get; set; }
        public bool Delivery { get; set; }
        public string Params { get; set; }
        public Vat Vat { get; set; }
        public string ShopSku { get; set; }
        public string Sku { get; set; }
        public List<Instance> Instances { get; set; }
        public List<Promo> Promos { get; set; }

    }
    public class ResponseItemFBS
    {
        public long FeedId { get; set; }
        public string OfferId { get; set; }
        public bool Delivery { get; set; }
        public int Count { get; set; }
    }
    public class ResponseItemDBS : ResponseItemFBS
    {
        public string SellerInn { get; set; }
    }
}
