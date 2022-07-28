using System.Collections.Generic;

namespace YandexClasses
{
    public class CartResponseDBSEntry
    {
        public CurrencyType DeliveryCurrency { get; set; }
        public List<DeliveryOption> DeliveryOptions { get; set; }
        public List<ResponseItemDBS> Items { get; set; }
        public List<PaymentMethod> PaymentMethods { get; set; }
        public CartResponseDBSEntry()
        {
            Items = new List<ResponseItemDBS>();
            DeliveryOptions = new List<DeliveryOption>();
            PaymentMethods = new List<PaymentMethod>();
        }
    }
    public class CartResponseFBSEntry
    {
        public List<ResponseItemFBS> Items { get; set; }
        public CartResponseFBSEntry()
        {
            Items = new List<ResponseItemFBS>();
        }
    }
}
