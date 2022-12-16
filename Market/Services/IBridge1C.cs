using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YandexClasses;
using StinClasses.Справочники;
using StinClasses;
using System.Threading;

namespace Market.Services
{
    public interface IBridge1C: IDisposable
    {
        Task<Фирма> ПолучитьФирму(string фирмаId);
        Task<List<Номенклатура>> ПолучитьСвободныеОстатки(List<string> requestedCodes, List<string> списокСкладов);
        Task<Tuple<string,DateTime>> NewOrder(bool isFBS, string authorizationApi, OrderRequestEntry order);
        Task<(string orderNo, DateTime ShipmentDate)> NewOrder(
                    string authorizationApi,
                    string marketplaceName,
                    string orderId,
                    List<OrderItem> items,
                    string regionName,
                    string outletId,
                    string orderDeliveryShipmentId,
                    DateTime orderShipmentDate,
                    double deliveryPrice,
                    double deliverySubsidy,
                    OrderRecipientAddress address,
                    StinPaymentType orderPaymentType,
                    StinPaymentMethod orderPaymentMethod,
                    StinDeliveryPartnerType orderDeliveryPartnerType,
                    StinDeliveryType orderDeliveryType,
                    string orderDeliveryServiceId,
                    string orderServiceName,
                    string orderDeliveryRegionId,
                    string orderDeliveryRegionName,
                    string orderNotes,
                    OrderBuyerRecipient recipientInfo,
                    CancellationToken cancellationToken);
        Task ChangeStatus(Order order, string authorizationApi, long Id, StatusYandex newStatus, SubStatusYandex newSubStatus, string userId = null, ReceiverPaymentType receiverPaymentType = ReceiverPaymentType.NotFound, string receiverEmail = null, string receiverPhone = null);
        Task<bool> ReduceCancelItems(string orderNo, string authorizationApi, List<OrderItem> cancelItems, CancellationToken cancellationToken);
        Task<string> ReduceCancelItems(string docId, CancellationToken stoppingToken);
        Task<string> SetStatusShipped(string orderId, string userId, ReceiverPaymentType paymentType, string email, string phone, CancellationToken cancellationToken);
        Task<string> SetStatusCancelledUserChangeMind(string orderId, CancellationToken cancellationToken);
        Task<List<Pickup>> ПолучитьТочкиСамовывоза(string фирмаId, string authorizationApi, string regionName = "");
        Task<int> РассчитатьКолвоДнейВыполнения(string складId, int addDays);
        Task SetCancelNotify(string authorizationApi, long Id);
        Task SetRecipientAndAddress(string authorizationApi, long Id, string LastName, string FirstName, string MiddleName, string Recipient, string Phone,
            string PostCode, string Country, string City, string Subway, string Street, string House, string Block, string Entrance, string Entryphone, string Floor, string Apartment,
            string CustomerNotes);
        Task<List<string>> ПолучитьLockedНоменклатураIds(string authorizationApi, List<string> списокКодовНоменклатуры);
        Task<Marketplace> ПолучитьМаркет(string authorizationApi, string firmaId);
        Task<List<string>> ПолучитьСкладIdОстатковMarketplace();
        Task<Dictionary<string, decimal>> ПолучитьКвант(List<string> списокКодовНоменклатуры, CancellationToken cancellationToken);
        Task<Dictionary<string, decimal>> ПолучитьDeltaStock(string marketId, List<string> списокКодовНоменклатуры, CancellationToken cancellationToken);
    }
}
