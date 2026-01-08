using JsonExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YandexClasses
{
    public class PriceUpdateRequest
    {
        public List<PriceOffer> Offers { get; set; }
        public PriceUpdateRequest()
        {
            Offers = new List<PriceOffer>();
        }
    }
    public class PriceOffer
    {
        public PriceFeed Feed { get; set; }
        public string Id { get; set; }
        public bool Delete { get; set; }
        public PriceElement Price { get; set; }
        public bool ShouldSerializeFeed()
        {
            return Feed != null;
        }
        public bool ShouldSerializeDelete()
        {
            return Delete;
        }
        public bool ShouldSerializePrice()
        {
            return (Price != null && !Delete);
        }
    }

    public class PriceElement
    {
        public CurrencyType CurrencyId { get; set; }
        public decimal DiscountBase { get; set; }
        public decimal Value { get; set; }
        public PriceVatType Vat { get; set; }
        public bool ShouldSerializeDiscountBase()
        {
            return DiscountBase > 0;
        }
        public bool ShouldSerializeVat()
        {
            return Vat != PriceVatType.vat_not_valid;
        }
    }

    public class PriceFeed
    {
        public long Id { get; set; }
    }
    [JsonConverter(typeof(EnumConverter))]
    public enum PriceVatType : int
    {
        vat_not_valid = -1,
        vat_10 = 2,
        vat_0 = 5,
        vat_not_used = 6,
        vat_20 = 7,
        vat_22 = 8,
    }
}
