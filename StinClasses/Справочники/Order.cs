using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StinClasses.Models;
using StinClasses.Регистры;

namespace StinClasses.Справочники
{
    //статусы в справочнике
    //0 - созданный новый ордер
    //1 - yandex FBS boxes сформированы
    //2 - yandex FBS labels скачены
    //3 - отправлен статус PROCESSING - READY_TO_SHIP
    //4 - нами отправлен запрос на уменьшение количества товара в заказе
    //5 - нами отправлен запрос на отмену заказа
    //6 - нами отправлен статус PROCESSING - SHIPPED
    //7 - получено уведомление от клиента о отмене заказа 
    //8 - получен статус PROCESSING - STARTED
    //9 - нами отправлен статус DELIVERY или PICKUP (в зависимости от OrderItems.DeliveryType)
    //13 - спорный ордер

    //статусы в регистре
    //10 - отправлен в набор
    //11 - набор завершен
    public class Order
    {
        public string Id { get; set; }
        public int InternalStatus { get; set; }
        public int Status { get; set; }
        public int SubStatus { get; set; }
        public string Тип { get; set; }
        public string Модель { get; set; }
        public string Marketplace { get; set; }
        public string MarketplaceId { get; set; }
        public string OrderNo { get; set; }
        public StinPaymentType PaymentType { get; set; }
        public StinPaymentMethod PaymentMethod { get; set; }
        public StinDeliveryPartnerType DeliveryPartnerType { get; set; }
        public StinDeliveryType DeliveryType { get; set; }
        public string DeliveryServiceId { get; set; }
        public string DeliveryServiceName { get; set; }
        public double DeliveryPrice { get; set; }
        public string ShipmentId { get; set; }
        public DateTime ShipmentDate { get; set; }
        public string RegionId { get; set; }
        public string RegionName { get; set; }
        public string CampaignId { get; set; }
        public string ClientId { get; set; }
        public string AuthToken { get; set; }
        public string AuthorizationApi { get; set; }
        public EncodeVersion Encode { get; set; }
        public OrderBuyerRecipient Recipient { get; set; }
        public OrderRecipientAddress Address { get; set; }
        public string CustomerComment { get; set; }
        public List<OrderItem> Items { get; set; }
        public decimal СуммаВозмещения { get; set; }
        public Order()
        {
            Items = new List<OrderItem>();
        }
    }
    public class OrderItem
    {
        public string Id { get; set; }
        public string Sku { get; set; }
        public string НоменклатураId { get; set; }
        public decimal Количество { get; set; }
        public decimal Квант { get; set; }
        public decimal КолМест { get; set; }
        public decimal Цена { get; set; }
        public decimal ЦенаСоСкидкой { get; set; }
        public decimal Вознаграждение { get; set; }
        public bool Доставка { get; set; }
        public string ДопПараметры { get; set; }
        public string ИдентификаторПоставщика { get; set; }
        public string ИдентификаторСклада { get; set; }
        public string ИдентификаторСкладаПартнера { get; set; }
    }
    public class OrderBuyerRecipient
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string Recipient { get; set; }
        public string Phone { get; set; }
    }
    public class OrderRecipientAddress
    {
        public string Postcode { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string Subway { get; set; }
        public string Street { get; set; }
        public string House { get; set; }
        public string Block { get; set; }
        public string Entrance { get; set; }
        public string Entryphone { get; set; }
        public string Floor { get; set; }
        public string Apartment { get; set; }
    }
    public interface IOrder : IDisposable
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
        Task ОбновитьOrderStatus(string Id, int NewStatus, string errMessage = "");
        Task ОбновитьOrderNoAndStatus(string Id, string OrderNo, int NewStatus);
        //Task<string> ReduceOrderItems(Order order, List<OrderItem> списокВозврата, CancellationToken cancellationToken);
        //Task<string> ChangeStatus(Order order, int статус, StinOrderStatus status, StinOrderSubStatus subStatus, CancellationToken cancellationToken);
        Task ОбновитьПолучателяЗаказаАдрес(string orderId, string LastName, string FirstName, string MiddleName, string Recipient, string Phone,
            string PostCode, string Country, string City, string Subway, string Street, string House, string Block, string Entrance, string Entryphone, string Floor, string Apartment,
            string CustomerNotes);
        Task SetOrdersPrinted(List<string> ids);
        Task ОбновитьOrderShipmentDate(string Id, DateTime ShipmentDate);
        Task RefreshOrderDeliveryServiceId(string Id, long DeliveryServiceId, string DeliveryServiceName, CancellationToken cancellationToken);
    }
    public class OrderEntity : IOrder
    {
        private StinDbContext _context;
        private bool disposed = false;
        private IРегистрMarketplaceOrders _регистрMarketplaceOrders;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _регистрMarketplaceOrders.Dispose();
                    _context.Dispose();
                }
            }
            this.disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public OrderEntity(StinDbContext context)
        {
            _context = context;
            _регистрMarketplaceOrders = new Регистр_MarketplaceOrders(context);
        }
        private async Task<Order> UpdateOrderStatusFromRegistry(Order order)
        {
            if (order != null)
            {
                var marketplaceOrders_Остатки = await _регистрMarketplaceOrders.ПолучитьОстаткиAsync(DateTime.Now, null, false,
                    null, order.Id, null);
                if (marketplaceOrders_Остатки != null && marketplaceOrders_Остатки.Count > 0)
                {
                    var regStatus = marketplaceOrders_Остатки.Min(x => x.Status);
                    order.Status = (int)((regStatus / 10) % 10);
                    order.SubStatus = (int)(regStatus % 10);
                }
            }
            return order;
        }
        public async Task<Order> ПолучитьOrder(string orderId)
        {
            var order = await (from x in _context.Sc13994s
                               join m in _context.Sc14042s on x.Sp14038 equals m.Id
                               where x.Id == orderId
                               select new Order
                               {
                                   Id = x.Id,
                                   InternalStatus = (int)x.Sp13982,
                                   //Status = (StatusYandex)((x.Sp13982 / 10) % 10),
                                   //SubStatus = (SubStatusYandex)(x.Sp13982 % 10),
                                   Тип = m.Sp14155.ToUpper().Trim(),
                                   Модель = m.Sp14164.ToUpper().Trim(),
                                   Marketplace = x.Descr.Trim(),
                                   MarketplaceId = x.Code.Trim(),
                                   OrderNo = x.Sp13981.Trim(),
                                   PaymentType = (StinPaymentType)x.Sp13983,
                                   PaymentMethod = (StinPaymentMethod)x.Sp13984,
                                   DeliveryPartnerType = (StinDeliveryPartnerType)x.Sp13985,
                                   DeliveryServiceId = x.Sp13986.ToString(),
                                   DeliveryServiceName = x.Sp13987.Trim(),
                                   DeliveryType = (StinDeliveryType)x.Sp13988,
                                   ShipmentId = x.Sp13989.Trim(),
                                   ShipmentDate = x.Sp13990,
                                   Recipient = string.IsNullOrWhiteSpace(x.Sp14116 + x.Sp14117 + x.Sp14118 + x.Sp14119 + x.Sp14120) ? null : 
                                        new OrderBuyerRecipient 
                                        {
                                            LastName = x.Sp14116.Trim(),
                                            FirstName = x.Sp14117.Trim(),
                                            MiddleName = x.Sp14118.Trim(),
                                            Recipient = x.Sp14119.Trim(),
                                            Phone = x.Sp14120.Trim()
                                        },
                                   Address = string.IsNullOrWhiteSpace(x.Sp14123 + x.Sp14124 + x.Sp14125 + x.Sp14126 + x.Sp14127 + x.Sp14128 + x.Sp14129 + x.Sp14130 + x.Sp14131 + x.Sp14132 + x.Sp14133) ? null :
                                        new OrderRecipientAddress 
                                        { 
                                            Postcode = x.Sp14123.Trim(),
                                            Country = x.Sp14124.Trim(),
                                            City = x.Sp14125.Trim(),
                                            Subway = x.Sp14126.Trim(),
                                            Street = x.Sp14127.Trim(),
                                            House = x.Sp14128.Trim(),
                                            Block = x.Sp14129.Trim(),
                                            Entrance = x.Sp14130.Trim(),
                                            Entryphone = x.Sp14131.Trim(),
                                            Floor = x.Sp14132.Trim(),
                                            Apartment = x.Sp14133.Trim()
                                        },
                                   СуммаВозмещения = x.Sp14135,
                                   CustomerComment = x.Sp14122,
                                   RegionId = x.Sp13991.ToString(),
                                   RegionName = x.Sp13992.Trim(),
                                   CampaignId = m.Code.Trim(),
                                   ClientId = m.Sp14053.Trim(),
                                   AuthToken = m.Sp14054.Trim(),
                                   AuthorizationApi = m.Sp14077.Trim(),
                                   Encode = (EncodeVersion)m.Sp14153
                               }).FirstOrDefaultAsync();

            return await UpdateOrderStatusFromRegistry(order);
        }
        public async Task<Order> ПолучитьOrderByMarketplaceId(string marketplaceId, string marketplaceOrderId)
        {
            var order = await (from x in _context.Sc13994s
                               join m in _context.Sc14042s on x.Sp14038 equals m.Id
                               where x.Code.Trim() == marketplaceOrderId && m.Id == marketplaceId
                               select new Order
                               {
                                   Id = x.Id,
                                   InternalStatus = (int)x.Sp13982,
                                   Тип = m.Sp14155.ToUpper().Trim(),
                                   Модель = m.Sp14164.ToUpper().Trim(),
                                   Marketplace = x.Descr.Trim(),
                                   MarketplaceId = x.Code.Trim(),
                                   OrderNo = x.Sp13981.Trim(),
                                   PaymentType = (StinPaymentType)x.Sp13983,
                                   PaymentMethod = (StinPaymentMethod)x.Sp13984,
                                   DeliveryPartnerType = (StinDeliveryPartnerType)x.Sp13985,
                                   DeliveryServiceId = x.Sp13986.ToString(),
                                   DeliveryServiceName = x.Sp13987.Trim(),
                                   DeliveryType = (StinDeliveryType)x.Sp13988,
                                   ShipmentId = x.Sp13989.Trim(),
                                   ShipmentDate = x.Sp13990,
                                   Recipient = string.IsNullOrWhiteSpace(x.Sp14116 + x.Sp14117 + x.Sp14118 + x.Sp14119 + x.Sp14120) ? null :
                                        new OrderBuyerRecipient
                                        {
                                            LastName = x.Sp14116.Trim(),
                                            FirstName = x.Sp14117.Trim(),
                                            MiddleName = x.Sp14118.Trim(),
                                            Recipient = x.Sp14119.Trim(),
                                            Phone = x.Sp14120.Trim()
                                        },
                                   Address = string.IsNullOrWhiteSpace(x.Sp14123 + x.Sp14124 + x.Sp14125 + x.Sp14126 + x.Sp14127 + x.Sp14128 + x.Sp14129 + x.Sp14130 + x.Sp14131 + x.Sp14132 + x.Sp14133) ? null :
                                        new OrderRecipientAddress
                                        {
                                            Postcode = x.Sp14123.Trim(),
                                            Country = x.Sp14124.Trim(),
                                            City = x.Sp14125.Trim(),
                                            Subway = x.Sp14126.Trim(),
                                            Street = x.Sp14127.Trim(),
                                            House = x.Sp14128.Trim(),
                                            Block = x.Sp14129.Trim(),
                                            Entrance = x.Sp14130.Trim(),
                                            Entryphone = x.Sp14131.Trim(),
                                            Floor = x.Sp14132.Trim(),
                                            Apartment = x.Sp14133.Trim()
                                        },
                                   СуммаВозмещения = x.Sp14135,
                                   CustomerComment = x.Sp14122,
                                   RegionId = x.Sp13991.ToString(),
                                   RegionName = x.Sp13992.Trim(),
                                   CampaignId = m.Code.Trim(),
                                   ClientId = m.Sp14053.Trim(),
                                   AuthToken = m.Sp14054.Trim(),
                                   AuthorizationApi = m.Sp14077.Trim(),
                                   Encode = (EncodeVersion)m.Sp14153
                               }).FirstOrDefaultAsync();

            return await UpdateOrderStatusFromRegistry(order);
        }
        public async Task<Order> ПолучитьOrderWithItems(string orderId)
        {
            var order = await ПолучитьOrder(orderId);
            if (order != null)
                order.Items = await (from x in _context.Sc14033s
                       join n in _context.Sc84s on x.Sp14022 equals n.Id
                       join ed in _context.Sc75s on n.Sp94 equals ed.Id
                       where x.Parentext == orderId
                       select new OrderItem
                       {
                           Id = x.Code,
                           Sku = n.Code.Encode(order.Encode),
                           НоменклатураId = x.Sp14022,
                           КолМест = ed.Sp14063 == 0 ? 1 : ed.Sp14063,
                           Квант = n.Sp14188 == 0 ? 1 : n.Sp14188,
                           Количество = x.Sp14023,
                           Цена = x.Sp14024,
                           ЦенаСоСкидкой = x.Sp14025,
                           Вознаграждение = x.Sp14026,
                           Доставка = x.Sp14027 == 1,
                           ДопПараметры = x.Sp14028,
                           ИдентификаторПоставщика = x.Sp14029,
                           ИдентификаторСклада = x.Sp14030,
                           ИдентификаторСкладаПартнера = x.Sp14031
                       }
                    ).ToListAsync();
            return order;
        }
        public async Task<Order> ПолучитьOrderWithItems(string marketplaceId, string marketplaceOrderId)
        {
            var order = await ПолучитьOrderByMarketplaceId(marketplaceId, marketplaceOrderId);
            if (order != null)
                order.Items = await (from x in _context.Sc14033s
                                     join n in _context.Sc84s on x.Sp14022 equals n.Id
                                     join ed in _context.Sc75s on n.Sp94 equals ed.Id
                                     where x.Parentext == order.Id
                                     select new OrderItem
                                     {
                                         Id = x.Code,
                                         Sku = n.Code.Encode(order.Encode),
                                         НоменклатураId = x.Sp14022,
                                         КолМест = ed.Sp14063 == 0 ? 1 : ed.Sp14063,
                                         Квант = n.Sp14188 == 0 ? 1 : n.Sp14188,
                                         Количество = x.Sp14023,
                                         Цена = x.Sp14024,
                                         ЦенаСоСкидкой = x.Sp14025,
                                         Вознаграждение = x.Sp14026,
                                         Доставка = x.Sp14027 == 1,
                                         ДопПараметры = x.Sp14028,
                                         ИдентификаторПоставщика = x.Sp14029,
                                         ИдентификаторСклада = x.Sp14030,
                                         ИдентификаторСкладаПартнера = x.Sp14031
                                     }
                    ).ToListAsync();
            return order;
        }
        public async Task ОбновитьOrderNo(string Id, string OrderNo)
        {
            var entity = await _context.Sc13994s.FirstOrDefaultAsync(x => x.Id == Id);
            if (entity != null)
            {
                entity.Sp13981 = OrderNo;
                _context.Update(entity);
                await _context.SaveChangesAsync();
            }
        }
        public async Task ОбновитьOrderShipmentDate(string Id, DateTime ShipmentDate)
        {
            var entity = await _context.Sc13994s.FirstOrDefaultAsync(x => x.Id == Id);
            if (entity != null)
            {
                entity.Sp13990 = ShipmentDate;
                _context.Update(entity);
                await _context.SaveChangesAsync();
            }
        }
        public async Task RefreshOrderDeliveryServiceId(string Id, long DeliveryServiceId, string DeliveryServiceName, CancellationToken cancellationToken)
        {
            var entity = await _context.Sc13994s.FirstOrDefaultAsync(x => x.Id == Id);
            if (entity != null)
            {
                entity.Sp13986 = DeliveryServiceId;
                entity.Sp13987 = DeliveryServiceName;
                _context.Update(entity);
                _context.РегистрацияИзмененийРаспределеннойИБ(13994, entity.Id);
                await _context.SaveChangesAsync();
            }
        }
        public async Task ОбновитьOrderStatus(string Id, int NewStatus, string errMessage = "")
        {
            var entity = await _context.Sc13994s.FirstOrDefaultAsync(x => x.Id == Id);
            if (entity != null)
            {
                entity.Sp13982 = NewStatus;
                if (!string.IsNullOrEmpty(errMessage))
                    entity.Sp14055 = errMessage;
                _context.Update(entity);
                _context.РегистрацияИзмененийРаспределеннойИБ(13994, entity.Id);
                await _context.SaveChangesAsync();
            }
        }
        public async Task ОбновитьOrderNoAndStatus(string Id, string OrderNo, int NewStatus)
        {
            var entity = await _context.Sc13994s.FirstOrDefaultAsync(x => x.Id == Id);
            if (entity != null)
            {
                entity.Sp13981 = OrderNo;
                entity.Sp13982 = NewStatus;
                _context.Update(entity);
                _context.РегистрацияИзмененийРаспределеннойИБ(13994, entity.Id);
                await _context.SaveChangesAsync();
            }
        }
        public async Task ОбновитьПолучателяЗаказаАдрес(string orderId, string LastName, string FirstName, string MiddleName, string Recipient, string Phone,
            string PostCode, string Country, string City, string Subway, string Street, string House, string Block, string Entrance, string Entryphone, string Floor, string Apartment,
            string CustomerNotes)
        {
            var entity = await _context.Sc13994s.FirstOrDefaultAsync(x => x.Id == orderId);
            if (entity != null)
            {
                if (entity.Sp14116.Trim().ValueChanged(LastName))
                    entity.Sp14116 = LastName.StringLimit(20);
                if (entity.Sp14117.Trim().ValueChanged(FirstName))
                    entity.Sp14117 = FirstName.StringLimit(20);
                if (entity.Sp14118.Trim().ValueChanged(MiddleName))
                    entity.Sp14118 = MiddleName.StringLimit(20);
                if (entity.Sp14119.Trim().ValueChanged(Recipient))
                    entity.Sp14119 = Recipient.StringLimit(100);
                if (entity.Sp14120.Trim().ValueChanged(Phone))
                    entity.Sp14120 = Phone.StringLimit(50);
                if (entity.Sp14123.Trim().ValueChanged(PostCode))
                    entity.Sp14123 = PostCode.StringLimit(20);
                if (entity.Sp14124.Trim().ValueChanged(Country))
                    entity.Sp14124 = Country.StringLimit(20);
                if (entity.Sp14125.Trim().ValueChanged(City))
                    entity.Sp14125 = City.StringLimit(20);
                if (entity.Sp14126.Trim().ValueChanged(Subway))
                    entity.Sp14126 = Subway.StringLimit(20);
                if (entity.Sp14127.Trim().ValueChanged(Street))
                    entity.Sp14127 = Street.StringLimit(150);
                if (entity.Sp14128.Trim().ValueChanged(House))
                    entity.Sp14128 = House.StringLimit(10);
                if (entity.Sp14129.Trim().ValueChanged(Block))
                    entity.Sp14129 = Block.StringLimit(10);
                if (entity.Sp14130.Trim().ValueChanged(Entrance))
                    entity.Sp14130 = Entrance.StringLimit(10);
                if (entity.Sp14131.Trim().ValueChanged(Entryphone))
                    entity.Sp14131 = Entryphone.StringLimit(10);
                if (entity.Sp14132.Trim().ValueChanged(Floor))
                    entity.Sp14132 = Floor.StringLimit(10);
                if (entity.Sp14133.Trim().ValueChanged(Apartment))
                    entity.Sp14133 = Apartment.StringLimit(10);
                if (entity.Sp14122.ValueChanged(CustomerNotes))
                    entity.Sp14122 = CustomerNotes;


                _context.Update(entity);
                _context.РегистрацияИзмененийРаспределеннойИБ(13994, entity.Id);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<Order> НовыйOrder(string firmaId, EncodeVersion encoding, string authApi, string marketplaceName, string marketplaceId,
            StinPaymentType paymentType, StinPaymentMethod paymentMethod,
            StinDeliveryPartnerType deliveryPartnerType, StinDeliveryType deliveryType, string deliveryServiceId, string deliveryServiceName, double deliveryPrice, double deliverySubsidy,
            string shipmentId, DateTime shipmentDate, string regionId, string regionName, OrderRecipientAddress address, string notes, List<OrderItem> items)
        {
            try
            {
                var order = new Order
                {
                    Id = _context.GenerateId(13994),
                    Status = (int)StinOrderStatus.RESERVED,
                    SubStatus = (int)StinOrderSubStatus.STARTED,
                    Marketplace = marketplaceName,
                    MarketplaceId = marketplaceId,
                    OrderNo = "",
                    PaymentType = paymentType,
                    PaymentMethod = paymentMethod,
                    DeliveryPartnerType = deliveryPartnerType,
                    DeliveryServiceId = deliveryServiceId,
                    DeliveryServiceName = deliveryServiceName,
                    DeliveryType = deliveryType,
                    DeliveryPrice = deliveryPrice,
                    ShipmentId = shipmentId,
                    ShipmentDate = shipmentDate,
                    RegionId = regionId,
                    RegionName = regionName,
                    Address = address,
                    СуммаВозмещения = (decimal)deliverySubsidy + items.Sum(x => x.Вознаграждение),
                    CustomerComment = string.IsNullOrWhiteSpace(notes) ? "" : notes
                };
                order.Items = items;
                var MarketplaceEntity = await _context.Sc14042s.Where(x => !x.Ismark && x.Parentext == firmaId && x.Sp14077.Trim() == authApi).FirstOrDefaultAsync();
                if (MarketplaceEntity == null)
                {
                    MarketplaceEntity = new Sc14042
                    {
                        Id = _context.GenerateId(14042),
                        Code = new string(marketplaceName.ToCharArray().Where(c => !Char.IsWhiteSpace(c)).ToArray()),
                        Descr = marketplaceName,
                        Ismark = false,
                        Verstamp = 0,
                        Parentext = firmaId,
                        Sp14053 = "",
                        Sp14195 = "",
                        Sp14054 = "",
                        Sp14076 = "",
                        Sp14077 = authApi,
                        Sp14153 = (int)encoding, 
                        Sp14154 = "",
                        Sp14155 = "Яндекс", //Тип
                        Sp14164 = "FBS", //Модель
                        Sp14165 = 1, //КоэфПроверкиЦен
                        Sp14156 = "", //ShortName
                        Sp14157 = 0, //Сортировка
                        Sp14175 = Common.ПустоеЗначение, //Контрагент
                        Sp14176 = Common.ПустоеЗначение, //Договор
                        Sp14241 = Common.ПустоеЗначение, //Склад
                        Sp14177 = 0, //StockRefresh
                        Sp14216 = 0 //StockOriginal
                    };
                    await _context.Sc14042s.AddAsync(MarketplaceEntity);
                    _context.РегистрацияИзмененийРаспределеннойИБ(14042, MarketplaceEntity.Id);
                }
                order.Тип = MarketplaceEntity.Sp14155.ToUpper().Trim();
                if ((order.Тип == "OZON") && (order.DeliveryServiceId != MarketplaceEntity.Code.Trim()))
                    order.Marketplace = MarketplaceEntity.Sp14155.Trim() + " realFBS";
                Sc13994 entity = new Sc13994
                {
                    Id = order.Id,
                    Code = order.MarketplaceId,
                    Descr = order.Marketplace,
                    Ismark = false,
                    Verstamp = 0,
                    Sp14038 = MarketplaceEntity.Id,
                    Sp13981 = order.OrderNo.StringLimit(20),//ПредварительнаяЗаявкаId
                    Sp13982 = 0, //(decimal)order.Status * 10 + (decimal)order.SubStatus,//Статус
                    Sp13983 = (decimal)order.PaymentType,
                    Sp13984 = (decimal)order.PaymentMethod,
                    Sp13985 = (decimal)order.DeliveryPartnerType,
                    Sp13986 = Convert.ToDecimal(order.DeliveryServiceId),
                    Sp13987 = order.DeliveryServiceName.StringLimit(20),
                    Sp13988 = (decimal)order.DeliveryType,
                    Sp13989 = order.ShipmentId.StringLimit(20),
                    Sp13990 = order.ShipmentDate,
                    Sp13991 = Convert.ToDecimal(order.RegionId),
                    Sp13992 = order.RegionName.StringLimit(30),
                    Sp14055 = "", //ИнформацияAPI
                    Sp14116 = "", //ПокупательФамилия
                    Sp14117 = "", //ПокупательИмя
                    Sp14118 = "", //ПокупательОтчеств
                    Sp14119 = "", //Получатель
                    Sp14120 = "", //Телефон
                    Sp14122 = order.CustomerComment, //ПокупательКоммент
                    Sp14123 = order.Address != null ? order.Address.Postcode.StringLimit(20) : "", //Индекс
                    Sp14124 = order.Address != null ? order.Address.Country.StringLimit(20) : "", //Страна
                    Sp14125 = order.Address != null ? order.Address.City.StringLimit(20) : "", //Город
                    Sp14126 = order.Address != null ? order.Address.Subway.StringLimit(20) : "", //Метро
                    Sp14127 = order.Address != null ? order.Address.Street.StringLimit(150) : "", //Улица
                    Sp14128 = order.Address != null ? order.Address.House.StringLimit(10) : "", //Дом
                    Sp14129 = order.Address != null ? order.Address.Block.StringLimit(10) : "", //Корпус
                    Sp14130 = order.Address != null ? order.Address.Entrance.StringLimit(10) : "", //Подъезд
                    Sp14131 = order.Address != null ? order.Address.Entryphone.StringLimit(10) : "", //КодДомофона
                    Sp14132 = order.Address != null ? order.Address.Floor.StringLimit(10) : "", //Этаж
                    Sp14133 = order.Address != null ? order.Address.Apartment.StringLimit(10) : "", //Квартира
                    Sp14135 = order.СуммаВозмещения,
                    Sp14192 = 0 //распечатан
                };
                await _context.Sc13994s.AddAsync(entity);
                _context.РегистрацияИзмененийРаспределеннойИБ(13994, entity.Id);
                foreach (var item in order.Items)
                {
                    var itemId = _context.GenerateId(14033);
                    await _context.Sc14033s.AddAsync(new Sc14033
                    {
                        Id = itemId,
                        Code = item.Id,
                        Parentext = entity.Id,
                        Ismark = false,
                        Verstamp = 0,
                        Sp14022 = item.НоменклатураId,
                        Sp14023 = item.Количество,
                        Sp14024 = item.Цена,
                        Sp14025 = item.ЦенаСоСкидкой,
                        Sp14026 = item.Вознаграждение,
                        Sp14027 = item.Доставка ? 1 : 0,
                        Sp14028 = item.ДопПараметры.StringLimit(150),
                        Sp14029 = item.ИдентификаторПоставщика.StringLimit(20),
                        Sp14030 = item.ИдентификаторСклада.StringLimit(20),
                        Sp14031 = item.ИдентификаторСкладаПартнера.StringLimit(100)
                    });
                    _context.РегистрацияИзмененийРаспределеннойИБ(14033, itemId);
                }
                await _context.SaveChangesAsync();
                return order;
            }
            catch
            {
                return null;
            }
        }
        public async Task SetOrdersPrinted(List<string> ids)
        {
            var entities = await _context.Sc13994s.Where(x => ids.Contains(x.Id)).ToListAsync();
            foreach (var entity in entities)
            {
                entity.Sp14192 = 1;
                _context.Update(entity);
                _context.РегистрацияИзмененийРаспределеннойИБ(13994, entity.Id);
            }
            await _context.SaveChangesAsync();
        }
    }
}