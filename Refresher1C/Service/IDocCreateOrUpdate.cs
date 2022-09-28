using StinClasses;
using StinClasses.Справочники;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C.Service
{
    interface IDocCreateOrUpdate: IDisposable
    {
        Task CompleteSuccessPaymentAsync(string idDoc, string status, CancellationToken stoppingToken);
        Task ОбновитьНомерМаршрута(Order order);
        Task CreateNabor(string заявкаId, CancellationToken stoppingToken);
        Task NewOrder(string тип, string defFirmaId, string customerId, string dogovorId, string authApi, bool hexEncoding,
            string складОтгрузкиId, string notes,
            string deliveryServiceId, string deliveryServiceName,
            StinDeliveryPartnerType partnerType, StinDeliveryType deliveryType, double deliveryPrice, double deliverySubsidy,
            StinPaymentType paymentType, StinPaymentMethod paymentMethod,
            string regionId, string regionName, OrderRecipientAddress address,
            string shipmentId, string postingNumber, DateTime shipmentDate, List<OrderItem> items, CancellationToken cancellationToken);
        Task OrderCancelled(Order order);
        Task OrderDeliveried(Order order);
    }
}
