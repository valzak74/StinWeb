using System.Collections.Generic;

namespace YandexClasses
{
    public class Delivery
    {
        public Region Region { get; set; }
        public Address Address { get; set; }
    }
    public class OrderDelivery
    {
        public DeliveryPartnerType DeliveryPartnerType { get; set; }
        public string DeliveryServiceId { get; set; }
        public string Id { get; set; }
        public string ShopDeliveryId { get; set; }
        public double Price { get; set; }
        public string ServiceName { get; set; }
        public DeliveryType Type { get; set; }
        public DeliveryLiftType LiftType { get; set; }
        public double LiftPrice { get; set; }
        public Vat Vat { get; set; }
        public List<Shipment> Shipments { get; set; }
        public Address Address { get; set; }
        public double Subsidy { get; set; }
        public Date Dates { get; set; }
        public Outlet Outlet { get; set; }
        public Region Region { get; set; }
    }
}
