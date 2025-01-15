using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AliExpressClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StinClasses;
using StinClasses.Models;
using StinClasses.Документы;
using StinClasses.Справочники;

namespace Refresher1C.Service
{
    class DocCreateOrUpdate: IDocCreateOrUpdate
    {
        private readonly StinDbContext _context;
        private readonly ILogger<DocCreateOrUpdate> _logger;

        private IOrder _order;
        private IMarketplace _marketplace;
        private IСклад _склад;
        private IФирма _фирма;
        private IНоменклатура _номенклатура;
        private IГрафикМаршрутов _графикМаршрутов;

        private IПродажаКасса _продажаКасса;
        private IОплатаЧерезЮКасса _оплатаЧерезЮКасса;
        private IПКО _пко;
        private IПредварительнаяЗаявка _предварительнаяЗаявка;
        private IЗаявкаПокупателя _заявкаПокупателя;
        private IСпрос _спрос;
        private IНабор _набор;
        private IПеремещениеТМЦ _перемещениеТМЦ;
        private IОтменаЗаявок _отменаЗаявок;
        private IОтменаНабора _отменаНабора;
        private IВозвратИзДоставки _возвратИзДоставки;
        private IКомплекснаяПродажа _комплекснаяПродажа;
        private IРеализация _реализация;
        private IСчетФактура _счетФактура;
        private IОтчетКомиссионера _отчетКомиссионера;


