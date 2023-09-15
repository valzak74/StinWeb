using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StinClasses.Models;
using Microsoft.Extensions.Logging;
using StinClasses;
using StinClasses.Документы;
using YandexClasses;
using Microsoft.Extensions.Configuration;
using StinClasses.Справочники;
using AtolOnlineClasses;
using System.Threading;
using System.Net.Http;
using HttpExtensions;

namespace Market.Services
{
    public class Bridge1C: IBridge1C
    {
        private IConfiguration _configuration;
        private IHttpService _httpService;
        private readonly StinDbContext _context;
        private readonly ILogger<Bridge1C> _logger;
        private protected bool disposed = false;

        private IФирма _фирма;
        private IOrder _order;
        private IMarketplace _marketplace;
        private IНоменклатура _номенклатура;
        private IPickup _pickup;
        private IСклад _склад;

        private IПредварительнаяЗаявка _предварительнаяЗаявка;
        private IЗаявкаПокупателя _заявкаПокупателя;
        private IСпрос _спрос;
        private IНабор _набор;
        private IОтменаЗаявок _отменаЗаявок;
        private IОтменаНабора _отменаНабора;
        private IВозвратИзНабора _возвратИзНабора;
        private IКомплекснаяПродажа _комплекснаяПродажа;
        private IПродажа _продажа;
        private IРеализация _реализация;
        private IСчетФактура _счетФактура;
        private IПеремещениеТМЦ _перемещениеТМЦ;
        private IВозвратИзДоставки _возвратИзДоставки;
        private IПКО _пко;
        private IЧекККМ _чек;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _фирма.Dispose();
                    _order.Dispose();
                    _marketplace.Dispose();
                    _номенклатура.Dispose();
                    //_pickup.Dispose();
                    _склад.Dispose();
                    _предварительнаяЗаявка.Dispose();
                    _заявкаПокупателя.Dispose();
                    _спрос.Dispose();
                    _набор.Dispose();
                    _отменаЗаявок.Dispose();
                    _отменаНабора.Dispose();
                    _возвратИзНабора.Dispose();
                    _комплекснаяПродажа.Dispose();
                    _продажа.Dispose();
                    _реализация.Dispose();
                    _счетФактура.Dispose();
                    _перемещениеТМЦ.Dispose();
                    _возвратИзДоставки.Dispose();
                    _пко.Dispose();
                    _чек.Dispose();
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
        public Bridge1C(ILogger<Bridge1C> logger, StinDbContext context, IConfiguration configuration, IHttpService httpService)
        {
            _logger = logger;
            _context = context;
            _configuration = configuration;
            _httpService = httpService;
            _фирма = new ФирмаEntity(context);
            _order = new OrderEntity(context);
            _marketplace = new MarketplaceEntity(context);
            _pickup = new PickupEntity(context);
            _номенклатура = new НоменклатураEntity(context);
            _склад = new СкладEntity(context);
            _предварительнаяЗаявка = new ПредварительнаяЗаявка(context);
            _заявкаПокупателя = new ЗаявкаПокупателя(context);
            _спрос = new Спрос(context);
            _набор = new Набор(context);
            _отменаЗаявок = new ОтменаЗаявок(context);
            _отменаНабора = new ОтменаНабора(context);
            _возвратИзНабора = new ВозвратИзНабора(context);
            _комплекснаяПродажа = new КомплекснаяПродажа(context);
            _продажа = new Продажа(context);
            _реализация = new Реализация(context);
            _счетФактура = new СчетФактура(context);
            _перемещениеТМЦ = new ПеремещениеТМЦ(context);
            _возвратИзДоставки = new ВозвратИзДоставки(context);
            _пко = new ПКО(context);
            _чек = new ЧекККМ(context);
        }
        public async Task<List<Номенклатура>> ПолучитьСвободныеОстатки(List<string> requestedCodes, List<string> списокСкладов)
        {
            var defFirma = _configuration["Settings:Firma"];
            var разрешенныеФирмы = await _фирма.ПолучитьСписокРазрешенныхФирмAsync(_configuration["Settings:" + defFirma + ":FirmaId"]);

            return await _номенклатура.ПолучитьСвободныеОстатки(разрешенныеФирмы, списокСкладов, requestedCodes, true);
        }
        public async Task<Фирма> ПолучитьФирму(string фирмаId)
        {
            //using IФирма _фирма = new ФирмаEntity(_context);
            return await _фирма.GetEntityByIdAsync(фирмаId);
        }
        public async Task<List<Pickup>> ПолучитьТочкиСамовывоза(string фирмаId, string authorizationApi, string regionName = "")
        {
            //using IPickup _pickup = new PickupEntity(_context);
            return await _pickup.GetPickups(фирмаId, authorizationApi, regionName);
        }
        public async Task<(string orderNo, DateTime ShipmentDate)> NewOrder(
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
            CancellationToken cancellationToken)
        {
            var defFirma = _configuration["Settings:Firma"];
            string _defFirmaId = _configuration["Settings:" + defFirma + ":FirmaId"];
            var market = await _marketplace.ПолучитьMarketplaceByFirma(authorizationApi, _defFirmaId);
            if (market == null)
            {
                _logger.LogError("Не обнаружен маркетплейс для фирмы " + _defFirmaId + " и authApi " + authorizationApi);
                return (orderNo: "", ShipmentDate: DateTime.Now);
            }

            Order getOrder = await _order.ПолучитьOrderByMarketplaceId(market.Id, orderId);
            string orderNo = getOrder != null ? getOrder.OrderNo.Trim() : "";
            if (string.IsNullOrEmpty(orderNo))
            {
                foreach (var item in items)
                {
                    item.Sku = item.Sku.Decode(market.Encoding); 
                }
                var точкиСамовывоза = await ПолучитьТочкиСамовывоза(_defFirmaId, authorizationApi, regionName);
                List<string> списокСкладовНаличияТовара = null;
                if (!string.IsNullOrEmpty(market.СкладId))
                    списокСкладовНаличияТовара = new List<string> { market.СкладId };
                else
                    списокСкладовНаличияТовара = await _склад.ПолучитьСкладIdОстатковMarketplace();

                var НоменклатураList = await ПолучитьСвободныеОстатки(items.Select(x => x.Sku).ToList(), списокСкладовНаличияТовара);
                var номенклатураCodes = items.Select(x => x.Sku).ToList();
                var nomQuantums = await _номенклатура.ПолучитьКвант(номенклатураCodes, cancellationToken);
                foreach (var item in items)
                {
                    item.НоменклатураId = НоменклатураList.Where(y => y.Code == item.Sku).Select(z => z.Id).FirstOrDefault();
                    var quantum = (int)nomQuantums.Where(q => q.Key == item.НоменклатураId).Select(q => q.Value).FirstOrDefault();
                    if (quantum == 0)
                        quantum = 1;
                    if (marketplaceName.Contains("YANDEX", StringComparison.InvariantCultureIgnoreCase))
                        quantum = 1;
                    item.Количество = item.Количество * quantum;
                    item.Цена = item.Цена / quantum;
                    item.ЦенаСоСкидкой = item.ЦенаСоСкидкой / quantum;
                    item.Вознаграждение = item.Вознаграждение / quantum;
                }

                bool нетВНаличие = НоменклатураList
                            .Any(x => x.Остатки
                                    .Sum(z => z.СвободныйОстаток) / x.Единица.Коэффициент <
                                        items
                                            .Where(b => b.Sku == x.Code)
                                            .Select(c => c.Количество)
                                            .FirstOrDefault());
                if (!нетВНаличие)
                {
                    string складОтгрузкиId = Common.SkladEkran;
                    if (!string.IsNullOrEmpty(outletId))
                        складОтгрузкиId = outletId;
                    using var tran = await _context.Database.BeginTransactionAsync(cancellationToken);
                    try
                    {
                        DateTime dateTimeTA = _context.GetDateTimeTA();
                        bool needToCalcDateTime = dateTimeTA.Month != DateTime.Now.Month;
                        var реквизитыПроведенныхДокументов = new List<ОбщиеРеквизиты>();

                        var new_order = await _order.НовыйOrder(_defFirmaId, market.Encoding, authorizationApi, marketplaceName, orderId,
                            orderPaymentType, orderPaymentMethod,
                            orderDeliveryPartnerType, orderDeliveryType, orderDeliveryServiceId, orderServiceName, deliveryPrice, deliverySubsidy,
                            orderDeliveryShipmentId, orderShipmentDate,
                            orderDeliveryRegionId, orderDeliveryRegionName, address, orderNotes, items);
                        if (new_order == null)
                        {
                            if (_context.Database.CurrentTransaction != null)
                                tran.Rollback();
                            _logger.LogError("Ошибка создания заказа");
                            return (orderNo: "", ShipmentDate: DateTime.Now);
                        }

                        var формаПредварительнаяЗаявка = await _предварительнаяЗаявка.НовыйДокумент(
                                 needToCalcDateTime ? dateTimeTA.AddMilliseconds(1) : DateTime.Now,
                                 _defFirmaId,
                                 string.IsNullOrEmpty(складОтгрузкиId) ? Common.SkladEkran : складОтгрузкиId,
                                 market.КонтрагентId,
                                 market.ДоговорId,
                                 Common.ТипЦенРозничный,
                                 new_order, НоменклатураList, address);
                        var result = await _предварительнаяЗаявка.ЗаписатьAsync(формаПредварительнаяЗаявка);
                        if (result != null)
                        {
                            if (_context.Database.CurrentTransaction != null)
                                tran.Rollback();
                            _logger.LogError(result.Description);
                            orderNo = null;
                        }
                        else
                        {
                            orderNo = формаПредварительнаяЗаявка.Общие.НомерДок + "-" + формаПредварительнаяЗаявка.Общие.ДатаДок.ToString("yyyy");
                            формаПредварительнаяЗаявка.Order.OrderNo = orderNo;
                            if (marketplaceName.Contains("YANDEX", StringComparison.InvariantCultureIgnoreCase))
                                await _order.ОбновитьOrderNo(формаПредварительнаяЗаявка.Order.Id, orderNo);
                            else
                                await _order.ОбновитьOrderNoAndStatus(формаПредварительнаяЗаявка.Order.Id, orderNo, 8);
                            if ((recipientInfo != null) && (address != null))
                                await _order.ОбновитьПолучателяЗаказаАдрес(формаПредварительнаяЗаявка.Order.Id, 
                                    recipientInfo.LastName, 
                                    recipientInfo.FirstName, 
                                    recipientInfo.MiddleName, 
                                    recipientInfo.Recipient, 
                                    recipientInfo.Phone,
                                    address.Postcode,
                                    address.Country,
                                    address.City,
                                    address.Subway,
                                    address.Street,
                                    address.House,
                                    address.Block,
                                    address.Entrance,
                                    address.Entryphone,
                                    address.Floor,
                                    address.Apartment,
                                    orderNotes);
                            result = await _предварительнаяЗаявка.ПровестиAsync(формаПредварительнаяЗаявка);
                            if (result != null)
                            {
                                if (_context.Database.CurrentTransaction != null)
                                    tran.Rollback();
                                _logger.LogError(result.Description);
                                orderNo = null;
                            }
                            else
                            {
                                реквизитыПроведенныхДокументов.Add(формаПредварительнаяЗаявка.Общие);
                                bool необходимоПеремещать = (формаПредварительнаяЗаявка.Order != null); //&&
                                    //(формаПредварительнаяЗаявка.Order.DeliveryPartnerType == StinDeliveryPartnerType.SHOP) &&
                                    //(формаПредварительнаяЗаявка.Order.DeliveryType == StinDeliveryType.PICKUP);
                                var ПереченьНаличия = await _предварительнаяЗаявка.РаспределитьТоварПоНаличиюAsync(формаПредварительнаяЗаявка, списокСкладовНаличияТовара);
                                List<string> notNativeKeys = new List<string> { "ДилерскаяЗаявка", "Спрос" };
                                var списокУслуг = формаПредварительнаяЗаявка.ТабличнаяЧасть.Where(x => x.Номенклатура.ЭтоУслуга).ToList();
                                if (ПереченьНаличия.Where(x => !notNativeKeys.Contains(x.Item2)).Count() > 0)
                                {
                                    foreach (var firmaId in ПереченьНаличия.Where(x => !notNativeKeys.Contains(x.Item2)).GroupBy(x => x.Item1).Select(gr => gr.Key))
                                    {
                                        foreach (var склId in ПереченьНаличия.Where(x => !notNativeKeys.Contains(x.Item2)).GroupBy(x => x.Item2).Select(gr => gr.Key))
                                        {
                                            var rowDataValue = ПереченьНаличия.Where(x => x.Item1 == firmaId && x.Item2 == склId).Select(x => x.Item3);
                                            if (rowDataValue.Count() > 0)
                                            {
                                                var rowData = rowDataValue.FirstOrDefault();
                                                if (необходимоПеремещать && (формаПредварительнаяЗаявка.Склад.Id != склId))
                                                {
                                                    var формаЗаявкаОдобренная = await _заявкаПокупателя.ВводНаОснованииAsync(формаПредварительнаяЗаявка, needToCalcDateTime ? dateTimeTA.AddMilliseconds(2) : DateTime.Now, списокУслуг.Count > 0, "Заявка (одобренная)", формаПредварительнаяЗаявка.Общие.Фирма.Id, формаПредварительнаяЗаявка.Склад.Id, rowData);
                                                    списокУслуг.Clear();
                                                    result = await _заявкаПокупателя.ЗаписатьПровестиAsync(формаЗаявкаОдобренная);
                                                    реквизитыПроведенныхДокументов.Add(формаЗаявкаОдобренная.Общие);
                                                    if (result != null)
                                                    {
                                                        if (_context.Database.CurrentTransaction != null)
                                                            tran.Rollback();
                                                        _logger.LogError(result.Description);
                                                        orderNo = null;
                                                        break;
                                                    }
                                                    //Перемещение со склада (включить маршрут)
                                                    var ПереченьФормаПеремещениеТМЦ = await _перемещениеТМЦ.ДляЗаказаЗаявки(формаЗаявкаОдобренная, needToCalcDateTime ? dateTimeTA.AddMilliseconds(3) : DateTime.Now, firmaId, склId);
                                                    foreach (var формаПеремещениеТмц in ПереченьФормаПеремещениеТМЦ)
                                                    {
                                                        result = await _перемещениеТМЦ.ЗаписатьПровестиAsync(формаПеремещениеТмц);
                                                        реквизитыПроведенныхДокументов.Add(формаПеремещениеТмц.Общие);
                                                        if (result != null)
                                                        {
                                                            if (_context.Database.CurrentTransaction != null)
                                                                tran.Rollback();
                                                            _logger.LogError(result.Description);
                                                            orderNo = null;
                                                            break;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    var формаСчет = await _заявкаПокупателя.ВводНаОснованииAsync(формаПредварительнаяЗаявка, needToCalcDateTime ? dateTimeTA.AddMilliseconds(2) : DateTime.Now, списокУслуг.Count > 0, "Счет на оплату", формаПредварительнаяЗаявка.Общие.Фирма.Id, склId, rowData);
                                                    списокУслуг.Clear();
                                                    result = await _заявкаПокупателя.ЗаписатьПровестиAsync(формаСчет);
                                                    реквизитыПроведенныхДокументов.Add(формаСчет.Общие);
                                                }
                                                if (result != null)
                                                {
                                                    if (_context.Database.CurrentTransaction != null)
                                                        tran.Rollback();
                                                    _logger.LogError(result.Description);
                                                    orderNo = null;
                                                    break;
                                                }
                                            }
                                        }
                                        if (result != null)
                                            break;
                                    }
                                }
                                if (ПереченьНаличия.Where(x => x.Item2 == "ДилерскаяЗаявка").Count() > 0)
                                {
                                    var rowData = ПереченьНаличия.Where(x => x.Item2 == "ДилерскаяЗаявка").Select(x => x.Item3).FirstOrDefault();
                                    if (rowData.Count > 0)
                                    {
                                        var формаДилерскаяЗаявка = await _заявкаПокупателя.ВводНаОснованииAsync(формаПредварительнаяЗаявка, needToCalcDateTime ? dateTimeTA.AddMilliseconds(2) : DateTime.Now, списокУслуг.Count > 0, "Заявка дилера", null, null, rowData);
                                        списокУслуг.Clear();
                                        result = await _заявкаПокупателя.ЗаписатьПровестиAsync(формаДилерскаяЗаявка);
                                        реквизитыПроведенныхДокументов.Add(формаДилерскаяЗаявка.Общие);
                                        if (result != null)
                                        {
                                            if (_context.Database.CurrentTransaction != null)
                                                tran.Rollback();
                                            _logger.LogError(result.Description);
                                            orderNo = null;
                                        }
                                    }
                                }
                                if (ПереченьНаличия.Where(x => x.Item2 == "Спрос").Count() > 0)
                                {
                                    var rowData = ПереченьНаличия.Where(x => x.Item2 == "Спрос").Select(x => x.Item3).FirstOrDefault();
                                    if (rowData.Count > 0)
                                    {
                                        var формаСпрос = await _спрос.ВводНаОснованииAsync(формаПредварительнаяЗаявка, needToCalcDateTime ? dateTimeTA.AddMilliseconds(2) : DateTime.Now, rowData);
                                        result = await _спрос.ЗаписатьПровестиAsync(формаСпрос);
                                        реквизитыПроведенныхДокументов.Add(формаСпрос.Общие);
                                        if (result != null)
                                        {
                                            if (_context.Database.CurrentTransaction != null)
                                                tran.Rollback();
                                            _logger.LogError(result.Description);
                                            orderNo = null;
                                        }
                                    }
                                }
                            }
                            if (реквизитыПроведенныхДокументов.Count > 0)
                                await _предварительнаяЗаявка.ОбновитьАктивность(реквизитыПроведенныхДокументов);
                        }
                        if (_context.Database.CurrentTransaction != null)
                            tran.Commit();
                    }
                    catch (DbUpdateException db_ex)
                    {
                        if (_context.Database.CurrentTransaction != null)
                            _context.Database.CurrentTransaction.Rollback();
                        _logger.LogError(db_ex.InnerException.ToString());
                    }
                    catch (Exception ex)
                    {
                        if (_context.Database.CurrentTransaction != null)
                            _context.Database.CurrentTransaction.Rollback();
                        _logger.LogError(ex.Message);
                    }
                }

            }
            else
                orderShipmentDate = getOrder.ShipmentDate;
            return (orderNo: orderNo, ShipmentDate: orderShipmentDate);
        }
        public async Task<Tuple<string, DateTime>> NewOrder(bool isFBS, string authorizationApi, OrderRequestEntry order)
        {
            var defFirma = _configuration["Settings:Firma"];
            string _defFirmaId = _configuration["Settings:" + defFirma + ":FirmaId"];
            var market = await _marketplace.ПолучитьMarketplaceByFirma(authorizationApi, _defFirmaId);
            if (market == null)
            {
                _logger.LogError("Не обнаружен маркетплейс для фирмы " + _defFirmaId + " и authApi " + authorizationApi);
                return Tuple.Create("", DateTime.Now);
            }

            Order getOrder = await _order.ПолучитьOrderByMarketplaceId(market.Id, order.Id.ToString());
            string orderNo = getOrder != null ? getOrder.OrderNo.Trim() : "";
            DateTime orderShipmentDate = DateTime.MinValue;
            if (string.IsNullOrEmpty(orderNo))
            {
                foreach (var item in order.Items)
                {
                    item.OfferId = item.OfferId.Decode(market.Encoding); //market.HexEncoding ? item.OfferId.DecodeHexString() : item.OfferId;
                }
                var requestedRegion = order.Delivery.Region.FindRegionByType(RegionType.CITY);
                if (requestedRegion == null)
                    requestedRegion = order.Delivery.Region.FindRegionByType(RegionType.SUBJECT_FEDERATION_DISTRICT);
                if (requestedRegion == null)
                    requestedRegion = order.Delivery.Region.FindRegionByType(RegionType.SUBJECT_FEDERATION);
                if (requestedRegion == null)
                    requestedRegion = order.Delivery.Region.FindRegionByType(RegionType.COUNTRY_DISTRICT);
                if (requestedRegion == null)
                    requestedRegion = order.Delivery.Region.FindRegionByType(RegionType.COUNTRY);
                var точкиСамовывоза = await ПолучитьТочкиСамовывоза(_defFirmaId, authorizationApi, requestedRegion.Name);
                List<string> списокСкладовНаличияТовара = null;
                if (!string.IsNullOrEmpty(market.СкладId))
                    списокСкладовНаличияТовара = new List<string> { market.СкладId };
                else
                    списокСкладовНаличияТовара = await _склад.ПолучитьСкладIdОстатковMarketplace();

                var НоменклатураList = await ПолучитьСвободныеОстатки(order.Items.Select(x => x.OfferId).Distinct().ToList(), списокСкладовНаличияТовара);
                bool нетВНаличие = НоменклатураList
                            .Any(x => x.Остатки
                                    .Sum(z => z.СвободныйОстаток) / x.Единица.Коэффициент <
                                        order.Items
                                            .Where(b => b.OfferId == x.Code)
                                            .Sum(c => c.Count));
                if (!нетВНаличие)
                {
                    var orderItems = new List<OrderItem>();
                    order.Items.ForEach(x =>
                    {
                        orderItems.Add(new OrderItem 
                        {
                            Id = x.Id.ToString(),
                            НоменклатураId = НоменклатураList.Where(y => y.Code == x.OfferId).Select(z => z.Id).FirstOrDefault(),
                            Количество = x.Count,
                            Цена = (decimal)x.Price,
                            ЦенаСоСкидкой = (decimal)x.BuyerPrice,
                            Вознаграждение = (decimal)x.Subsidy,
                            Доставка = x.Delivery,
                            ДопПараметры = x.Params,
                            ИдентификаторПоставщика = x.FulfilmentShopId.ToString(),
                            ИдентификаторСклада = x.WarehouseId.ToString(),
                            ИдентификаторСкладаПартнера = x.PartnerWarehouseId
                        });
                    });
                    string складОтгрузкиId = Common.SkladEkran;
                    if (!isFBS && (order.Delivery.Outlet != null))
                        складОтгрузкиId = order.Delivery.Outlet.Code.FormatTo1CId();
                    using var tran = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        DateTime dateTimeTA = _context.GetDateTimeTA();
                        bool needToCalcDateTime = dateTimeTA.Month != DateTime.Now.Month;
                        var реквизитыПроведенныхДокументов = new List<ОбщиеРеквизиты>();
                        string orderDeliveryShipmentId = "nothing";
                        if (order.Delivery.Shipments != null && order.Delivery.Shipments.Count > 0)
                        {
                            orderDeliveryShipmentId = order.Delivery.Shipments[0].Id.ToString();
                            orderShipmentDate = order.Delivery.Shipments[0].ShipmentDate;
                        }
                        if (orderShipmentDate == DateTime.MinValue)
                        {
                            if (order.Delivery.Dates != null && order.Delivery.Dates.FromDate > DateTime.MinValue)
                                orderShipmentDate = order.Delivery.Dates.FromDate;
                            else
                                orderShipmentDate = DateTime.Today.AddDays(await РассчитатьКолвоДнейВыполнения(складОтгрузкиId, 0));
                        }
                        double deliveryPrice = 0;
                        double deliverySubsidy = 0;
                        if ((order.Delivery.DeliveryPartnerType == DeliveryPartnerType.SHOP) &&
                            (order.Delivery.Type != DeliveryType.PICKUP))
                        {
                            deliveryPrice = order.Delivery.Price + order.Delivery.Subsidy;
                            deliverySubsidy = order.Delivery.Subsidy;
                        }
                        OrderRecipientAddress address = order.Delivery.Address != null ? new OrderRecipientAddress
                        {
                            Postcode = order.Delivery.Address.Postcode ?? "",
                            Country = order.Delivery.Address.Country ?? "",
                            City = order.Delivery.Address.City ?? "",
                            Subway = order.Delivery.Address.Subway ?? "",
                            Street = order.Delivery.Address.Street ?? "",
                            House = order.Delivery.Address.House ?? "",
                            Block = order.Delivery.Address.Block ?? "",
                            Entrance = order.Delivery.Address.Entrance ?? "",
                            Entryphone = order.Delivery.Address.Entryphone ?? "",
                            Floor = order.Delivery.Address.Floor ?? "",
                            Apartment = order.Delivery.Address.Apartment ?? ""
                        } : null;

                        var new_order = await _order.НовыйOrder(_defFirmaId, market.Encoding, authorizationApi, "Yandex " + (isFBS ? "FBS" : "DBS"), order.Id.ToString(),
                            (StinPaymentType)order.PaymentType, (StinPaymentMethod)order.PaymentMethod,
                            (StinDeliveryPartnerType)order.Delivery.DeliveryPartnerType, (StinDeliveryType)order.Delivery.Type, order.Delivery.DeliveryServiceId, order.Delivery.ServiceName, deliveryPrice, deliverySubsidy,
                            orderDeliveryShipmentId, orderShipmentDate,
                            order.Delivery.Region.Id.ToString(), order.Delivery.Region.Name, address, order.Notes, orderItems);
                        if (new_order == null)
                        {
                            if (_context.Database.CurrentTransaction != null)
                                tran.Rollback();
                            _logger.LogError("Ошибка создания заказа");
                            return Tuple.Create("", DateTime.Now);
                        }

                        var формаПредварительнаяЗаявка = await _предварительнаяЗаявка.НовыйДокумент(
                                 needToCalcDateTime ? dateTimeTA.AddMilliseconds(1) : DateTime.Now,
                                 _defFirmaId,
                                 string.IsNullOrEmpty(складОтгрузкиId) ? Common.SkladEkran : складОтгрузкиId,
                                 market.КонтрагентId,
                                 market.ДоговорId,
                                 Common.ТипЦенРозничный,
                                 new_order, НоменклатураList, address);
                        var result = await _предварительнаяЗаявка.ЗаписатьAsync(формаПредварительнаяЗаявка);
                        if (result != null)
                        {
                            if (_context.Database.CurrentTransaction != null)
                                tran.Rollback();
                            _logger.LogError(result.Description);
                            orderNo = null;
                        }
                        else
                        {
                            orderNo = формаПредварительнаяЗаявка.Общие.НомерДок + "-" + формаПредварительнаяЗаявка.Общие.ДатаДок.ToString("yyyy");
                            формаПредварительнаяЗаявка.Order.OrderNo = orderNo;
                            await _order.ОбновитьOrderNo(формаПредварительнаяЗаявка.Order.Id, orderNo);
                            result = await _предварительнаяЗаявка.ПровестиAsync(формаПредварительнаяЗаявка);
                            if (result != null)
                            {
                                if (_context.Database.CurrentTransaction != null)
                                    tran.Rollback();
                                _logger.LogError(result.Description);
                                orderNo = null;
                            }
                            else
                            {
                                реквизитыПроведенныхДокументов.Add(формаПредварительнаяЗаявка.Общие);
                                bool необходимоПеремещать = (формаПредварительнаяЗаявка.Order != null); //&&
                                    //(формаПредварительнаяЗаявка.Order.DeliveryPartnerType == StinDeliveryPartnerType.SHOP) &&
                                    //(формаПредварительнаяЗаявка.Order.DeliveryType == StinDeliveryType.PICKUP);
                                var ПереченьНаличия = await _предварительнаяЗаявка.РаспределитьТоварПоНаличиюAsync(формаПредварительнаяЗаявка, списокСкладовНаличияТовара);
                                List<string> notNativeKeys = new List<string> { "ДилерскаяЗаявка", "Спрос" };
                                var списокУслуг = формаПредварительнаяЗаявка.ТабличнаяЧасть.Where(x => x.Номенклатура.ЭтоУслуга).ToList();
                                if (ПереченьНаличия.Where(x => !notNativeKeys.Contains(x.Item2)).Count() > 0)
                                {
                                    foreach (var firmaId in ПереченьНаличия.Where(x => !notNativeKeys.Contains(x.Item2)).GroupBy(x => x.Item1).Select(gr => gr.Key))
                                    {
                                        foreach (var склId in ПереченьНаличия.Where(x => !notNativeKeys.Contains(x.Item2)).GroupBy(x => x.Item2).Select(gr => gr.Key))
                                        {
                                            var rowDataValue = ПереченьНаличия.Where(x => x.Item1 == firmaId && x.Item2 == склId).Select(x => x.Item3);
                                            if (rowDataValue.Count() > 0)
                                            {
                                                var rowData = rowDataValue.FirstOrDefault();
                                                if (необходимоПеремещать && (формаПредварительнаяЗаявка.Склад.Id != склId))
                                                {
                                                    var формаЗаявкаОдобренная = await _заявкаПокупателя.ВводНаОснованииAsync(формаПредварительнаяЗаявка, needToCalcDateTime ? dateTimeTA.AddMilliseconds(2) : DateTime.Now, списокУслуг.Count > 0, "Заявка (одобренная)", формаПредварительнаяЗаявка.Общие.Фирма.Id, формаПредварительнаяЗаявка.Склад.Id, rowData);
                                                    списокУслуг.Clear();
                                                    result = await _заявкаПокупателя.ЗаписатьПровестиAsync(формаЗаявкаОдобренная);
                                                    реквизитыПроведенныхДокументов.Add(формаЗаявкаОдобренная.Общие);
                                                    if (result != null)
                                                    {
                                                        if (_context.Database.CurrentTransaction != null)
                                                            tran.Rollback();
                                                        _logger.LogError(result.Description);
                                                        orderNo = null;
                                                        break;
                                                    }
                                                    //Перемещение со склада (включить маршрут)
                                                    var ПереченьФормаПеремещениеТМЦ = await _перемещениеТМЦ.ДляЗаказаЗаявки(формаЗаявкаОдобренная, needToCalcDateTime ? dateTimeTA.AddMilliseconds(3) : DateTime.Now, firmaId, склId);
                                                    foreach (var формаПеремещениеТмц in ПереченьФормаПеремещениеТМЦ)
                                                    {
                                                        result = await _перемещениеТМЦ.ЗаписатьПровестиAsync(формаПеремещениеТмц);
                                                        реквизитыПроведенныхДокументов.Add(формаПеремещениеТмц.Общие);
                                                        if (result != null)
                                                        {
                                                            if (_context.Database.CurrentTransaction != null)
                                                                tran.Rollback();
                                                            _logger.LogError(result.Description);
                                                            orderNo = null;
                                                            break;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    var формаСчет = await _заявкаПокупателя.ВводНаОснованииAsync(формаПредварительнаяЗаявка, needToCalcDateTime ? dateTimeTA.AddMilliseconds(2) : DateTime.Now, списокУслуг.Count > 0, "Счет на оплату", формаПредварительнаяЗаявка.Общие.Фирма.Id, склId, rowData);
                                                    списокУслуг.Clear();
                                                    result = await _заявкаПокупателя.ЗаписатьПровестиAsync(формаСчет);
                                                    реквизитыПроведенныхДокументов.Add(формаСчет.Общие);
                                                }
                                                if (result != null)
                                                {
                                                    if (_context.Database.CurrentTransaction != null)
                                                        tran.Rollback();
                                                    _logger.LogError(result.Description);
                                                    orderNo = null;
                                                    break;
                                                }
                                            }
                                        }
                                        if (result != null)
                                            break;
                                    }
                                }
                                if (ПереченьНаличия.Where(x => x.Item2 == "ДилерскаяЗаявка").Count() > 0)
                                {
                                    var rowData = ПереченьНаличия.Where(x => x.Item2 == "ДилерскаяЗаявка").Select(x => x.Item3).FirstOrDefault();
                                    if (rowData.Count > 0)
                                    {
                                        var формаДилерскаяЗаявка = await _заявкаПокупателя.ВводНаОснованииAsync(формаПредварительнаяЗаявка, needToCalcDateTime ? dateTimeTA.AddMilliseconds(2) : DateTime.Now, списокУслуг.Count > 0, "Заявка дилера", null, null, rowData);
                                        списокУслуг.Clear();
                                        result = await _заявкаПокупателя.ЗаписатьПровестиAsync(формаДилерскаяЗаявка);
                                        реквизитыПроведенныхДокументов.Add(формаДилерскаяЗаявка.Общие);
                                        if (result != null)
                                        {
                                            if (_context.Database.CurrentTransaction != null)
                                                tran.Rollback();
                                            _logger.LogError(result.Description);
                                            orderNo = null;
                                        }
                                    }
                                }
                                if (ПереченьНаличия.Where(x => x.Item2 == "Спрос").Count() > 0)
                                {
                                    var rowData = ПереченьНаличия.Where(x => x.Item2 == "Спрос").Select(x => x.Item3).FirstOrDefault();
                                    if (rowData.Count > 0)
                                    {
                                        var формаСпрос = await _спрос.ВводНаОснованииAsync(формаПредварительнаяЗаявка, needToCalcDateTime ? dateTimeTA.AddMilliseconds(2) : DateTime.Now, rowData);
                                        result = await _спрос.ЗаписатьПровестиAsync(формаСпрос);
                                        реквизитыПроведенныхДокументов.Add(формаСпрос.Общие);
                                        if (result != null)
                                        {
                                            if (_context.Database.CurrentTransaction != null)
                                                tran.Rollback();
                                            _logger.LogError(result.Description);
                                            orderNo = null;
                                        }
                                    }
                                }
                            }
                            if (реквизитыПроведенныхДокументов.Count > 0)
                                await _предварительнаяЗаявка.ОбновитьАктивность(реквизитыПроведенныхДокументов);
                        }
                        if (_context.Database.CurrentTransaction != null)
                            tran.Commit();
                    }
                    catch (DbUpdateException db_ex)
                    {
                        if (_context.Database.CurrentTransaction != null)
                            _context.Database.CurrentTransaction.Rollback();
                        _logger.LogError(db_ex.InnerException.ToString());
                    }
                    catch (Exception ex)
                    {
                        if (_context.Database.CurrentTransaction != null)
                            _context.Database.CurrentTransaction.Rollback();
                        _logger.LogError(ex.Message);
                    }
                }
            }
            else
                orderShipmentDate = getOrder.ShipmentDate;
            return Tuple.Create(orderNo, orderShipmentDate);
        }
        public async Task ChangeStatus(Order order, string authorizationApi, long Id, StatusYandex newStatus, SubStatusYandex newSubStatus, string userId = null, ReceiverPaymentType receiverPaymentType = ReceiverPaymentType.NotFound, string receiverEmail = null, string receiverPhone = null)
        {
            if (order == null)
            {
                var defFirma = _configuration["Settings:Firma"];
                string _defFirmaId = _configuration["Settings:" + defFirma + ":FirmaId"];
                var market = await _marketplace.ПолучитьMarketplaceByFirma(authorizationApi, _defFirmaId);
                if (market == null)
                {
                    _logger.LogError("Не обнаружен маркетплейс для фирмы " + _defFirmaId + " и authApi " + authorizationApi);
                    return;
                }
                order = await _order.ПолучитьOrderByMarketplaceId(market.Id, Id.ToString());
            }
            if (order != null && ((order.Status != (int)newStatus) || (order.SubStatus != (int)newSubStatus)))
            {
                using var tran = await _context.Database.BeginTransactionAsync();
                try
                {
                    DateTime dateTimeTA = _context.GetDateTimeTA();
                    bool needToCalcDateTime = dateTimeTA.Month != DateTime.Now.Month;
                    var реквизитыПроведенныхДокументов = new List<ОбщиеРеквизиты>();
                    if (order.Status == (int)StatusYandex.RESERVED && newStatus == StatusYandex.PROCESSING)
                    {
                        await _order.ОбновитьOrderStatus(order.Id, 8);
                    }
                    else if (order.Status == (int)StatusYandex.PROCESSING && order.SubStatus == (int)SubStatusYandex.READY_TO_SHIP &&
                        newStatus == StatusYandex.PROCESSING && newSubStatus == SubStatusYandex.SHIPPED)
                    {
                        bool isDBS = order.DeliveryPartnerType == StinDeliveryPartnerType.SHOP;
                        var активныеНаборы = await _набор.ПолучитьСписокАктивныхНаборов(order.Id, true);
                        if (активныеНаборы != null && активныеНаборы.Count > 0)
                        {
                            var формаПредварительнаяЗаявка = await _предварительнаяЗаявка.GetФормаПредварительнаяЗаявкаByOrderId(order.Id);
                            decimal суммаКОплате = ((формаПредварительнаяЗаявка.Order.DeliveryPartnerType == StinDeliveryPartnerType.SHOP) && (формаПредварительнаяЗаявка.Order.PaymentType == StinPaymentType.POSTPAID)) ? формаПредварительнаяЗаявка.ТабличнаяЧасть.Sum(x => x.Сумма) : 0m;
                            Sno системаНалогооблажения = Sno.osn;
                            if (!string.IsNullOrWhiteSpace(формаПредварительнаяЗаявка.Общие.Фирма.СистемаНалогооблажения))
                            {
                                if (!int.TryParse(формаПредварительнаяЗаявка.Общие.Фирма.СистемаНалогооблажения, out int intSno))
                                    intSno = 0;
                                системаНалогооблажения = (Sno)intSno;
                            }
                            var списокКомплеснаяПродажа = await _комплекснаяПродажа.ЗаполнитьНаОснованииAsync(userId, формаПредварительнаяЗаявка, needToCalcDateTime ? dateTimeTA.AddMilliseconds(1) : DateTime.Now, активныеНаборы, !isDBS);
                            foreach (var формаКомплеснаяПродажа in списокКомплеснаяПродажа)
                            {
                                var result = await _комплекснаяПродажа.ЗаписатьПровестиAsync(формаКомплеснаяПродажа);
                                реквизитыПроведенныхДокументов.Add(формаКомплеснаяПродажа.Общие);
                                if (result == null)
                                {
                                    ФормаРеализация формаРеализация = null;
                                    var ПереченьНаличия = await _комплекснаяПродажа.РаспределитьТоварПоНаличиюAsync(формаКомплеснаяПродажа);
                                    var списокУслуг = формаКомплеснаяПродажа.ТабличнаяЧастьРазвернутая.Where(x => x.Номенклатура.ЭтоУслуга).ToList();
                                    foreach (var data in ПереченьНаличия)
                                    {
                                        формаРеализация = await _реализация.ВводНаОснованииAsync(формаКомплеснаяПродажа, needToCalcDateTime ? dateTimeTA.AddMilliseconds(2) : DateTime.Now, data.Key, data.Value, списокУслуг);
                                        списокУслуг.Clear();
                                        result = await _реализация.ЗаписатьПровестиAsync(формаРеализация);
                                        реквизитыПроведенныхДокументов.Add(формаРеализация.Общие);
                                        if ((result == null) && (!формаРеализация.Общие.Фирма.НетСчетФактуры) && (формаРеализация.КодОперации.Key == "   16S   "))
                                        {
                                            var формаСчетФактура = await _счетФактура.ВводНаОснованииAsync(формаРеализация, needToCalcDateTime ? dateTimeTA.AddMilliseconds(3) : DateTime.Now);
                                            result = await _счетФактура.ЗаписатьПровестиAsync(формаСчетФактура);
                                            реквизитыПроведенныхДокументов.Add(формаСчетФактура.Общие);
                                        }
                                        if ((result == null) && (суммаКОплате > 0))
                                        {
                                            decimal суммаРеализации = формаРеализация.ТабличнаяЧасть.Sum(x => x.Сумма);
                                            decimal суммаПКО = Math.Min(суммаКОплате, суммаРеализации);
                                            if (суммаПКО > 0)
                                            {
                                                суммаКОплате = суммаКОплате - суммаПКО;
                                                var формаПКО = await _пко.ВводНаОснованииAsync(формаРеализация, needToCalcDateTime ? dateTimeTA.AddMilliseconds(3) : DateTime.Now, суммаПКО, receiverPaymentType);
                                                result = await _пко.ЗаписатьПровестиAsync(формаПКО, null, true);
                                                реквизитыПроведенныхДокументов.Add(формаПКО.Общие);
                                                if ((result == null) && (receiverPaymentType != ReceiverPaymentType.NotFound) &&
                                                    (!string.IsNullOrWhiteSpace(receiverEmail) || !string.IsNullOrWhiteSpace(receiverPhone)))
                                                {
                                                    var формаЧекКкмТЧ = new List<ФормаЧекКкмТЧ>();
                                                    формаЧекКкмТЧ.Add(new ФормаЧекКкмТЧ
                                                    {
                                                        ТипОплаты = receiverPaymentType == ReceiverPaymentType.БанковскойКартой ? ТипыОплатыЧекаККМ.БанковскаяКарта : ТипыОплатыЧекаККМ.Наличными,
                                                        СуммаОплаты = суммаПКО
                                                    });
                                                    if (суммаРеализации > суммаПКО)
                                                    {
                                                        формаЧекКкмТЧ.Add(new ФормаЧекКкмТЧ
                                                        {
                                                            ТипОплаты = ТипыОплатыЧекаККМ.Кредит,
                                                            СуммаОплаты = суммаРеализации - суммаПКО
                                                        });
                                                    }
                                                    var формаЧек = await _чек.ВводНаОснованииAsync(формаПКО, needToCalcDateTime ? dateTimeTA.AddMilliseconds(4) : DateTime.Now, ВидыОперацииЧекаККМ.Чек, order.MarketplaceId, receiverEmail, receiverPhone,
                                                            !string.IsNullOrWhiteSpace(receiverEmail), !string.IsNullOrWhiteSpace(receiverPhone), true, формаЧекКкмТЧ);
                                                    result = await _чек.ЗаписатьAsync(формаЧек);
                                                    if (result == null)
                                                    {
                                                        //атол онлайн
                                                        var sellRequest = new SellRequest();
                                                        sellRequest.timestamp = DateTime.Now;
                                                        sellRequest.external_id = order.MarketplaceId + "/" + order.OrderNo;
                                                        sellRequest.receipt = new Receipt();
                                                        sellRequest.receipt.client = new SellClient();
                                                        if (!string.IsNullOrWhiteSpace(receiverEmail))
                                                            sellRequest.receipt.client.email = receiverEmail;
                                                        if (!string.IsNullOrWhiteSpace(receiverPhone))
                                                            sellRequest.receipt.client.phone = receiverPhone;

                                                        sellRequest.receipt.company = new SellCompany
                                                        {
                                                            inn = формаЧек.Общие.Фирма.ЮрЛицо.ТолькоИНН,
                                                            sno = системаНалогооблажения,
                                                            payment_address = формаЧек.Общие.Фирма.МестоОнлайнРасчетов
                                                        };
                                                        sellRequest.receipt.items = new List<SellItem>();
                                                        foreach (var row in формаРеализация.ТабличнаяЧасть)
                                                        {
                                                            sellRequest.receipt.items.Add(new SellItem
                                                            {
                                                                name = row.Номенклатура.Наименование,
                                                                price = row.Сумма / row.Количество,
                                                                quantity = row.Количество,
                                                                sum = row.Сумма,
                                                                measurement_unit = row.Единица.Code,
                                                                payment_method = суммаПКО == суммаРеализации ? SellPaymentMethod.full_payment : SellPaymentMethod.partial_payment,
                                                                payment_object = row.Номенклатура.ЭтоУслуга ? SellPaymentObject.service : SellPaymentObject.commodity,
                                                                vat = new SellVat { type = AtolOperations.АтолСтавкиНДС.Where(x => x.Key == row.СтавкаНДС.Наименование).Select(x => x.Value).FirstOrDefault() }
                                                            });
                                                        }
                                                        sellRequest.receipt.payments = new List<SellPaymentItem>();
                                                        sellRequest.receipt.payments.Add(new SellPaymentItem
                                                        {
                                                            type = receiverPaymentType == ReceiverPaymentType.БанковскойКартой ? SellPaymentType.безналичный : SellPaymentType.наличные,
                                                            sum = суммаПКО
                                                        });
                                                        if (суммаРеализации > суммаПКО)
                                                        {
                                                            sellRequest.receipt.payments.Add(new SellPaymentItem
                                                            {
                                                                type = SellPaymentType.кредит,
                                                                sum = суммаРеализации - суммаПКО
                                                            });
                                                        }
                                                        sellRequest.receipt.vats = new List<SellVat>();
                                                        foreach (var gr in формаРеализация.ТабличнаяЧасть.GroupBy(x => x.СтавкаНДС.Наименование))
                                                        {
                                                            sellRequest.receipt.vats.Add(new SellVat
                                                            {
                                                                type = AtolOperations.АтолСтавкиНДС.Where(x => x.Key == gr.Key).Select(x => x.Value).FirstOrDefault(),
                                                                sum = gr.Sum(x => x.СуммаНДС)
                                                            });
                                                        }
                                                        sellRequest.receipt.total = суммаРеализации;

                                                        var sellResult = await AtolOperations.СоздатьЧекПриход(
                                                                формаЧек.Общие.Фирма.AtolLogin,
                                                                формаЧек.Общие.Фирма.AtolPassword,
                                                                формаЧек.Общие.Фирма.AlolGroupCode,
                                                                sellRequest);
                                                        if (string.IsNullOrWhiteSpace(sellResult))
                                                            result = await _чек.ПровестиAsync(формаЧек, true);
                                                        else
                                                            result = new ExceptionData { Description = sellResult };
                                                    }
                                                    реквизитыПроведенныхДокументов.Add(формаЧек.Общие);
                                                }
                                            }
                                        }
                                        if (result != null)
                                            break;
                                    }
                                }
                                if (result != null)
                                    break;
                            }
                            await _order.ОбновитьOrderStatus(order.Id, isDBS ? 6 : 14);
                        }
                        else
                            await _order.ОбновитьOrderStatus(order.Id, isDBS ? -6 : -14);
                    }
                    else if (newStatus == StatusYandex.CANCELLED)
                    {
                        var активныеСчета = await _заявкаПокупателя.ПолучитьСписокАктивныхСчетов(order.Id, true);
                        var активныеПеремещения = await _перемещениеТМЦ.ПолучитьСписокАктивныхПеремещений(активныеСчета);
                        foreach (var перемещениеТмц in активныеПеремещения)
                        {
                            var возвратИзДоставки = await _возвратИзДоставки.ВводНаОснованииAsync(перемещениеТмц, needToCalcDateTime ? dateTimeTA.AddMilliseconds(1) : DateTime.Now);
                            var result = await _возвратИзДоставки.ЗаписатьПровестиAsync(возвратИзДоставки);
                            реквизитыПроведенныхДокументов.Add(возвратИзДоставки.Общие);
                            if (result != null)
                            {
                                if (_context.Database.CurrentTransaction != null)
                                    tran.Rollback();
                                _logger.LogError(result.Description);
                                break;
                            }
                        }
                        var отменыЗаявок = await _отменаЗаявок.ВводНаОснованииAsync(активныеСчета, needToCalcDateTime ? dateTimeTA.AddMilliseconds(2) : DateTime.Now);
                        foreach (var формаОтменаЗаявок in отменыЗаявок)
                        {
                            var result = await _отменаЗаявок.ЗаписатьПровестиAsync(формаОтменаЗаявок);
                            реквизитыПроведенныхДокументов.Add(формаОтменаЗаявок.Общие);
                            if (result != null)
                            {
                                if (_context.Database.CurrentTransaction != null)
                                    tran.Rollback();
                                _logger.LogError(result.Description);
                                break;
                            }
                        }
                        var активныеНаборы = await _набор.ПолучитьСписокАктивныхНаборов(order.Id, false);
                        foreach (var формаНабор in активныеНаборы)
                        {
                            var формаОтменаНабора = await _отменаНабора.ВводНаОснованииAsync(формаНабор, needToCalcDateTime ? dateTimeTA.AddMilliseconds(3) : DateTime.Now);
                            var result = await _отменаНабора.ЗаписатьПровестиAsync(формаОтменаНабора);
                            реквизитыПроведенныхДокументов.Add(формаОтменаНабора.Общие);
                            if (result != null)
                            {
                                if (_context.Database.CurrentTransaction != null)
                                    tran.Rollback();
                                _logger.LogError(result.Description);
                                break;
                            }
                        }
                        await _order.ОбновитьOrderStatus(order.Id, 5);
                    }
                    if (реквизитыПроведенныхДокументов.Count > 0)
                        await _предварительнаяЗаявка.ОбновитьАктивность(реквизитыПроведенныхДокументов);
                    else
                        await _предварительнаяЗаявка.ОбновитьСетевуюАктивность();
                    if (_context.Database.CurrentTransaction != null)
                        tran.Commit();
                }
                catch (DbUpdateException db_ex)
                {
                    if (_context.Database.CurrentTransaction != null)
                        _context.Database.CurrentTransaction.Rollback();
                    _logger.LogError(db_ex.InnerException.ToString());
                }
                catch (Exception ex)
                {
                    if (_context.Database.CurrentTransaction != null)
                        _context.Database.CurrentTransaction.Rollback();
                    _logger.LogError(ex.Message);
                }
            }
        }
        public async Task<bool> ReduceCancelItems(string orderNo, string authorizationApi, List<OrderItem> cancelItems, CancellationToken cancellationToken)
        {
            var defFirma = _configuration["Settings:Firma"];
            string _defFirmaId = _configuration["Settings:" + defFirma + ":FirmaId"];
            var market = await _marketplace.ПолучитьMarketplaceByFirma(authorizationApi, _defFirmaId);
            if (market == null)
            {
                _logger.LogError("Не обнаружен маркетплейс для фирмы " + _defFirmaId + " и authApi " + authorizationApi);
                return false;
            }
            var order = await _order.ПолучитьOrderWithItems(market.Id, orderNo);
            if (order != null && (order.InternalStatus != 5) && (order.InternalStatus != 6))
            {
                var reduceList = new List<OrderItem>();
                foreach (var item in order.Items)
                {
                    if (cancelItems.Any(x => x.Sku == item.Sku))
                    {
                        var строкаВозврата = cancelItems.FirstOrDefault(x => x.Sku == item.Sku);
                        if (item.Количество >= строкаВозврата.Количество)
                        {
                            reduceList.Add(new OrderItem { НоменклатураId = item.НоменклатураId, Количество = строкаВозврата.Количество });
                            item.Количество = Math.Round(item.Количество - строкаВозврата.Количество);
                            cancelItems.Remove(строкаВозврата);
                        }
                        else
                        {
                            reduceList.Add(new OrderItem { НоменклатураId = item.НоменклатураId, Количество = item.Количество });
                            строкаВозврата.Количество = Math.Round(строкаВозврата.Количество - item.Количество);
                            item.Количество = 0;
                        }
                    }
                }
                if (cancelItems.Count > 0)
                {
                    _logger.LogError("Превышено количество возвращаемых позиций по orderNo = " + orderNo);
                    return false;
                }
                if (order.Items.Where(x => x.Количество > 0).Any())
                {
                    // частичная отмена
                    _logger.LogError("Частичная отмена по orderNo = " + orderNo + " не реализована");
                    return false;
                }
                else
                {
                    // полная отмена
                    await ChangeStatus(order, authorizationApi, 0, StatusYandex.CANCELLED, SubStatusYandex.NotFound);
                }
            }
            return true;
        }
        public async Task<string> ReduceCancelItems(string docId, CancellationToken stoppingToken)
        {
            //using IOrder _order = new OrderEntity(_context);
            //using IВозвратИзНабора _возвратИзНабора = new ВозвратИзНабора(_context);
            //using IОтменаНабора _отменаНабора = new ОтменаНабора(_context);
            string result = "";
            using var tran = await _context.Database.BeginTransactionAsync();
            try
            {
                Order order = null;
                List<OrderItem> списокВозврата = new List<OrderItem>();
                decimal СуммаЗаявки = 0;
                decimal СуммаВозврата = 0;
                //определить вид док-та 
                var ДокВид = await _отменаНабора.ПолучитьВидДокумента(docId);
                //получить orderId из документа
                if (ДокВид == ВидДокумента.ВозвратИзНабора)
                {
                    var docV = await _возвратИзНабора.GetФормаВозвратИзНабораById(docId);
                    order = docV.Order;
                    СуммаВозврата = docV.ТабличнаяЧасть.Sum(x => x.Сумма);
                    foreach (var row in docV.ТабличнаяЧасть)
                    {
                        списокВозврата.Add(new OrderItem { НоменклатураId = row.Номенклатура.Id, Количество = row.Количество });
                    }
                }
                else if (ДокВид == ВидДокумента.ОтменаНабора)
                {
                    var docO = await _отменаНабора.GetФормаОтменаНабораById(docId);
                    order = docO.Order;
                    var ТаблЧастьОтменыНабора = await _отменаНабора.ПолучитьСписокВозвращаемыхПозиций(docO);
                    СуммаВозврата = ТаблЧастьОтменыНабора.Sum(x => x.Сумма);
                    foreach (var row in ТаблЧастьОтменыНабора)
                    {
                        списокВозврата.Add(new OrderItem { НоменклатураId = row.Номенклатура.Id, Количество = row.Количество });
                    }
                }
                if (списокВозврата.Count == 0)
                    return "Возвращаемые позиции не обнаружены";
                if (order != null)
                {
                    СуммаЗаявки = order.Items.Sum(x => x.Цена * x.Количество);
                    if (СуммаЗаявки == 0)
                        return "Нулевая сумма исходной заявки";
                    if (((order.Status == (int)StinOrderStatus.PROCESSING) && (order.SubStatus == (int)StinOrderSubStatus.READY_TO_SHIP)) ||
                        (СуммаВозврата/СуммаЗаявки >= 0.99m))
                    {
                        //только отмена
                        if (order.Тип == "ЯНДЕКС")
                        {
                            var yandexResult = await YandexClasses.YandexOperators.ChangeStatus(_httpService, "",
                                order.CampaignId, order.MarketplaceId,
                                order.ClientId, order.AuthToken,
                                YandexClasses.StatusYandex.CANCELLED, YandexClasses.SubStatusYandex.SHOP_FAILED,
                                (YandexClasses.DeliveryType)order.DeliveryType,
                                stoppingToken);
                            if (yandexResult.Item1)
                                await _order.ОбновитьOrderStatus(order.Id, 5);
                            else
                            {
                                await _order.ОбновитьOrderStatus(order.Id, -5, yandexResult.Item2);
                                result += yandexResult.Item2;
                            }
                        }
                        else if (order.Тип == "SBER")
                        {
                            var sberResult = await SberClasses.Functions.OrderReject(_httpService, "", order.AuthToken, order.MarketplaceId, SberClasses.SberReason.OUT_OF_STOCK,
                                order.Items.Select(x => new KeyValuePair<string,string> (x.Id,x.Sku)).ToList(), stoppingToken);
                            if (!string.IsNullOrEmpty(sberResult.error))
                                result += sberResult.error;
                            if (sberResult.success)
                                await _order.ОбновитьOrderStatus(order.Id, 5);
                            else
                            {
                                _logger.LogError(result);
                                await _order.ОбновитьOrderStatus(order.Id, -5, sberResult.error);
                            }
                        }
                        else if (order.Тип == "OZON")
                        {
                            result += await OzonClasses.OzonOperators.CancelOrder(_httpService, "", order.ClientId, order.AuthToken,
                                order.MarketplaceId, 352, "Product is out of stock", null, stoppingToken);
                            if (!string.IsNullOrEmpty(result))
                            {
                                _logger.LogError(result);
                                await _order.ОбновитьOrderStatus(order.Id, -5);
                            }
                            else
                                await _order.ОбновитьOrderStatus(order.Id, 5);
                        }
                        else if (order.Тип == "WILDBERRIES")
                        {
                            var cancelResult = await WbClasses.Functions.CancelOrder(_httpService, "", order.AuthToken,
                                order.MarketplaceId, stoppingToken);
                            if (!string.IsNullOrEmpty(cancelResult.error))
                            {
                                _logger.LogError(cancelResult.error);
                                result += cancelResult.error;
                            }
                            if (!cancelResult.success)
                            {
                                _logger.LogError("Wb order: " + order.MarketplaceId + " can't be cancelled");
                                result += "Wb order: " + order.MarketplaceId + " can't be cancelled";
                            }
                        }
                        _context.ОбновитьСетевуюАктивность();
                        await _context.SaveChangesAsync(stoppingToken);
                    }
                    else
                    {
                        //изменение состава
                        if (order.Тип == "ЯНДЕКС")
                        {
                            var request = new YandexClasses.ChangeItemsRequest { Items = new List<YandexClasses.ChangeItem>() };

                            foreach (var item in order.Items)
                            {
                                if (списокВозврата.Any(x => x.НоменклатураId == item.НоменклатураId))
                                {
                                    var строкаВозврата = списокВозврата.FirstOrDefault(x => x.НоменклатураId == item.НоменклатураId);
                                    if (item.Количество >= строкаВозврата.Количество)
                                    {
                                        item.Количество = Math.Round(item.Количество - строкаВозврата.Количество);
                                        списокВозврата.Remove(строкаВозврата);
                                    }
                                    else
                                    {
                                        строкаВозврата.Количество = Math.Round(строкаВозврата.Количество - item.Количество);
                                        item.Количество = 0;
                                    }
                                    request.Items.Add(new YandexClasses.ChangeItem
                                    {
                                        Id = long.Parse(item.Id),
                                        Count = (int)item.Количество,
                                    });
                                }
                            }
                            var yandexResult = await YandexClasses.YandexOperators.Exchange<YandexClasses.ErrorResponse>(_httpService,
                                string.Format(YandexClasses.YandexOperators.urlChangeItems, order.CampaignId, order.MarketplaceId),
                                HttpMethod.Put,
                                order.ClientId,
                                order.AuthToken,
                                request,
                                stoppingToken);
                            if ((yandexResult.Item1 == YandexClasses.ResponseStatus.ERROR) || (yandexResult.Item2 == null) || (yandexResult.Item2.Status == YandexClasses.ResponseStatus.ERROR))
                            {
                                if (!string.IsNullOrEmpty(yandexResult.Item3))
                                {
                                    result += yandexResult.Item3;
                                    _logger.LogError(yandexResult.Item3);
                                }
                                await _order.ОбновитьOrderStatus(order.Id, -4);
                            }
                            else
                            {
                                foreach (var item in order.Items)
                                {
                                    var entityItem = await _context.Sc14033s.FirstOrDefaultAsync(x => x.Parentext == order.Id && x.Sp14022 == item.НоменклатураId);
                                    if (entityItem != null)
                                    {
                                        if (item.Количество == 0)
                                            _context.Sc14033s.Remove(entityItem);
                                        else
                                        {
                                            entityItem.Sp14023 = item.Количество;
                                            _context.Update(entityItem);
                                        }
                                        _context.РегистрацияИзмененийРаспределеннойИБ(14033, entityItem.Id);
                                    }
                                }
                            }
                        }
                        else if (order.Тип == "SBER")
                        {
                            var cancelData = new List<KeyValuePair<string, string>>();
                            foreach (var item in order.Items)
                            {
                                if (списокВозврата.Any(x => x.НоменклатураId == item.НоменклатураId))
                                {
                                    var строкаВозврата = списокВозврата.FirstOrDefault(x => x.НоменклатураId == item.НоменклатураId);
                                    if (item.Количество >= строкаВозврата.Количество)
                                    {
                                        item.Количество = Math.Round(item.Количество - строкаВозврата.Количество);
                                        списокВозврата.Remove(строкаВозврата);
                                    }
                                    else
                                    {
                                        строкаВозврата.Количество = Math.Round(строкаВозврата.Количество - item.Количество);
                                        item.Количество = 0;
                                    }
                                    cancelData.Add(new(item.Id, item.Sku));
                                }
                            }

                            var sberResult = await SberClasses.Functions.OrderReject(_httpService, "", order.AuthToken, order.MarketplaceId, SberClasses.SberReason.OUT_OF_STOCK,
                                cancelData, stoppingToken);
                            if (!string.IsNullOrEmpty(sberResult.error))
                                result += sberResult.error;
                            if (sberResult.success)
                            {
                                foreach (var item in order.Items)
                                {
                                    var entityItem = await _context.Sc14033s.FirstOrDefaultAsync(x => (x.Parentext == order.Id) && (x.Sp14022 == item.НоменклатураId) && (x.Code == item.Id));
                                    if (entityItem != null)
                                    {
                                        if (item.Количество == 0)
                                            _context.Sc14033s.Remove(entityItem);
                                        else
                                        {
                                            entityItem.Sp14023 = item.Количество;
                                            _context.Update(entityItem);
                                        }
                                        _context.РегистрацияИзмененийРаспределеннойИБ(14033, entityItem.Id);
                                    }
                                }
                                var files = await _context.VzOrderBinaries.Where(x => x.Id == order.Id && x.Extension == "LABELS")
                                    .ToListAsync();
                                foreach (var file in files)
                                    _context.VzOrderBinaries.Remove(file);
                                if (order.InternalStatus == 2)
                                    await _order.ОбновитьOrderStatus(order.Id, 1);
                            }
                            else
                            {
                                _logger.LogError(result);
                                await _order.ОбновитьOrderStatus(order.Id, -4, sberResult.error);
                            }
                        }
                        else if (order.Тип == "OZON")
                        {
                            var cancelData = new List<OzonClasses.CancelItem>();
                            foreach (var item in order.Items)
                            {
                                if (списокВозврата.Any(x => x.НоменклатураId == item.НоменклатураId))
                                {
                                    var строкаВозврата = списокВозврата.FirstOrDefault(x => x.НоменклатураId == item.НоменклатураId);
                                    if (item.Количество >= строкаВозврата.Количество)
                                    {
                                        item.Количество = Math.Round(item.Количество - строкаВозврата.Количество);
                                        списокВозврата.Remove(строкаВозврата);
                                    }
                                    else
                                    {
                                        строкаВозврата.Количество = Math.Round(строкаВозврата.Количество - item.Количество);
                                        item.Количество = 0;
                                    }
                                    cancelData.Add(new OzonClasses.CancelItem
                                    {
                                        Sku = long.Parse(item.Id),
                                        Quantity = (int)item.Количество,
                                    });
                                }
                            }
                            result += await OzonClasses.OzonOperators.CancelOrder(_httpService, "", order.ClientId, order.AuthToken,
                                order.MarketplaceId, 400, "Fall off", cancelData, stoppingToken);
                            if (!string.IsNullOrEmpty(result))
                            {
                                _logger.LogError(result);
                                await _order.ОбновитьOrderStatus(order.Id, -4);
                            }
                            else
                            {
                                foreach (var item in order.Items)
                                {
                                    var entityItem = await _context.Sc14033s.FirstOrDefaultAsync(x => x.Parentext == order.Id && x.Sp14022 == item.НоменклатураId);
                                    if (entityItem != null)
                                    {
                                        if (item.Количество == 0)
                                            _context.Sc14033s.Remove(entityItem);
                                        else
                                        {
                                            entityItem.Sp14023 = item.Количество;
                                            _context.Update(entityItem);
                                        }
                                        _context.РегистрацияИзмененийРаспределеннойИБ(14033, entityItem.Id);
                                    }
                                }
                            }
                        }
                        else if (order.Тип == "WILDBERRIES")
                        {
                            result += "Частичная отмена заказа Wildberries невозможна";
                            _logger.LogError("Частичная отмена заказа Wildberries невозможна");
                        }    
                        _context.ОбновитьСетевуюАктивность();
                        await _context.SaveChangesAsync();
                    }
                }
                if (_context.Database.CurrentTransaction != null)
                    tran.Commit();
            }
            catch (DbUpdateException db_ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                _logger.LogError(db_ex.InnerException.ToString());
                result += db_ex.InnerException.ToString();
            }
            catch (Exception ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                _logger.LogError(ex.Message);
                result += ex.Message;
            }
            return result;
        }
        public async Task<string> SetStatusShipped(string orderId, string userId, ReceiverPaymentType paymentType, string email, string phone, CancellationToken cancellationToken)
        {
            string result = "";
            var order = await _order.ПолучитьOrderWithItems(orderId);
            if ((order != null) && (order.Status == (int)StatusYandex.PROCESSING) && (order.SubStatus == (int)SubStatusYandex.READY_TO_SHIP))
            {
                int newStatus = 14;
                if (order.DeliveryPartnerType == StinDeliveryPartnerType.SHOP)
                    newStatus = 6;
                //using var tran = await _context.Database.BeginTransactionAsync();
                try
                {
                    if (order.Тип == "ЯНДЕКС")
                    {
                        var yandexResult = await YandexClasses.YandexOperators.ChangeStatus(_httpService, "",
                            order.CampaignId, order.MarketplaceId,
                            order.ClientId, order.AuthToken,
                            order.DeliveryPartnerType == StinDeliveryPartnerType.SHOP ? StatusYandex.DELIVERED : StatusYandex.PROCESSING,
                            order.DeliveryPartnerType == StinDeliveryPartnerType.SHOP ? SubStatusYandex.NotFound : SubStatusYandex.SHIPPED,
                            (YandexClasses.DeliveryType)order.DeliveryType,
                            cancellationToken);
                        string alreadyDeliveredError = $"STATUS_NOT_ALLOWED : Order {order.MarketplaceId} with status DELIVERED and substatus DELIVERY_SERVICE_DELIVERED is not allowed for status DELIVERED and substatus DELIVERY_SERVICE_DELIVERED";
                        if (!string.IsNullOrEmpty(yandexResult.Item2) && yandexResult.Item2 != alreadyDeliveredError)
                            result += yandexResult.Item2;
                    }
                    else if (order.Тип == "SBER")
                    {
                        int boxCount = order.Items.Sum(x => (int)x.КолМест);
                        var sberResult = await SberClasses.Functions.OrderShipping(_httpService,
                            order.CampaignId, order.AuthToken, order.MarketplaceId, order.OrderNo, DateTime.Now, boxCount, cancellationToken);
                        //if (sberResult.success)
                        //    await _order.ОбновитьOrderStatus(order.Id, newStatus);
                        //else
                        //    await _order.ОбновитьOrderStatus(order.Id, -newStatus, sberResult.error);
                        result += sberResult.error;
                    }
                    //if (_context.Database.CurrentTransaction != null)
                    //    tran.Commit();
                }
                catch (Exception ex)
                {
                    //if (_context.Database.CurrentTransaction != null)
                    //    _context.Database.CurrentTransaction.Rollback();
                    _logger.LogError(ex.Message);
                    result += ex.Message + " " + ex.InnerException;
                }
                if (string.IsNullOrEmpty(result))
                    await ChangeStatus(order, order.AuthorizationApi, long.Parse(order.MarketplaceId), StatusYandex.PROCESSING, SubStatusYandex.SHIPPED, userId, paymentType, email, phone);
            }
            else
                result += "Order не обнаружен";
            return result;
        }
        public async Task<string> SetStatusCancelledUserChangeMind(string orderId, CancellationToken cancellationToken)
        {
            string result = "";
            var order = await _order.ПолучитьOrder(orderId);
            if ((order != null) && (order.Status == (int)StatusYandex.PROCESSING) && (order.SubStatus == (int)SubStatusYandex.READY_TO_SHIP))
            {
                using var tran = await _context.Database.BeginTransactionAsync();
                try
                {
                    var yandexResult = await YandexClasses.YandexOperators.ChangeStatus(_httpService, "",
                        order.CampaignId, order.MarketplaceId,
                        order.ClientId, order.AuthToken,
                        YandexClasses.StatusYandex.CANCELLED,
                        YandexClasses.SubStatusYandex.USER_CHANGED_MIND,
                        (YandexClasses.DeliveryType)order.DeliveryType,
                        cancellationToken);
                    if (yandexResult.Item1)
                        await _order.ОбновитьOrderStatus(order.Id, 5);
                    else
                        await _order.ОбновитьOrderStatus(order.Id, -5, yandexResult.Item2);
                    result = yandexResult.Item2;
                    if (_context.Database.CurrentTransaction != null)
                        tran.Commit();
                }
                catch (DbUpdateException db_ex)
                {
                    if (_context.Database.CurrentTransaction != null)
                        _context.Database.CurrentTransaction.Rollback();
                    _logger.LogError(db_ex.InnerException.ToString());
                    result += db_ex.InnerException.ToString();
                }
                catch (Exception ex)
                {
                    if (_context.Database.CurrentTransaction != null)
                        _context.Database.CurrentTransaction.Rollback();
                    _logger.LogError(ex.Message);
                    result += ex.Message + " " + ex.InnerException;
                }
                if (string.IsNullOrEmpty(result))
                    await ChangeStatus(order, order.AuthorizationApi, long.Parse(order.MarketplaceId), StatusYandex.CANCELLED, SubStatusYandex.USER_CHANGED_MIND);
            }
            else
                result += "Order не обнаружен";
            return result;
        }
        public async Task<int> РассчитатьКолвоДнейВыполнения(string складId, int addDays)
        {
            //using IСклад _склад = new СкладEntity(_context);
            return await _склад.ЭтоРабочийДень(складId, addDays);
        }
        public async Task SetCancelNotify(string authorizationApi, long Id)
        {
            var defFirma = _configuration["Settings:Firma"];
            string _defFirmaId = _configuration["Settings:" + defFirma + ":FirmaId"];
            var market = await _marketplace.ПолучитьMarketplaceByFirma(authorizationApi, _defFirmaId);
            if (market == null)
            {
                _logger.LogError("Не обнаружен маркетплейс для фирмы " + _defFirmaId + " и authApi " + authorizationApi);
                return;
            }
            var order = await _order.ПолучитьOrderByMarketplaceId(market.Id, Id.ToString());
            if (order != null)
            {
                await _order.ОбновитьOrderStatus(order.Id, 7);
                _context.ОбновитьСетевуюАктивность();
            }
        }
        public async Task<Marketplace> ПолучитьМаркет(string authorizationApi, string firmaId)
        {
            return await _marketplace.ПолучитьMarketplaceByFirma(authorizationApi, firmaId);
        }
        public async Task SetRecipientAndAddress(string authorizationApi, long Id, string LastName, string FirstName, string MiddleName, string Recipient, string Phone,
            string PostCode, string Country, string City, string Subway, string Street, string House, string Block, string Entrance, string Entryphone, string Floor, string Apartment,
            string CustomerNotes)
        {
            var defFirma = _configuration["Settings:Firma"];
            string _defFirmaId = _configuration["Settings:" + defFirma + ":FirmaId"];
            var market = await _marketplace.ПолучитьMarketplaceByFirma(authorizationApi, _defFirmaId);
            if (market == null)
            {
                _logger.LogError("Не обнаружен маркетплейс для фирмы " + _defFirmaId + " и authApi " + authorizationApi);
                return;
            }
            var order = await _order.ПолучитьOrderByMarketplaceId(market.Id, Id.ToString());
            if (order != null && ((order.Recipient == null) || (order.Address == null)))
            {
                using var tran = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _order.ОбновитьПолучателяЗаказаАдрес(order.Id, LastName, FirstName, MiddleName, Recipient, Phone,
                        PostCode, Country, City, Subway, Street, House, Block, Entrance, Entryphone, Floor, Apartment, CustomerNotes);
                    _context.ОбновитьСетевуюАктивность();
                    if (_context.Database.CurrentTransaction != null)
                        tran.Commit();
                }
                catch (DbUpdateException db_ex)
                {
                    if (_context.Database.CurrentTransaction != null)
                        _context.Database.CurrentTransaction.Rollback();
                    _logger.LogError(db_ex.InnerException.ToString());
                }
                catch (Exception ex)
                {
                    if (_context.Database.CurrentTransaction != null)
                        _context.Database.CurrentTransaction.Rollback();
                    _logger.LogError(ex.Message);
                }
            }
        }
        public async Task<List<string>> ПолучитьLockedНоменклатураIds(string authorizationApi, List<string> списокКодовНоменклатуры)
        {
            return await _marketplace.GetLockedMarketplaceCatalogEntries(authorizationApi, списокКодовНоменклатуры);
        }
        public async Task<List<string>> ПолучитьСкладIdОстатковMarketplace()
        {
            return await _склад.ПолучитьСкладIdОстатковMarketplace();
        }
        public async Task<Dictionary<string, decimal>> ПолучитьКвант(List<string> списокКодовНоменклатуры, CancellationToken cancellationToken)
        {
            //return await _marketplace.GetQuantumInfo(marketId, списокКодовНоменклатуры, cancellationToken);
            return await _номенклатура.ПолучитьКвант(списокКодовНоменклатуры, cancellationToken);
        }
        public async Task<Dictionary<string, decimal>> ПолучитьDeltaStock(string marketId, List<string> списокКодовНоменклатуры, CancellationToken cancellationToken)
        {
            return await _marketplace.GetDeltaStockInfo(marketId, списокКодовНоменклатуры, cancellationToken);
        }
        public async Task SetElectronicAcceptanceCertificateCode(string authorizationApi, long Id, string code, CancellationToken cancellationToken)
        {
            var defFirma = _configuration["Settings:Firma"];
            string _defFirmaId = _configuration["Settings:" + defFirma + ":FirmaId"];
            var market = await _marketplace.ПолучитьMarketplaceByFirma(authorizationApi, _defFirmaId);
            if (market == null)
            {
                _logger.LogError("Не обнаружен маркетплейс для фирмы " + _defFirmaId + " и authApi " + authorizationApi);
                return;
            }
            var order = await _order.ПолучитьOrderByMarketplaceId(market.Id, Id.ToString());
            if ((order != null) && (order.DeliveryServiceName != code))
                await _order.RefreshOrderDeliveryServiceId(order.Id, long.Parse(order.DeliveryServiceId), code, cancellationToken);
        }
        public async Task<IDictionary<string, decimal>> ПолучитьРезервМаркета(string marketId, IEnumerable<string> nomIds)
        {
            return await _номенклатура.GetReserveByMarketplace(marketId, nomIds);
        }
    }
}
