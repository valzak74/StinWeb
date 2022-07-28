using System.Collections.Generic;

namespace YandexClasses
{
    public class DeliveryOption
    {
        public string Id { get; set; }
        public bool PaymentAllow { get; set; }
        public double Price { get; set; }
        public string ServiceName { get; set; }
        public DeliveryType Type { get; set; }
        public Date Dates { get; set; }
        public List<Outlet> Outlets { get; set; }
        public List<PaymentMethod> PaymentMethods { get; set; }
        public DeliveryOption()
        {
            Outlets = new List<Outlet>();
            PaymentMethods = new List<PaymentMethod>();
        }
        public bool ShouldSerializeId()
        {
            return !string.IsNullOrEmpty(Id);
        }
        public bool ShouldSerializeOutlets()
        {
            return (Outlets != null) && (Outlets.Count > 0);
        }
        public bool ShouldSerializePaymentMethods()
        {
            return (PaymentMethods != null) && (PaymentMethods.Count > 0);
        }
    }
}
