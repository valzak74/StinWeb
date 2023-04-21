using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StinClasses.Справочники.Functions
{
    public interface IOrderFunctions
    {
        Task<Order> ПолучитьOrder(string orderId);
        Task<Order> ПолучитьOrderByMarketplaceId(string marketplaceId, string marketplaceOrderId);
        Task<Order> ПолучитьOrderWithItems(string orderId);
        Task<Order> ПолучитьOrderWithItems(string marketplaceId, string marketplaceOrderId);
        Task<Order> НовыйOrder(string firmaId, EncodeVersion encoding, string authApi, string marketplaceName, string marketplaceId,
            StinPaymentType paymentType, StinPaymentMethod paymentMethod,
            StinDeliveryPartnerType deliveryPartnerType, StinDeliveryType deliveryType, string deliveryServiceId, string deliveryServiceName, double deliveryPrice, double deliverySubsidy,
            string shipmentId, DateTime shipmentDate, string regionId, string regionName, OrderRecipientAddress address, string notes, List<OrderItem> items);
        Task ОбновитьOrderNo(string Id, string OrderNo);
        Task UpdateOrderStatus(string id, int newStatus, string errMessage, CancellationToken cancellationToken);
        Task ОбновитьOrderNoAndStatus(string Id, string OrderNo, int NewStatus);
        Task ОбновитьПолучателяЗаказаАдрес(string orderId, string LastName, string FirstName, string MiddleName, string Recipient, string Phone,
            string PostCode, string Country, string City, string Subway, string Street, string House, string Block, string Entrance, string Entryphone, string Floor, string Apartment,
            string CustomerNotes);
        Task SetOrdersPrinted(List<string> ids);
        Task<string> SetOrderScanned(DateTime shipDate, string campaignInfo, string barcode, int scanMark, CancellationToken cancellationToken);
        Task ClearOrderScanned(DateTime shipDate, List<string> campaignIds, string warehouseId, CancellationToken cancellationToken);
        Task ОбновитьOrderShipmentDate(string Id, DateTime ShipmentDate);
        Task RefreshOrderDeliveryServiceId(string Id, long DeliveryServiceId, string DeliveryServiceName, CancellationToken cancellationToken);
        DateTime GetShipmentDateByServiceName(string marketplaceId, string serviceName);
    }
}