        private protected bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _order.Dispose();
                    _marketplace.Dispose();
                    _склад.Dispose();
                    _фирма.Dispose();
                    _номенклатура.Dispose();
                    _графикМаршрутов.Dispose();
                    _продажаКасса.Dispose();
                    _оплатаЧерезЮКасса.Dispose();
                    _пко.Dispose();
                    _предварительнаяЗаявка.Dispose();
                    _заявкаПокупателя.Dispose();
                    _спрос.Dispose();
                    _набор.Dispose();
                    _перемещениеТМЦ.Dispose();
                    _отменаЗаявок.Dispose();
                    _отменаНабора.Dispose();
                    _возвратИзДоставки.Dispose();
                    _комплекснаяПродажа.Dispose();
                    _реализация.Dispose();
                    _счетФактура.Dispose();
                    _отчетКомиссионера.Dispose();
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
        public DocCreateOrUpdate(ILogger<DocCreateOrUpdate> logger, StinDbContext context)
        {
            _logger = logger;
            _context = context;
            _order = new OrderEntity(context);
            _marketplace = new MarketplaceEntity(context);
            _склад = new СкладEntity(context);
            _фирма = new ФирмаEntity(context);
            _номенклатура = new НоменклатураEntity(context);
            _графикМаршрутов = new ГрафикМаршрутовEntity(context);
            _пко = new ПКО(context);
            _продажаКасса = new ПродажаКасса(context);
            _оплатаЧерезЮКасса = new ОплатаЧерезЮКасса(context);
            _предварительнаяЗаявка = new ПредварительнаяЗаявка(context);
            _заявкаПокупателя = new ЗаявкаПокупателя(context);
            _спрос = new Спрос(context);
            _набор = new Набор(context);
            _перемещениеТМЦ = new ПеремещениеТМЦ(context);
            _отменаЗаявок = new ОтменаЗаявок(context);
            _отменаНабора = new ОтменаНабора(context);
            _возвратИзДоставки = new ВозвратИзДоставки(context);
            _комплекснаяПродажа = new КомплекснаяПродажа(context);
            _реализация = new Реализация(context);
            _счетФактура = new СчетФактура(context);
            _отчетКомиссионера = new ОтчетКомиссионера(context);
        }
        public async Task CompleteSuccessPaymentAsync(string idDoc, string status, CancellationToken stoppingToken)
        {
            using var tran = await _context.Database.BeginTransactionAsync();
            try
            {
                if (!_оплатаЧерезЮКасса.NeedToOpenPeriod())
                {
                    var ФормаОплатаЮКасса = await _оплатаЧерезЮКасса.GetФормаОплатаЧерезЮКассаById(idDoc);
                    ФормаОплатаЮКасса.СостояниеПлатежа = status;
                    var result = await _оплатаЧерезЮКасса.ОбновитьСтатусAsync(ФормаОплатаЮКасса);
                    if (result != null)
                    {
                        if (_context.Database.CurrentTransaction != null)
                            tran.Rollback();
                        _logger.LogError(result.Description);
                    }
                    else
                    {
                        if (ФормаОплатаЮКасса.СостояниеПлатежа == "   AOY   ")
                        {
                            var ФормаПко = await _пко.ВводНаОснованииAsync(ФормаОплатаЮКасса.Общие.ДокОснование.IdDoc, Common.UserRobot);
                            if (ФормаПко.Ошибка == null || ФормаПко.Ошибка.Skip)
                            {
                                result = await _пко.ЗаписатьПровестиAsync(ФормаПко, ФормаОплатаЮКасса);
                                if (result != null)
                                {
                                    if (_context.Database.CurrentTransaction != null)
                                        tran.Rollback();
                                    _logger.LogError(result.Description);
                                }
                            }
                            else
                            {
                                if (_context.Database.CurrentTransaction != null)
                                    tran.Rollback();
                                _logger.LogError(ФормаПко.Ошибка != null ? ФормаПко.Ошибка.Description : "Ошибка ввода на основании ПКО");
                            }
                            //var ФормаПродажаКасса = await _продажаКасса.ВводНаОснованииAsync(ФормаОплатаЮКасса.Общие.ДокОснование.IdDoc, Common.UserRobot);
                            //if (ФормаПродажаКасса.Ошибка == null || ФормаПродажаКасса.Ошибка.Skip)
                            //{
                            //    result = await _продажаКасса.ЗаписатьПровестиAsync(ФормаПродажаКасса, ФормаОплатаЮКасса);
                            //    if (result != null)
                            //    {
                            //        if (_context.Database.CurrentTransaction != null)
                            //            tran.Rollback();
                            //        _logger.LogError(result.Description);
                            //    }
                            //}
                            //else
                            //{
                            //    if (_context.Database.CurrentTransaction != null)
                            //        tran.Rollback();
                            //    _logger.LogError(ФормаПродажаКасса.Ошибка != null ? ФормаПродажаКасса.Ошибка.Description : "Ошибка ввода на основании ПродажаКасса");
                            //}
                        }
                    }
                }
                else
                {
                    if (_context.Database.CurrentTransaction != null)
                        _context.Database.CurrentTransaction.Rollback();
                    _logger.LogError("Период не открыт");
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
        public async Task CreateNabor(string заявкаId, CancellationToken stoppingToken)
        {
            var реквизитыПроведенныхДокументов = new List<ОбщиеРеквизиты>();
            using var tran = await _context.Database.BeginTransactionAsync();
            try
            {
                var формаСчет = await _заявкаПокупателя.GetФормаЗаявкаById(заявкаId);
                var ПереченьФормаНабор = await _набор.ВводНаОснованииAsync(формаСчет, DateTime.Now);
                ExceptionData result = null;
                foreach (var формаНабор in ПереченьФормаНабор)
                {
                    result = await _набор.ЗаписатьПровестиAsync(формаНабор);
                    реквизитыПроведенныхДокументов.Add(формаНабор.Общие);
                    if (result != null)
                    {
                        if (_context.Database.CurrentTransaction != null)
                            tran.Rollback();
                        _logger.LogError(result.Description);
                        break;
                    }
                }
                if (result == null)
                {
                    if (реквизитыПроведенныхДокументов.Count > 0)
                        await _заявкаПокупателя.ОбновитьАктивность(реквизитыПроведенныхДокументов);
                    else
                        await _заявкаПокупателя.ОбновитьСетевуюАктивность();
                }
                if (_context.Database.CurrentTransaction != null)
                    tran.Commit();
            }
            catch (Exception ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                _logger.LogError(ex.Message);
            }
        }
        public async Task NewOrder(string тип, string defFirmaId, string customerId, string dogovorId, string authApi, EncodeVersion encoding,
            string складОтгрузкиId, string notes,
            string deliveryServiceId, string deliveryServiceName,
            StinDeliveryPartnerType partnerType, StinDeliveryType deliveryType, double deliveryPrice, double deliverySubsidy,
            StinPaymentType paymentType, StinPaymentMethod paymentMethod,
            string regionId, string regionName, OrderRecipientAddress address,
            string shipmentId, string postingNumber, DateTime shipmentDate, List<OrderItem> items, CancellationToken cancellationToken)
        {
            var разрешенныеФирмы = await _фирма.ПолучитьСписокРазрешенныхФирмAsync(defFirmaId);
            var market = await _marketplace.ПолучитьMarketplaceByFirma(authApi, defFirmaId);
            List<string> списокСкладовНаличияТовара = null;
            if (!string.IsNullOrEmpty(market.СкладId))
                списокСкладовНаличияТовара = new List<string> { market.СкладId };
            else
                списокСкладовНаличияТовара = await _склад.ПолучитьСкладIdОстатковMarketplace();
            var номенклатураCodes = items.Select(x => x.НоменклатураId).ToList();
            var НоменклатураList = await _номенклатура.ПолучитьСвободныеОстатки(
                разрешенныеФирмы, 
                списокСкладовНаличияТовара,
                номенклатураCodes, 
                true);
            var nomQuantums = await _номенклатура.ПолучитьКвант(номенклатураCodes, cancellationToken);
            items.ForEach(x => 
            { 
                x.ИдентификаторПоставщика = тип == "YANDEX" ? x.ИдентификаторПоставщика : "";
                x.ИдентификаторСклада = тип == "YANDEX" ? x.ИдентификаторСклада : "";
                x.ИдентификаторСкладаПартнера = тип == "YANDEX" ? x.ИдентификаторСкладаПартнера : "";
                x.НоменклатураId = НоменклатураList.Where(y => y.Code == x.НоменклатураId).Select(z => z.Id).FirstOrDefault();
                var quantum = (int)nomQuantums.Where(q => q.Key == x.НоменклатураId).Select(q => q.Value).FirstOrDefault();
                if (quantum == 0)
                    quantum = 1;
                //if (тип == "YANDEX")
                //    quantum = 1;
                x.Количество = x.Количество * quantum;
                x.Цена = x.Цена / quantum;
                x.ЦенаСоСкидкой = x.ЦенаСоСкидкой / quantum;
                x.Вознаграждение = x.Вознаграждение / quantum;
            });
            DateTime dateTimeTA = _context.GetDateTimeTA();
            bool needToCalcDateTime = dateTimeTA.Month != DateTime.Now.Month;
            var реквизитыПроведенныхДокументов = new List<ОбщиеРеквизиты>();
            using var tran = await _context.Database.BeginTransactionAsync();
            try
            {
                string marketplaceName = "";
                switch (тип)
                {
                    case "OZON":
                        marketplaceName = "Ozon FBS";
                        break;
                    case "ALIEXPRESS":
                        marketplaceName = "Aliexpress FBS";
                        break;
                    case "SBER":
                        marketplaceName = "Sber FBS";
                        break;
                    case "WILDBERRIES":
                        marketplaceName = "Wildberries FBS";
                        break;
                    case "YANDEX":
                        marketplaceName = "Yandex ";
                        if (partnerType == StinDeliveryPartnerType.SHOP)
                            marketplaceName += "DBS";
                        else
                            marketplaceName += "FBS";
                        break;
                    default:
                        marketplaceName = "Ozon FBS";
                        break;
                }
                var new_order = await _order.НовыйOrder(defFirmaId, encoding, authApi,
                    marketplaceName, postingNumber, paymentType, paymentMethod, partnerType, deliveryType,
                    deliveryServiceId, deliveryServiceName, deliveryPrice, deliverySubsidy, shipmentId, shipmentDate,
                    regionId, regionName, address, notes, items);
                if (new_order == null)
                {
                    if (_context.Database.CurrentTransaction != null)
                        tran.Rollback();
                    _logger.LogError("Ошибка создания заказа");
                    return;
                }

                var формаПредварительнаяЗаявка = await _предварительнаяЗаявка.НовыйДокумент(
                         needToCalcDateTime ? dateTimeTA.AddMilliseconds(1) : DateTime.Now,
                         defFirmaId,
                         string.IsNullOrEmpty(складОтгрузкиId) ? Common.SkladEkran : складОтгрузкиId,
                         customerId,
                         dogovorId,
                         Common.ТипЦенРозничный,
                         new_order, НоменклатураList, address);
                var result = await _предварительнаяЗаявка.ЗаписатьAsync(формаПредварительнаяЗаявка);
                if (result != null)
                {
                    if (_context.Database.CurrentTransaction != null)
                        tran.Rollback();
                    _logger.LogError(result.Description);
                }
                else
                {
                    string orderNo = формаПредварительнаяЗаявка.Общие.НомерДок + "-" + формаПредварительнаяЗаявка.Общие.ДатаДок.ToString("yyyy");
                    формаПредварительнаяЗаявка.Order.OrderNo = orderNo;
                    if (тип == "YANDEX")
                        await _order.ОбновитьOrderNo(формаПредварительнаяЗаявка.Order.Id, orderNo);
                    else
                        await _order.ОбновитьOrderNoAndStatus(формаПредварительнаяЗаявка.Order.Id, orderNo, 8);
                    result = await _предварительнаяЗаявка.ПровестиAsync(формаПредварительнаяЗаявка);
                    if (result != null)
                    {
                        if (_context.Database.CurrentTransaction != null)
                            tran.Rollback();
                        _logger.LogError(result.Description);
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
            catch (Exception ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                _logger.LogError(ex.Message);
            }
        }
        public async Task OrderFromTransferDeliveried(Order order)
        {
            DateTime dateTimeTA = _context.GetDateTimeTA();
            bool needToCalcDateTime = dateTimeTA.Month != DateTime.Now.Month;
            var списокКомплексПродаж = await _комплекснаяПродажа.GetФормаКомплекснаяПродажаByOrderId(order.Id);
            if (списокКомплексПродаж?.Count > 0)
            {
                var реквизитыПроведенныхДокументов = new List<ОбщиеРеквизиты>();
                using var tran = await _context.Database.BeginTransactionAsync();
                try
                {
                    ExceptionData result = null;
                    foreach (var формаКомплекснаяПродажа in списокКомплексПродаж)
                    {
                        var формаОтчетКомиссионера = await _отчетКомиссионера.ЗаполнитьНаОснованииAsync(формаКомплекснаяПродажа, needToCalcDateTime ? dateTimeTA.AddMilliseconds(1) : DateTime.Now);
                        if (формаОтчетКомиссионера != null)
                        {
                            result = await _отчетКомиссионера.ЗаписатьПровестиAsync(формаОтчетКомиссионера);
                            if (result == null)
                            {
                                реквизитыПроведенныхДокументов.Add(формаОтчетКомиссионера.Общие);
                                //if (!формаОтчетКомиссионера.Общие.Фирма.НетСчетФактуры)
                                //{
                                //    var формаСчетФактура = await _счетФактура.ВводНаОснованииAsync(формаОтчетКомиссионера, needToCalcDateTime ? dateTimeTA.AddMilliseconds(2) : DateTime.Now);
                                //    result = await _счетФактура.ЗаписатьПровестиAsync(формаСчетФактура);
                                //    реквизитыПроведенныхДокументов.Add(формаСчетФактура.Общие);
                                //}
                            }
                        }
                        if (result != null)
                        {
                            if (_context.Database.CurrentTransaction != null)
                                tran.Rollback();
                            _logger.LogError(result.Description);
                            break;
                        }
                    }

                    if (_context.Database.CurrentTransaction != null)
                    {
                        await _order.ОбновитьOrderStatus(order.Id, 6);
                        if (реквизитыПроведенныхДокументов.Count > 0)
                            await _предварительнаяЗаявка.ОбновитьАктивность(реквизитыПроведенныхДокументов);
                        else
                            await _предварительнаяЗаявка.ОбновитьСетевуюАктивность();
                    }
                    if (_context.Database.CurrentTransaction != null)
                        tran.Commit();
                }
                catch (Exception ex)
                {
                    if (_context.Database.CurrentTransaction != null)
                        _context.Database.CurrentTransaction.Rollback();
                    _logger.LogError(ex.Message);
                }
            }
        }
        public async Task OrderDeliveried(Order order, bool transferred = false)
        {
            DateTime dateTimeTA = _context.GetDateTimeTA();
            bool needToCalcDateTime = dateTimeTA.Month != DateTime.Now.Month;
            var реквизитыПроведенныхДокументов = new List<ОбщиеРеквизиты>();
            using var tran = await _context.Database.BeginTransactionAsync();
            try
            {
                var активныеНаборы = await _набор.ПолучитьСписокАктивныхНаборов(order.Id, true);
                if (активныеНаборы != null && активныеНаборы.Count > 0)
                {
                    ExceptionData result = null;
                    var формаПредварительнаяЗаявка = await _предварительнаяЗаявка.GetФормаПредварительнаяЗаявкаByOrderId(order.Id);
                    var списокКомплеснаяПродажа = await _комплекснаяПродажа.ЗаполнитьНаОснованииAsync(Common.UserRobot, формаПредварительнаяЗаявка, needToCalcDateTime ? dateTimeTA.AddMilliseconds(1) : DateTime.Now, активныеНаборы, transferred);
                    foreach (var формаКомплеснаяПродажа in списокКомплеснаяПродажа)
                    {
                        result = await _комплекснаяПродажа.ЗаписатьПровестиAsync(формаКомплеснаяПродажа);
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
                                if (result != null)
                                    break;
                            }
                        }
                        if (result != null)
                        {
                            if (_context.Database.CurrentTransaction != null)
                                tran.Rollback();
                            _logger.LogError(result.Description);
                            break;
                        }
                    }
                    if (result == null)
                        await _order.ОбновитьOrderStatus(order.Id, transferred ? 14 : 6);
                }
                else
                    await _order.ОбновитьOrderStatus(order.Id, transferred ? -14 : -6);
                if (_context.Database.CurrentTransaction != null)
                {
                    if (реквизитыПроведенныхДокументов.Count > 0)
                        await _предварительнаяЗаявка.ОбновитьАктивность(реквизитыПроведенныхДокументов);
                    else
                        await _предварительнаяЗаявка.ОбновитьСетевуюАктивность();
                }
                if (_context.Database.CurrentTransaction != null)
                    tran.Commit();
            }
            catch (Exception ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                _logger.LogError(ex.Message);
            }
        }
        public async Task OrderCancelled(Order order)
        {
            DateTime dateTimeTA = _context.GetDateTimeTA();
            bool needToCalcDateTime = dateTimeTA.Month != DateTime.Now.Month;
            var реквизитыПроведенныхДокументов = new List<ОбщиеРеквизиты>();
            using var tran = await _context.Database.BeginTransactionAsync();
            try
            {
                var активныеСчета = await _заявкаПокупателя.ПолучитьСписокАктивныхСчетов(order.Id, true);
                if (_context.Database.CurrentTransaction != null)
                {
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
                }
                if (_context.Database.CurrentTransaction != null)
                {
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
                }
                if (_context.Database.CurrentTransaction != null)
                {
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
                }
                if (_context.Database.CurrentTransaction != null)
                {
                    await _order.ОбновитьOrderStatus(order.Id, 5);
                    if (реквизитыПроведенныхДокументов.Count > 0)
                        await _предварительнаяЗаявка.ОбновитьАктивность(реквизитыПроведенныхДокументов);
                    else
                        await _предварительнаяЗаявка.ОбновитьСетевуюАктивность();
                }
                if (_context.Database.CurrentTransaction != null)
                    tran.Commit();
            }
            catch (Exception ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                _logger.LogError(ex.Message);
            }
        }
        public async Task ОбновитьНомерМаршрута(Order order)
        {
            string маршрутКод = _графикМаршрутов.ПолучитьКодМаршрута(order.ShipmentDate, "50");
            using var tran = await _context.Database.BeginTransactionAsync();
            try
            { 
                await _предварительнаяЗаявка.ОбновитьНомерМаршрута(order.Id, маршрутКод);
                var активныеСчета = await _заявкаПокупателя.ПолучитьСписокАктивныхСчетов(order.Id, true);
                if (активныеСчета != null && активныеСчета.Count > 0)
                {
                    foreach (var формаЗаявкаПокупателя in активныеСчета.Where(x => (x.Маршрут != null) && (x.Маршрут.Наименование != маршрутКод)))
                    {
                        await _заявкаПокупателя.ОбновитьНомерМаршрута(формаЗаявкаПокупателя, маршрутКод);
                    }
                }
                var активныеНаборы = await _набор.ПолучитьСписокАктивныхНаборов(order.Id, false);
                if (активныеНаборы != null && активныеНаборы.Count > 0)
                {
                    foreach (var формаНабор in активныеНаборы.Where(x => (x.Маршрут != null) && (x.Маршрут.Наименование != маршрутКод)))
                    {
                        await _набор.ОбновитьНомерМаршрута(формаНабор, маршрутКод);
                    }
                }
                if (_context.Database.CurrentTransaction != null)
                    tran.Commit();
            }
            catch (Exception ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                _logger.LogError(ex.Message);
            }
        }
    }
}
