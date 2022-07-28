using System.Collections.Generic;

namespace YandexClasses
{
    public class CartRequestEntry
    {
        public CurrencyType Currency { get; set; }
        public CurrencyType DeliveryCurrency { get; set; }
        public Delivery Delivery { get; set; }
        public List<RequestedItem> Items { get; set; }
        public CartRequestEntry()
        {
            Items = new List<RequestedItem>();
        }
    }
}
