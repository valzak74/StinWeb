using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StinClasses;
using StinClasses.Models;
using StinClasses.Документы;
using StinClasses.Справочники;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HttpExtensions;
using System.Data;
using System.IO;
using System.Collections;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using StinClasses.MarketCommission;
using System.Globalization;
using AliExpressClasses;
using Newtonsoft.Json.Linq;

namespace Refresher1C.Service
{
    class MarketplaceService : IMarketplaceService
    {
        private readonly StinDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MarketplaceService> _logger;
        private IHttpService _httpService;
        private IDocCreateOrUpdate _docService;
        IServiceProvider _serviceProvider;

        private readonly Dictionary<string, string> _firmProxy;
        private readonly List<SleepPeriod> _sleepPeriods;

        private IOrder _order;
        private IMarketplace _marketplace;
        private IФирма _фирма;
        private IНоменклатура _номенклатура;
        private IСклад _склад;

        private IЗаявкаПокупателя _заявкаПокупателя;
        private IНабор _набор;
        private IОтменаНабора _отменаНабора;

        private protected bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _order.Dispose();
                    _marketplace.Dispose();
                    _фирма.Dispose();
                    _номенклатура.Dispose();
                    _склад.Dispose();
                    _заявкаПокупателя.Dispose();
                    _набор.Dispose();
                    _отменаНабора.Dispose();
                    _context.Dispose();
                    _firmProxy.Clear();
                }
            }
            this.disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public MarketplaceService(IConfiguration configuration, ILogger<MarketplaceService> logger, IDocCreateOrUpdate docService, StinDbContext context, IHttpService httpService,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _docService = docService;
            _serviceProvider = serviceProvider;
            _httpService = httpService;
            _context = context;
            _configuration = configuration;
            _marketplace = new MarketplaceEntity(context);
            _order = new OrderEntity(context);
            _фирма = new ФирмаEntity(context);
            _номенклатура = new НоменклатураEntity(context);
            _склад = new СкладEntity(context);
            _заявкаПокупателя = new ЗаявкаПокупателя(context);
            _набор = new Набор(context);
            _отменаНабора = new ОтменаНабора(context);

            _firmProxy = new Dictionary<string, string>();
            foreach (var item in _configuration.GetSection("CommonSettings:FirmData").GetChildren())
            {
                var configData = item.AsEnumerable();
                var firmaId = configData.FirstOrDefault(x => x.Key.EndsWith("FirmaId")).Value;
                var proxy = configData.FirstOrDefault(x => x.Key.EndsWith("Proxy")).Value;
                _firmProxy.Add(firmaId, proxy);
            }
            var sleepPeriods = _configuration["Orderer:sleepPeriods"].Split(';').Select(x => x.Split('-'));
            _sleepPeriods = new List<SleepPeriod>();
            foreach (var period in sleepPeriods)
            {
                if ((period.Length == 2) &&
                    TimeSpan.TryParseExact(period[0], @"hh\:mm", null, out TimeSpan startTimeSpan) &&
                    TimeSpan.TryParseExact(period[1], @"hh\:mm", null, out TimeSpan endTimeSpan))
                    _sleepPeriods.Add(new SleepPeriod(startTimeSpan, endTimeSpan));
            }
        }
        public async Task CheckNaborNeeded(CancellationToken stoppingToken)
        {
            if (!_заявкаПокупателя.NeedToOpenPeriod())
            {
                DateTime dateRegTA = _context.GetRegTA();
                string видОперацииЗаявкаОдобренная = Common.ВидыОперации.FirstOrDefault(x => x.Value == "Заявка (одобренная)").Key;
                string видОперацииСчетНаОплату = Common.ВидыОперации.FirstOrDefault(x => x.Value == "Счет на оплату").Key;
                var остаткиРегЗаявки = from docT in _context.Dt2457s
                                       join nom in _context.Sc84s on docT.Sp2446 equals nom.Id
                                       join doc in _context.Dh2457s on docT.Iddoc equals doc.Iddoc
                                       join order in _context.Sc13994s on doc.Sp13995 equals order.Id
                                       where
                                           (nom.Sp2417 == ВидыНоменклатуры.Товар) &&
                                           (order.Sp13982 == 8) && //order статус = 8
                                           ((doc.Sp4760 == видОперацииЗаявкаОдобренная) || (doc.Sp4760 == видОперацииСчетНаОплату))
                                           //&& order.Code.Trim() == "172517024"
                                       group new { doc, docT } by new { ЗаявкаId = doc.Iddoc, OrderId = doc.Sp13995, НоменклатураId = docT.Sp2446, Коэффициент = docT.Sp2449 } into gr
                                       select new
                                       {
                                           ЗаявкаId = gr.Key.ЗаявкаId,
                                           OrderId = gr.Key.OrderId,
                                           НоменклатураId = gr.Key.НоменклатураId,
                                           КолДокумента = gr.Sum(x => x.docT.Sp2447) * gr.Key.Коэффициент,
                                       } into docData
                                       join r in _context.Rg4674s.Where(x => x.Period == dateRegTA) on new { IdDoc = docData.ЗаявкаId, НоменклатураId = docData.НоменклатураId } equals new { IdDoc = r.Sp4671, НоменклатураId = r.Sp4669 }
                                       group new { docData, r } by new { docData.ЗаявкаId, docData.OrderId, docData.НоменклатураId, docData.КолДокумента } into gr
                                       select new
                                       {
                                           ЗаявкаId = gr.Key.ЗаявкаId,
                                           OrderId = gr.Key.OrderId,
                                           НоменклатураId = gr.Key.НоменклатураId,
                                           КолДокумента = gr.Key.КолДокумента,
                                           КолДоступно = gr.Sum(x => x.r.Sp4672)
                                       };
                var notReadyЗаявкиIds = остаткиРегЗаявки.Where(x => x.КолДокумента != x.КолДоступно).Select(x => new { x.ЗаявкаId, x.OrderId }).Distinct();
                var readyЗаявкиIds = await остаткиРегЗаявки.Select(x => new { x.ЗаявкаId, x.OrderId }).Distinct().Except(notReadyЗаявкиIds).ToListAsync();

                foreach (var data in readyЗаявкиIds)
                    await _docService.CreateNabor(data.ЗаявкаId, stoppingToken);
            }
        }
        public async Task PrepareYandexFbsBoxes(bool regular, CancellationToken stoppingToken)
        {
            using var tran = await _context.Database.BeginTransactionAsync(stoppingToken);
            try
            {
                DateTime dateRegTA = _context.GetRegTA();
                var data = await (from r in _context.Rg14021s
                                  join order in _context.Sc13994s on r.Sp14010 equals order.Id
                                  join market in _context.Sc14042s on order.Sp14038 equals market.Id
                                  join nom in _context.Sc84s on r.Sp14012 equals nom.Id
                                  join ed in _context.Sc75s on nom.Sp94 equals ed.Id
                                  join item in _context.Sc14033s on new { orderId = order.Id, nomId = nom.Id }  equals new { orderId = item.Parentext, nomId = item.Sp14022 }
                                  //join markUse in _context.Sc14152s.Where(x => !x.Ismark) on new { NomId = nom.Id, MarketId = market.Id } equals new { NomId = markUse.Parentext, MarketId = markUse.Sp14147 } into _markUse
                                  //from markUse in _markUse.DefaultIfEmpty()
                                  where r.Period == dateRegTA && ((r.Sp14011 == 10) || (r.Sp14011 == 11)) &&
                                    (regular ? (order.Sp13982 == 8) : (order.Sp13982 == -1)) //order статус = 8
                                    //order.Code.Trim() == "741303769"
                                    && (((StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.YANDEX_MARKET) ||
                                        ((StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.SBER_MEGA_MARKET) ||
                                        ((StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.ALIEXPRESS_LOGISTIC) ||
                                        ((StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.WILDBERRIES) ||
                                        ((StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.OZON_LOGISTIC))
                                          //&& market.Code.Trim() == "18795"
                                          //&& order.Code.Trim() == "172517024"
                                  group new { r, order, market, nom, ed, item } by new
                                  {
                                      OrderId = order.Id,
                                      MarketplaceId = order.Code,
                                      OrderOurId = order.Sp13981,
                                      ShipmentId = order.Sp13989,
                                      FirmaId = market.Parentext,
                                      Тип = market.Sp14155,
                                      CampaignId = market.Code,
                                      ClientId = market.Sp14053,
                                      AuthToken = market.Sp14054,
                                      НоменклатураId = nom.Id,
                                      КоэффициентЕдиницы = ed.Sp78,
                                      КолМест = ed.Sp14063,
                                      Квант = nom.Sp14188,
                                      Encode = market.Sp14153,
                                      ItemIndex = item.Code,
                                      Sku = nom.Code
                                  } into gr
                                  where (gr.Sum(x => x.r.Sp14017) != 0)
                                  select new
                                  {
                                      OrderId = gr.Key.OrderId,
                                      MarketplaceId = gr.Key.MarketplaceId.Trim(),
                                      OrderOurId = gr.Key.OrderOurId.Trim(),
                                      ShipmentId = gr.Key.ShipmentId.Trim(),
                                      FirmaId = gr.Key.FirmaId,
                                      Тип = gr.Key.Тип.ToUpper().Trim(),
                                      CampaignId = gr.Key.CampaignId.Trim(),
                                      ClientId = gr.Key.ClientId.Trim(),
                                      AuthToken = gr.Key.AuthToken.Trim(),
                                      НоменклатураId = gr.Key.НоменклатураId,
                                      КолМест = gr.Key.КолМест == 0 ? 1 : gr.Key.КолМест,
                                      Количество = gr.Sum(x => x.r.Sp14017) / gr.Key.КоэффициентЕдиницы,
                                      Квант = gr.Key.Квант,
                                      ItemIndex = gr.Key.ItemIndex.Trim(),
                                      Sku = gr.Key.Sku.Encode((EncodeVersion)gr.Key.Encode)
                                  })
                       .ToListAsync(stoppingToken);
                if ((data != null) && (data.Count > 0))
                {
                    var dataGrouped = data.GroupBy(x => new
                    {
                        x.OrderId,
                        x.MarketplaceId,
                        x.OrderOurId,
                        x.ShipmentId,
                        x.FirmaId,
                        x.Тип,
                        x.CampaignId,
                        x.ClientId,
                        x.AuthToken,
                    });
                    foreach (var obj in dataGrouped)
                    {
                        var entity = await _context.Sc13994s.OrderBy(x => x.RowId).FirstOrDefaultAsync(x => x.Id == obj.Key.OrderId);
                        if (entity != null)
                        {
                            decimal status = 1;
                            string err = "";
                            if (obj.Key.Тип == "ЯНДЕКС")
                            {
                                var request = new YandexClasses.BoxesRequest();
                                int boxCount = 0;
                                foreach (var d in obj)
                                {
                                    var мест = (int)(d.Количество * d.КолМест);
                                    if (d.Квант > 1)
                                    {
                                        мест = (int)(d.Количество / d.Квант);
                                        if ((d.Количество % d.Квант) > 0)
                                            мест++;
                                    }
                                    for (int i = 1; i <= мест; i++)
                                    {
                                        boxCount++;
                                        request.Boxes.Add(new YandexClasses.Box
                                        {
                                            FulfilmentId = obj.Key.MarketplaceId + "-" + boxCount.ToString(),
                                        });
                                    }
                                }
                                var proxy = _firmProxy[obj.Key.FirmaId];
                                var campaignId = obj.Key.CampaignId;
                                var orderId = obj.Key.MarketplaceId;
                                var shipmentId = obj.Key.ShipmentId;
                                var result = await YandexClasses.YandexOperators.Exchange<YandexClasses.BoxesResponse>(_httpService,
                                    $"https://{proxy}api.partner.market.yandex.ru/v2/campaigns/{campaignId}/orders/{orderId}/delivery/shipments/{shipmentId}/boxes.json",
                                    HttpMethod.Put,
                                    obj.Key.ClientId,
                                    obj.Key.AuthToken,
                                    request,
                                    stoppingToken);
                                if (result.Item1 == YandexClasses.ResponseStatus.ERROR)
                                {
                                    if (!string.IsNullOrEmpty(result.Item3) && !result.Item3.Contains("INTERNAL_SERVER_ERROR"))
                                    {
                                        //записать ошибку
                                        status = -1;
                                        err = result.Item3;
                                    }
                                }
                                else
                                {
                                    if ((result.Item1 == YandexClasses.ResponseStatus.OK) && (result.Item2 != null))
                                        try
                                        {
                                            if (result.Item2.Status != YandexClasses.ResponseStatus.OK)
                                            {
                                                status = -1;
                                                err = "Internal : Boxes response ERROR status";
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            status = -1;
                                            err = "Internal (PrepareYandexFbsBoxes) : " + ex.Message;
                                        }
                                }
                            }
                            else if (obj.Key.Тип == "SBER")
                            {
                                var items = new List<KeyValuePair<string, string>>();
                                foreach (var item in obj)
                                {
                                    items.Add(new(item.ItemIndex, item.Sku));
                                }
                                var result = await SberClasses.Functions.OrderConfirm(_httpService,
                                    _firmProxy[obj.Key.FirmaId],
                                    obj.Key.AuthToken,
                                    obj.Key.MarketplaceId,
                                    obj.Key.OrderOurId,
                                    items,
                                    stoppingToken);
                                if (!string.IsNullOrEmpty(result.error))
                                    err = result.error;
                                if (!result.success)
                                    status = -1;
                            }
                            else if (obj.Key.Тип == "WILDBERRIES")
                            {
                                var supplyIdResult = await GetWbActiveSupplyId(_firmProxy[obj.Key.FirmaId], obj.Key.AuthToken, stoppingToken);
                                if (!supplyIdResult.success)
                                    status = -1;
                                else if (!string.IsNullOrEmpty(supplyIdResult.supplyId)) 
                                {
                                    var addToSupplyResult = await WbClasses.Functions.AddToSupply(_httpService, _firmProxy[obj.Key.FirmaId], obj.Key.AuthToken, supplyIdResult.supplyId, obj.Key.MarketplaceId, stoppingToken);
                                    if (!string.IsNullOrEmpty(addToSupplyResult.error))
                                        err = addToSupplyResult.error;
                                    if (!addToSupplyResult.success)
                                        status = -1;
                                    else
                                        entity.Sp13987 = supplyIdResult.supplyId; //DeliveryServiceName
                                }
                            }

                            entity.Sp13982 = status;
                            if (!string.IsNullOrEmpty(err))
                                entity.Sp14055 = err;

                            _context.Update(entity);
                            _context.РегистрацияИзмененийРаспределеннойИБ(13994, entity.Id);
                        }

                    }
                    _context.ОбновитьСетевуюАктивность();
                    await _context.SaveChangesAsync(stoppingToken);
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
        public async Task PrepareFbsLabels(bool regular, CancellationToken stoppingToken)
        {
            using var tran = await _context.Database.BeginTransactionAsync(stoppingToken);
            try
            {
                var data = await (from order in _context.Sc13994s
                                  join market in _context.Sc14042s on order.Sp14038 equals market.Id
                                  join binary in _context.VzOrderBinaries.Where(x => x.Extension.Trim().ToUpper() == "LABELS") on order.Id equals binary.Id into _binary
                                  from binary in _binary.DefaultIfEmpty()
                                  where //order.Code.Trim() == "2301258054266676" &&
                                        (regular ? ((order.Sp13982 == 1) || (order.Sp13982 == 2) || (order.Sp13982 == 3)) : (order.Sp13982 == -2)) && //order статус = 1 (грузовые места сформированы)
                                        (((StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.YANDEX_MARKET) ||
                                        ((StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.SBER_MEGA_MARKET) ||
                                        ((StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.ALIEXPRESS_LOGISTIC) ||
                                        ((StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.WILDBERRIES) ||
                                        ((StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.OZON_LOGISTIC)) &&
                                        (binary == null)
                                        //&& market.Code.Trim() == "18795"
                                        //&& order.Code.Trim() == "172517024"
                                  group new { order, market } by new { order.Id, order.Code, firmaId = market.Parentext, campaignId = market.Code, clientId = market.Sp14053, token = market.Sp14054, тип = market.Sp14155 } into gr
                                  select new
                                  {
                                      OrderId = gr.Key.Id,
                                      MarketplaceId = gr.Key.Code.Trim(),
                                      FirmaId = gr.Key.firmaId,
                                      CampaignId = gr.Key.campaignId.Trim(),
                                      ClientId = gr.Key.clientId.Trim(),
                                      AuthToken = gr.Key.token.Trim(),
                                      Тип = gr.Key.тип.ToUpper().Trim()
                                  }).ToListAsync(stoppingToken);
                if ((data != null) && (data.Count() > 0))
                {
                    foreach (var order in data)
                    {
                        var entity = await _context.Sc13994s.OrderBy(x => x.RowId).FirstOrDefaultAsync(x => x.Id == order.OrderId, stoppingToken);
                        if (entity != null)
                        {
                            byte[] label = null;
                            decimal status = 2;
                            string err = "";
                            if (order.Тип == "ЯНДЕКС")
                            {
                                var proxyHost = _firmProxy[order.FirmaId];
                                var campaignId = order.CampaignId;
                                var orderId = order.MarketplaceId;
                                var result = await YandexClasses.YandexOperators.Exchange<byte[]>(_httpService,
                                    $"https://{proxyHost}api.partner.market.yandex.ru/v2/campaigns/{campaignId}/orders/{orderId}/delivery/labels.json",
                                    HttpMethod.Get,
                                    order.ClientId,
                                    order.AuthToken,
                                    null, 
                                    stoppingToken);
                                if ((result.Item1 == YandexClasses.ResponseStatus.OK) && (result.Item2 != null))
                                    label = result.Item2;
                                else if (!string.IsNullOrEmpty(result.Item3) && !result.Item3.Contains("INTERNAL_SERVER_ERROR"))
                                {
                                    status = -2;
                                    err = result.Item3;
                                }
                            }
                            else if (order.Тип == "SBER")
                            {
                                var orderEntry = await _order.ПолучитьOrderWithItems(order.OrderId);
                                List<KeyValuePair<string, int>> items = orderEntry.Items
                                    .Select(x => new KeyValuePair<string, int>(x.Id, (int)x.КолМест))
                                    .ToList();
                                var result = await SberClasses.Functions.StickerPrint(_httpService, _firmProxy[order.FirmaId], orderEntry.CampaignId, orderEntry.AuthToken,
                                    orderEntry.MarketplaceId, orderEntry.OrderNo, items,
                                    stoppingToken);
                                if (result.pdf != null)
                                {
                                    label = result.pdf;
                                }
                            }
                            else if (order.Тип == "OZON")
                            {
                                TimeSpan sleepPeriod = TimeSpan.FromSeconds(5);
                                int tryCount = 5;
                                while (true)
                                {
                                    var result = await OzonClasses.OzonOperators.GetLabels(_httpService, _firmProxy[order.FirmaId], order.ClientId, order.AuthToken,
                                        new List<string> { order.MarketplaceId },
                                        stoppingToken);
                                    if (result.Item1 != null)
                                    {
                                        label = result.Item1;
                                        var resultDetails = await OzonClasses.OzonOperators.OrderDetails(_httpService, _firmProxy[order.FirmaId], order.ClientId, order.AuthToken,
                                            order.MarketplaceId,
                                            stoppingToken);
                                        if (resultDetails.Item2 != null)
                                            _logger.LogError(resultDetails.Item2);
                                        if (resultDetails.Item3 != null)
                                        {
                                            if (!string.IsNullOrEmpty(resultDetails.Item3.Upper_barcode))
                                                entity.Sp13987 = resultDetails.Item3.Upper_barcode;
                                            if (!string.IsNullOrEmpty(resultDetails.Item3.Lower_barcode))
                                                entity.Sp13992 = resultDetails.Item3.Lower_barcode;
                                        }
                                        break;
                                    }
                                    if (result.Item2 != null && !string.IsNullOrEmpty(result.Item2) && (--tryCount == 0))
                                    {
                                        if ((result.Item2 == "3: POSTINGS_NOT_READY") || (result.Item2 == "3: INVALID_ARGUMENT"))
                                            status = entity.Sp13982;
                                        else
                                        {
                                            _logger.LogError(result.Item2);
                                            status = -2;
                                            err = result.Item2;
                                        }
                                        break;
                                    }
                                    await Task.Delay(sleepPeriod);
                                }
                            }
                            else if (order.Тип == "ALIEXPRESS")
                            {
                                var orderEntry = await _order.ПолучитьOrder(order.OrderId);
                                List<long> logisticsOrderIds = orderEntry.DeliveryServiceId.Split(", ").Select(x => Convert.ToInt64(x)).ToList();
                                var result = await AliExpressClasses.Functions.GetLabels(_httpService, _firmProxy[order.FirmaId], orderEntry.AuthToken,
                                    logisticsOrderIds,
                                    stoppingToken);
                                if (result.pdf != null)
                                {
                                    label = result.pdf;
                                    if (string.IsNullOrEmpty(orderEntry.DeliveryServiceName))
                                    {
                                        var updateTrackingNumberResult = await AliExpressClasses.Functions.GetLogisticsOrders(_httpService, _firmProxy[order.FirmaId], orderEntry.AuthToken,
                                            logisticsOrderIds, 
                                            stoppingToken);
                                        if (updateTrackingNumberResult.logisticsOrderTrackingNumbers?.Count > 0)
                                        {
                                            var firstLogisticsOrder = updateTrackingNumberResult.logisticsOrderTrackingNumbers.FirstOrDefault();
                                            await _order.RefreshOrderDeliveryServiceId(orderEntry.Id, firstLogisticsOrder.Key, firstLogisticsOrder.Value, stoppingToken);
                                        }
                                    }
                                }
                                else
                                    status = entity.Sp13982;
                            }
                            else if (order.Тип == "WILDBERRIES")
                            {
                                var result = await WbClasses.Functions.GetLabel(_httpService, _firmProxy[order.FirmaId], order.AuthToken,
                                    new List<long> { Convert.ToInt64(order.MarketplaceId) },
                                    stoppingToken);
                                if (result.png != null)
                                    label = PdfHelper.PdfFunctions.Instance.GetPdfFromImage(result.png.ResizeImage(58,40), 0, 0);
                                if (!string.IsNullOrEmpty(result.barcode))
                                    entity.Sp13992 = result.barcode;
                                if (result.partA.HasValue && result.partB.HasValue)
                                {
                                    entity.Sp13986 = result.partA.Value;
                                    entity.Sp13991 = result.partB.Value;
                                }
                            }
                            if (!string.IsNullOrEmpty(err))
                                entity.Sp14055 = err;
                            if (((status > 0) && ((entity.Sp13982 == 1) || (entity.Sp13982 == -2))) || ((status < 0) && (entity.Sp13982 > 0)))
                                entity.Sp13982 = status;
                            _context.Update(entity);
                            _context.РегистрацияИзмененийРаспределеннойИБ(13994, entity.Id);
                            if (label != null)
                            {
                                var orderBinary = await _context.VzOrderBinaries
                                    .OrderBy(x => x.RowId)
                                    .FirstOrDefaultAsync(x => (x.Id == order.OrderId) && (x.Extension.Trim().ToUpper() == "LABELS"));
                                if (orderBinary == null)
                                {
                                    orderBinary = new VzOrderBinary
                                    {
                                        Id = order.OrderId,
                                        Extension = "LABELS",
                                        Binary = label
                                    };
                                    await _context.VzOrderBinaries.AddAsync(orderBinary);
                                }
                                else
                                {
                                    orderBinary.Binary = label;
                                    _context.Update(orderBinary);
                                }
                            }
                        }
                    }
                    _context.ОбновитьСетевуюАктивность();
                    await _context.SaveChangesAsync();
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
        public async Task RefreshBuyerInfo(CancellationToken stoppingToken)
        {
            var dbsData = await (from order in _context.Sc13994s
                                 join market in _context.Sc14042s on order.Sp14038 equals market.Id
                                 where !order.Ismark &&
                                       ((order.Sp13982 != 5) || (order.Sp13982 != 6)) && //order статус
                                       ((StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.SHOP) &&
                                       string.IsNullOrWhiteSpace(order.Sp14120)
                                 select new
                                 {
                                     OrderId = order.Id,
                                     MarketplaceOrderId = order.Code.Trim(),
                                     FirmaId = market.Parentext,
                                     Тип = market.Sp14155.ToUpper().Trim(),
                                     CampaignId = market.Code.Trim(),
                                     ClientId = market.Sp14053.Trim(),
                                     AuthToken = market.Sp14054.Trim(),
                                 })
                          .ToListAsync(stoppingToken);
            if ((dbsData != null) && (dbsData.Count > 0))
                foreach (var row in dbsData)
                {
                    var order = await _order.ПолучитьOrder(row.OrderId);
                    if (order != null)
                    {
                        if (row.Тип == "ЯНДЕКС")
                        {
                            var result = await YandexClasses.YandexOperators.BuyerDetails(_httpService, _firmProxy[row.FirmaId], row.CampaignId, row.ClientId, row.AuthToken, 
                                row.MarketplaceOrderId, 
                                stoppingToken);
                            if (result != null)
                                await _order.ОбновитьПолучателяЗаказаАдрес(order.Id, 
                                    result.LastName, result.FirstName, result.MiddleName, "", result.Phone, 
                                    "", "", "", "", "", "", "", "", "", "", "", "");
                        }
                    }
                }
        }
        public async Task ChangeOrderStatus(CancellationToken stoppingToken)
        {
            bool needSaveChanges = false;
            using var tran = await _context.Database.BeginTransactionAsync(stoppingToken);
            try
            {
                DateTime dateRegTA = _context.GetRegTA();
                //установить статус PROCESSING / READY_TO_SHIP для Yandex FBS
                var fbsData = await (from order in _context.Sc13994s
                                     join items in _context.Sc14033s on order.Id equals items.Parentext
                                     join nom in _context.Sc84s on items.Sp14022 equals nom.Id
                                     join ed in _context.Sc75s on nom.Sp94 equals ed.Id
                                     where ((order.Sp13982 == 1) || (order.Sp13982 == 2)) && //order статус = 1 или 2 (грузовые места сформированы, лейблы скачены)
                                       (((StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.YANDEX_MARKET) ||
                                       ((StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.SBER_MEGA_MARKET) ||
                                       ((StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.ALIEXPRESS_LOGISTIC) ||
                                       ((StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.WILDBERRIES) ||
                                       ((StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.OZON_LOGISTIC))
                                       //&& order.Code.Trim() == "158496222"
                                     group new { order, items, ed } by new { OrderId = order.Id, order.Code, MarketId = order.Sp14038, edK = ed.Sp78 } into grOrder
                                     select new
                                     {
                                         grOrder.Key.OrderId,
                                         grOrder.Key.Code,
                                         grOrder.Key.MarketId,
                                         //grOrder.Key.НоменклатураId,
                                         ItemsCount = grOrder.Sum(x => x.items.Sp14023 * (grOrder.Key.edK == 0 ? 1 : grOrder.Key.edK))
                                     } into orderData
                                     join market in _context.Sc14042s on orderData.MarketId equals market.Id
                                     //join nom in _context.Sc84s on orderData.НоменклатураId equals nom.Id
                                     //join ed in _context.Sc75s on nom.Sp94 equals ed.Id
                                     join r in _context.Rg14021s on orderData.OrderId equals r.Sp14010 into _r
                                     from r in _r.DefaultIfEmpty()
                                     where (r.Period == dateRegTA) && (r.Sp14011 == 11) 
                                       //&& market.Code.Trim() == "18795"
                                     group new { orderData, market, r } by new { orderData.OrderId, orderData.Code, orderData.ItemsCount, firmaId = market.Parentext, тип = market.Sp14155, campaignId = market.Code, clientId = market.Sp14053, token = market.Sp14054 } into gr
                                     where gr.Key.ItemsCount == gr.Sum(x => x.r.Sp14017)
                                     select new
                                     {
                                         OrderId = gr.Key.OrderId,
                                         MarketplaceId = gr.Key.Code.Trim(),
                                         FirmaId = gr.Key.firmaId,
                                         Тип = gr.Key.тип.ToUpper().Trim(),
                                         CampaignId = gr.Key.campaignId.Trim(),
                                         ClientId = gr.Key.clientId.Trim(),
                                         AuthToken = gr.Key.token.Trim(),
                                     })
                       .Distinct()
                       .ToListAsync(stoppingToken);
                if (fbsData?.Count > 0)
                    foreach (var row in fbsData)
                    {
                        Order order;
                        if (row.Тип == "SBER")
                            order = await _order.ПолучитьOrderWithItems(row.OrderId);
                        else
                            order = await _order.ПолучитьOrder(row.OrderId);
                        if ((order != null) && (order.Status == (int)StinOrderStatus.PROCESSING) && (order.SubStatus == (int)StinOrderSubStatus.READY_TO_SHIP))
                        {
                            needSaveChanges = true;
                            if (row.Тип == "ЯНДЕКС")
                            {
                                var statusResult = await YandexClasses.YandexOperators.OrderDetails(_httpService, _firmProxy[row.FirmaId], row.CampaignId, row.ClientId, row.AuthToken, row.MarketplaceId, stoppingToken);
                                if ((statusResult.Item1 == YandexClasses.StatusYandex.PROCESSING) &&
                                    (statusResult.Item2 == YandexClasses.SubStatusYandex.READY_TO_SHIP))
                                {
                                    await _order.ОбновитьOrderStatus(order.Id, 3);
                                }
                                else if (statusResult.Item1 == YandexClasses.StatusYandex.CANCELLED)
                                {
                                    await _docService.OrderCancelled(order);
                                }
                                else
                                {
                                    var result = await YandexClasses.YandexOperators.ChangeStatus(_httpService, _firmProxy[row.FirmaId],
                                        order.CampaignId, order.MarketplaceId,
                                        order.ClientId, order.AuthToken,
                                        YandexClasses.StatusYandex.PROCESSING, YandexClasses.SubStatusYandex.READY_TO_SHIP,
                                        (YandexClasses.DeliveryType)order.DeliveryType,
                                        stoppingToken);
                                    if ((result != null) && result.Item1)
                                        await _order.ОбновитьOrderStatus(order.Id, 3);
                                    else
                                        await _order.ОбновитьOrderStatus(order.Id, -3, result.Item2);
                                }
                            }
                            else if (row.Тип == "SBER")
                            {
                                List<KeyValuePair<string, int>> items = new List<KeyValuePair<string, int>>();
                                foreach (var item in order.Items)
                                {
                                    items.Add(new(item.Id, (int)item.КолМест));
                                }
                                var result = await SberClasses.Functions.OrderPicking(_httpService, _firmProxy[row.FirmaId],
                                    order.CampaignId,
                                    order.AuthToken,
                                    order.MarketplaceId,
                                    order.OrderNo,
                                    items,
                                    stoppingToken);
                                if (result.success)
                                    await _order.ОбновитьOrderStatus(order.Id, 3);
                                else
                                    await _order.ОбновитьOrderStatus(order.Id, -3, result.error);
                            }
                            else if ((row.Тип == "OZON") || (row.Тип == "ALIEXPRESS") || (row.Тип == "WILDBERRIES"))
                                await _order.ОбновитьOrderStatus(order.Id, 3);
                        }
                    }
                //установить статус DELIVERY или PICKUP для Yandex DBS
                var dbsData = await (from items in _context.Sc14033s
                                     join order in _context.Sc13994s on items.Parentext equals order.Id
                                     join market in _context.Sc14042s on order.Sp14038 equals market.Id
                                     join nom in _context.Sc84s on items.Sp14022 equals nom.Id
                                     join ed in _context.Sc75s on nom.Sp94 equals ed.Id
                                     join r in _context.Rg14021s on new { OrderId = order.Id, НоменклатураId = items.Sp14022 } equals new { OrderId = r.Sp14010, НоменклатураId = r.Sp14012 } into _r
                                     from r in _r.DefaultIfEmpty()
                                     where !items.Ismark &&
                                       order.Sp13982 == 8 && //order статус = 8
                                       (r != null ? (r.Period == dateRegTA && r.Sp14011 == 11 && r.Sp14017 > 0) : true) &&
                                       (StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.SHOP
                                     group new { order, items, market, ed, r } by new
                                     {
                                         OrderId = items.Parentext,
                                         DeliveryType = (StinDeliveryType)order.Sp13988,
                                         НоменклатураId = items.Sp14022,
                                         Коэффициент = ed.Sp78,
                                         MarketplaceId = order.Code,
                                         FirmaId = market.Parentext,
                                         CampaignId = market.Code,
                                         ClientId = market.Sp14053,
                                         Token = market.Sp14054
                                     } into gr
                                     where gr.Sum(x => x.items.Sp14023) == (gr.Where(d => d.r.Sp14011 == 11).Sum(x => x.r.Sp14017) / (gr.Key.Коэффициент == 0 ? 1 : gr.Key.Коэффициент))
                                     select new
                                     {
                                         OrderId = gr.Key.OrderId,
                                         DeliveryType = gr.Key.DeliveryType,
                                         MarketplaceId = gr.Key.MarketplaceId.Trim(),
                                         FirmaId = gr.Key.FirmaId,
                                         CampaignId = gr.Key.CampaignId.Trim(),
                                         ClientId = gr.Key.ClientId.Trim(),
                                         AuthToken = gr.Key.Token.Trim(),
                                         НоменклатураId = gr.Key.НоменклатураId,
                                     })
                        .Distinct()
                        .ToListAsync(stoppingToken);
                if ((dbsData != null) && (dbsData.Count > 0))
                    foreach (var row in dbsData)
                    {
                        var order = await _order.ПолучитьOrder(row.OrderId);
                        if ((order != null) && (order.Status == (int)StinOrderStatus.PROCESSING) && (order.SubStatus == (int)StinOrderSubStatus.READY_TO_SHIP))
                        {
                            needSaveChanges = true;
                            var statusResult = await YandexClasses.YandexOperators.OrderDetails(_httpService, _firmProxy[row.FirmaId], row.CampaignId, row.ClientId, row.AuthToken, row.MarketplaceId, stoppingToken);
                            if ((statusResult.Item1 == YandexClasses.StatusYandex.DELIVERY) &&
                                (statusResult.Item2 == YandexClasses.SubStatusYandex.DELIVERY_SERVICE_RECEIVED))
                            {
                                if (order.DeliveryType == StinDeliveryType.PICKUP)
                                {
                                    var result = await YandexClasses.YandexOperators.ChangeStatus(_httpService, _firmProxy[row.FirmaId],
                                        order.CampaignId, order.MarketplaceId,
                                        order.ClientId, order.AuthToken,
                                        YandexClasses.StatusYandex.PICKUP, YandexClasses.SubStatusYandex.NotFound,
                                        (YandexClasses.DeliveryType)order.DeliveryType,
                                        stoppingToken);
                                    if ((result != null) && result.Item1)
                                        await _order.ОбновитьOrderStatus(order.Id, 9);
                                    else
                                        await _order.ОбновитьOrderStatus(order.Id, -9, result.Item2);
                                }
                                else
                                    await _order.ОбновитьOrderStatus(order.Id, 9);
                            }
                            else if ((statusResult.Item1 == YandexClasses.StatusYandex.PICKUP) &&
                                (statusResult.Item2 == YandexClasses.SubStatusYandex.PICKUP_SERVICE_RECEIVED))
                            {
                                await _order.ОбновитьOrderStatus(order.Id, 9);
                            }
                            else if (statusResult.Item1 == YandexClasses.StatusYandex.CANCELLED)
                            {
                                await _docService.OrderCancelled(order);
                            }
                            else
                            {
                                var result = await YandexClasses.YandexOperators.ChangeStatus(_httpService, _firmProxy[row.FirmaId],
                                    order.CampaignId, order.MarketplaceId,
                                    order.ClientId, order.AuthToken,
                                    YandexClasses.StatusYandex.DELIVERY, YandexClasses.SubStatusYandex.NotFound,
                                    (YandexClasses.DeliveryType)order.DeliveryType,
                                    stoppingToken);
                                if ((result != null) && result.Item1)
                                {
                                    if (order.DeliveryType == StinDeliveryType.PICKUP)
                                    {
                                        result = await YandexClasses.YandexOperators.ChangeStatus(_httpService, _firmProxy[row.FirmaId],
                                            order.CampaignId, order.MarketplaceId,
                                            order.ClientId, order.AuthToken,
                                            YandexClasses.StatusYandex.PICKUP, YandexClasses.SubStatusYandex.NotFound,
                                            (YandexClasses.DeliveryType)order.DeliveryType,
                                            stoppingToken);
                                    }
                                    if (result.Item1)
                                        await _order.ОбновитьOrderStatus(order.Id, 9);
                                    else
                                        await _order.ОбновитьOrderStatus(order.Id, -9, result.Item2);
                                }
                                else
                                    await _order.ОбновитьOrderStatus(order.Id, -9, result.Item2);
                            }
                        }
                    }

                if (needSaveChanges)
                {
                    _context.ОбновитьСетевуюАктивность();
                    await _context.SaveChangesAsync(stoppingToken);
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
        public async Task CheckPickupExpired(CancellationToken stoppingToken)
        {
            using var tran = await _context.Database.BeginTransactionAsync();
            try
            {
                if (!_отменаНабора.NeedToOpenPeriod())
                {
                    DateTime dateRegTA = _context.GetRegTA();
                    DateTime limitDate = DateTime.Today.AddDays(-3);
                    var data = await (from r in _context.Rg11973s
                                      join d in _context.Dh11948s on r.Sp11970 equals d.Iddoc
                                      join order in _context.Sc13994s on d.Sp14003 equals order.Id
                                      join market in _context.Sc14042s on order.Sp14038 equals market.Id
                                      where r.Period == dateRegTA &&
                                        order.Sp13988 == (decimal)StinDeliveryType.PICKUP &&
                                        ((order.Sp13990 < limitDate) || (order.Sp13982 == 7)) &&
                                        (StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.SHOP
                                      group new { r, order, market } by new
                                      {
                                          OrderId = order.Id,
                                          FirmaId = market.Parentext, 
                                          НаборId = r.Sp11970,
                                          //CampaignId = market.Code,
                                          //ClientId = market.Sp14053,
                                          //Token = market.Sp14054
                                      } into gr
                                      where gr.Sum(x => x.r.Sp11972) != 0
                                      select new
                                      {
                                          OrderId = gr.Key.OrderId,
                                          FirmaId = gr.Key.FirmaId,
                                          НаборId = gr.Key.НаборId,
                                          //CampaignId = gr.Key.CampaignId.Trim(),
                                          //ClientId = gr.Key.ClientId.Trim(),
                                          //AuthToken = gr.Key.Token.Trim(),
                                      })
                                      .ToListAsync();
                    var реквизитыПроведенныхДокументов = new List<ОбщиеРеквизиты>();
                    foreach (var groupedData in data.GroupBy(x => new { x.OrderId, x.FirmaId }))
                    {
                        var order = await _order.ПолучитьOrder(groupedData.Key.OrderId);
                        var orderDetails = await YandexClasses.YandexOperators.OrderDetailsFull(_httpService, _firmProxy[groupedData.Key.FirmaId],
                            order.CampaignId, order.ClientId, order.AuthToken, order.MarketplaceId, stoppingToken);
                        var expiredDate = orderDetails?.Order?.Delivery?.OutletStorageLimitDate;
                        if (expiredDate.HasValue && (expiredDate.Value < DateTime.Today))
                        {
                            var yandexResult = await YandexClasses.YandexOperators.ChangeStatus(_httpService, _firmProxy[groupedData.Key.FirmaId],
                            order.CampaignId, order.MarketplaceId,
                            order.ClientId, order.AuthToken,
                            YandexClasses.StatusYandex.CANCELLED, order.InternalStatus == 7 ? YandexClasses.SubStatusYandex.USER_CHANGED_MIND : YandexClasses.SubStatusYandex.PICKUP_EXPIRED,
                            (YandexClasses.DeliveryType)order.DeliveryType,
                                stoppingToken);
                            if (yandexResult.Item1)
                                await _order.ОбновитьOrderStatus(order.Id, 5);
                            else
                                await _order.ОбновитьOrderStatus(order.Id, -5, yandexResult.Item2);
                            if (yandexResult.Item1)
                            {
                                ExceptionData result = null;
                                foreach (var наборId in groupedData.Select(x => x.НаборId))
                                {
                                    var набор = await _набор.GetФормаНаборById(наборId);
                                    var формаОтменаНабора = await _отменаНабора.ВводНаОснованииAsync(набор, DateTime.Now);
                                    result = await _отменаНабора.ЗаписатьПровестиAsync(формаОтменаНабора);
                                    реквизитыПроведенныхДокументов.Add(формаОтменаНабора.Общие);
                                    if (result != null)
                                        break;
                                }
                                if (result != null)
                                {
                                    if (_context.Database.CurrentTransaction != null)
                                        _context.Database.CurrentTransaction.Rollback();
                                    break;
                                }
                            }
                        }
                    }
                    if (реквизитыПроведенныхДокументов.Count > 0)
                        await _отменаНабора.ОбновитьАктивность(реквизитыПроведенныхДокументов);
                }
                else
                {
                    if (_context.Database.CurrentTransaction != null)
                        _context.Database.CurrentTransaction.Rollback();
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
        private decimal GetPriceMarketplace(decimal ЦенаРозн, decimal ЦенаСп, decimal ЦенаЗакуп, decimal CheckCoeff, decimal ЦенаФикс, decimal Коэф, decimal Multiplayer, decimal DeltaPrice)
        {
            var Цена = ЦенаСп > 0 ? Math.Min(ЦенаСп, ЦенаРозн) : ЦенаРозн;
            if (ЦенаФикс > 0)
            {
                if (ЦенаФикс >= Цена)
                    Цена = ЦенаФикс;
                else
                {
                    var Порог = ЦенаЗакуп * (Коэф > 0 ? Коэф : (Multiplayer > 0 ? Multiplayer : CheckCoeff));
                    if (Порог > ЦенаФикс)
                    {
                        //удалить ЦенаФикс из markUsing ???
                        //entry.Sp14148 = 0;
                    }
                    else
                    {
                        Цена = ЦенаФикс;
                    }
                }
            }
            else if (DeltaPrice != 0)
            {
                var Порог = ЦенаЗакуп * (Коэф > 0 ? Коэф : (Multiplayer > 0 ? Multiplayer : CheckCoeff));
                var calcPrice = Цена * (100 + DeltaPrice) / 100;
                if (calcPrice >= Порог)
                    Цена = calcPrice;
            }
            return Цена;
        }
        public async Task UpdatePrices(CancellationToken stoppingToken)
        {
            if (int.TryParse(_configuration["Pricer:maxPerRequest"], out int maxPerRequest))
                maxPerRequest = Math.Max(maxPerRequest, 1);
            else
                maxPerRequest = 50;
            if (decimal.TryParse(_configuration["Pricer:checkCoefficient"], out decimal checkCoeff))
                checkCoeff = Math.Max(checkCoeff, 1);
            else
                checkCoeff = 1.2m;
            DateTime limitDate = DateTime.Today.AddDays(-20);
            try
            {
                var marketplaceIds = await (from marketUsing in _context.Sc14152s
                                            join market in _context.Sc14042s on marketUsing.Sp14147 equals market.Id
                                            join updPrice in _context.VzUpdatingPrices on marketUsing.Id equals updPrice.MuId
                                            where !marketUsing.Ismark && 
                                                (marketUsing.Sp14158 == 1) && //Есть в каталоге
                                                (updPrice.Flag || (updPrice.Updated < limitDate))
                                                //&& (market.Code.Trim() == "3530297616")
                                                //&& (market.Code.Trim() == "43956")
                                                //&& (market.Sp14155.Trim() == "Wildberries")
                                            select new
                                            {
                                                Id = market.Id,
                                                FirmaId = market.Parentext,
                                                Тип = market.Sp14155.Trim(),
                                                Наименование = market.Descr.Trim(),
                                                CampaignId = market.Code.Trim(),
                                                ClientId = market.Sp14053.Trim(),
                                                AuthToken = market.Sp14054.Trim(),
                                                AuthSecret = market.Sp14195.Trim(),
                                                Authorization = market.Sp14077.Trim(),
                                                FeedId = market.Sp14154.Trim(),
                                                Encoding = (EncodeVersion)market.Sp14153,
                                                Multiplayer = market.Sp14165
                                            })
                                            .Distinct()
                                            .ToListAsync();
                List<string> uploadIds = new List<string>();
                var uploadData = Enumerable.Repeat(new
                {
                    FirmaId = "",
                    Тип = "",
                    CampaignId = "",
                    ClientId = "",
                    AuthToken = "",
                    AuthSecret = "",
                    Authorization = "",
                    FeedId = (long)0,
                    Квант = 0m,
                    Id = "",
                    Код = "",
                    ProductId = "",
                    Цена = 0.00m,
                    ЦенаДоСкидки = 0.00m,
                }, 0).ToList();

                foreach (var marketplace in marketplaceIds)
                {
                    var data = (from marketUsing in _context.Sc14152s
                                join nom in _context.Sc84s on marketUsing.Parentext equals nom.Id
                                join vzTovar in _context.VzTovars on nom.Id equals vzTovar.Id into _vzTovar
                                from vzTovar in _vzTovar.DefaultIfEmpty()
                                join updPrice in _context.VzUpdatingPrices on marketUsing.Id equals updPrice.MuId
                                where !marketUsing.Ismark &&
                                    (marketUsing.Sp14158 == 1) && //Есть в каталоге
                                    (updPrice.Flag || (updPrice.Updated < limitDate)) &&
                                    (marketUsing.Sp14147 == marketplace.Id)
                                    //&& ((vzTovar == null) || (vzTovar.Rozn <= 0))
                                    //&& nom.Code == "K00035471"
                                    //&& nom.Code == "D00040383"
                                select new
                                {
                                    Id = marketUsing.Id,
                                    NomCode = nom.Code,
                                    //NomArt = nom.Sp85.Trim(),
                                    ProductId = marketUsing.Sp14190.Trim(),
                                    Квант = nom.Sp14188,
                                    DeltaPrice = marketUsing.Sp14213,
                                    ЦенаРозн = vzTovar != null ? vzTovar.Rozn ?? 0 : 0,
                                    ЦенаСп = vzTovar != null ? vzTovar.RoznSp ?? 0 : 0,
                                    ЦенаЗакуп = vzTovar != null ? vzTovar.Zakup ?? 0 : 0,
                                    ЦенаФикс = marketUsing.Sp14148,
                                    Коэф = marketUsing.Sp14149,
                                })
                                .OrderBy(x => x.NomCode)
                                .Take(maxPerRequest);
                                //.ToList();
                    foreach (var d in data)
                    {
                        var Код = d.NomCode.Encode(marketplace.Encoding);
                        var Цена = GetPriceMarketplace(d.ЦенаРозн, d.ЦенаСп, d.ЦенаЗакуп, checkCoeff, d.ЦенаФикс, d.Коэф, marketplace.Multiplayer, d.DeltaPrice);
                        if (long.TryParse(marketplace.FeedId, out long feedId))
                            feedId = 0;
                        if (Цена > 0)
                            uploadData.Add(new
                            {
                                FirmaId = marketplace.FirmaId,
                                Тип = marketplace.Тип.ToUpper(),
                                CampaignId = marketplace.CampaignId,
                                ClientId = marketplace.ClientId,
                                AuthToken = marketplace.AuthToken,
                                AuthSecret = marketplace.AuthSecret,
                                Authorization = marketplace.Authorization,
                                FeedId = ((marketplace.FeedId.Length > 0) && (feedId > 0)) ? feedId : 0,
                                Квант = d.Квант == 0 ? 1 : d.Квант,
                                Id = d.Id,
                                Код = Код,
                                ProductId = d.ProductId,
                                Цена = Цена,
                                ЦенаДоСкидки = Цена < d.ЦенаРозн ? d.ЦенаРозн : 0.00m,
                            });
                        else
                            _logger.LogError("UpdatePrice : code " + Код + " цена не превышает 0");
                    }
                }
                foreach (var priceData in uploadData.GroupBy(x => new { x.FirmaId, x.Тип, x.CampaignId, x.ClientId, x.AuthToken, x.AuthSecret, x.Authorization, x.FeedId }))
                {
                    uploadIds.Clear();
                    if (priceData.Key.Тип == "ЯНДЕКС")
                    {
                        var request = new YandexClasses.PriceUpdateRequest();
                        foreach (var data in priceData)
                        {
                            request.Offers.Add(new YandexClasses.PriceOffer
                            {
                                Feed = data.FeedId > 0 ? new YandexClasses.PriceFeed { Id = data.FeedId } : null,
                                Id = data.Код,
                                Delete = false,
                                Price = new YandexClasses.PriceElement
                                {
                                    CurrencyId = YandexClasses.CurrencyType.RUR,
                                    Value = data.Цена,
                                    Vat = YandexClasses.PriceVatType.vat_not_valid
                                }
                            });
                            uploadIds.Add(data.Id);
                        }
                        var result = await YandexClasses.YandexOperators.Exchange<YandexClasses.ErrorResponse>(_httpService,
                            $"https://{_firmProxy[priceData.Key.FirmaId]}api.partner.market.yandex.ru/v2/campaigns/{priceData.Key.CampaignId}/offer-prices/updates.json",
                            HttpMethod.Post,
                            priceData.Key.ClientId,
                            priceData.Key.AuthToken,
                            request,
                            stoppingToken);
                        if ((result.Item1 == YandexClasses.ResponseStatus.ERROR) || 
                            (result.Item2?.Status == YandexClasses.ResponseStatus.ERROR))
                        {
                            if (_context.Database.CurrentTransaction != null)
                                _context.Database.CurrentTransaction.Rollback();
                            if (!string.IsNullOrEmpty(result.Item3))
                                _logger.LogError(result.Item3);
                            uploadIds.Clear();
                        }
                    }
                    else if (priceData.Key.Тип == "OZON")
                    {
                        var result = await OzonClasses.OzonOperators.UpdatePrice(_httpService, _firmProxy[priceData.Key.FirmaId], priceData.Key.ClientId, priceData.Key.AuthToken,
                            priceData.Select(x =>
                            {
                                var oldPrice = x.ЦенаДоСкидки * x.Квант;
                                var price = x.Цена * x.Квант;
                                if ((oldPrice > 400) && (oldPrice <= 10000))
                                {
                                    if ((oldPrice - price) <= (oldPrice / 20))
                                        oldPrice = price;
                                }
                                else if (oldPrice > 10000)
                                {
                                    if ((oldPrice - price) <= 500)
                                        oldPrice = price;
                                }
                                return new OzonClasses.PriceRequest
                                {
                                    Offer_id = x.Код,
                                    Old_price = oldPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                                    Price = price.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                                };
                            }).ToList(),
                            stoppingToken);
                        if (result.Item1 != null)
                        {
                            uploadIds.AddRange(priceData.Where(x => result.Item1.Contains(x.Код)).Select(x => x.Id));
                            if (!string.IsNullOrEmpty(result.Item2))
                                _logger.LogError(result.Item2);
                        }
                        else
                        {
                            if (_context.Database.CurrentTransaction != null)
                                _context.Database.CurrentTransaction.Rollback();
                            if (!string.IsNullOrEmpty(result.Item2))
                                _logger.LogError(result.Item2);
                            uploadIds.Clear();
                        }
                    }
                    else if (priceData.Key.Тип == "SBER")
                    {
                        var result = await SberClasses.Functions.UpdatePrice(_httpService, _firmProxy[priceData.Key.FirmaId], priceData.Key.AuthToken,
                            priceData.Select(x => new
                            {
                                Offer_id = x.Код,
                                Price = (int)(x.Цена * x.Квант)
                            }).ToDictionary(k => k.Offer_id, v => v.Price),
                           stoppingToken);
                        if (result.success)
                        {
                            uploadIds.AddRange(priceData.Select(x => x.Id));
                            if (!string.IsNullOrEmpty(result.error))
                                _logger.LogError(result.error);
                        }
                        else
                        {
                            if (_context.Database.CurrentTransaction != null)
                                _context.Database.CurrentTransaction.Rollback();
                            if (!string.IsNullOrEmpty(result.error))
                                _logger.LogError(result.error);
                            uploadIds.Clear();
                        }
                    }
                    else if (priceData.Key.Тип == "ALIEXPRESS")
                    {
                        //global API
                        //var result = await AliExpressClasses.Functions.UpdatePriceGlobal(_httpService,
                        //    priceData.Key.ClientId, priceData.Key.AuthSecret, priceData.Key.Authorization,
                        //    priceData.Select(x =>
                        //    {
                        //        if (!long.TryParse(x.ProductId, out long productId))
                        //            productId = 0;
                        //        return new AliExpressClasses.PriceProductGlobal
                        //        {
                        //            Product_id = productId,
                        //            Multiple_sku_update_list = new List<AliExpressClasses.PriceSku>
                        //            {
                        //                new AliExpressClasses.PriceSku
                        //                {
                        //                    Sku_code = x.Код,
                        //                    Discount_price = (x.Квант * x.Цена).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                        //                    Price = x.ЦенаДоСкидки > 0 ? (x.Квант * x.ЦенаДоСкидки).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) : (x.Квант * x.Цена).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                        //                }
                        //            }
                        //        };
                        //    }).ToList(),
                        //    stoppingToken);
                        //if (result.Item1 != null)
                        //{
                        //    var stringData = result.Item1.Select(x => x.ToString()).ToList();
                        //    uploadIds.AddRange(priceData.Where(x => stringData.Contains(x.ProductId)).Select(x => x.Id));
                        //    if (!string.IsNullOrEmpty(result.Item2))
                        //        _logger.LogError(result.Item2);
                        //}
                        //else
                        //{
                        //    if (_context.Database.CurrentTransaction != null)
                        //        _context.Database.CurrentTransaction.Rollback();
                        //    _logger.LogError(result.Item2);
                        //    uploadIds.Clear();
                        //}

                        //local API
                        var result = await AliExpressClasses.Functions.UpdatePrice(_httpService, _firmProxy[priceData.Key.FirmaId], priceData.Key.AuthToken,
                            priceData.Select(x => new AliExpressClasses.PriceProduct
                            {

                                Product_id = x.ProductId,
                                Skus = new List<AliExpressClasses.PriceSku>
                                {
                                    new AliExpressClasses.PriceSku
                                    {
                                        Sku_code = x.Код,
                                        Discount_price = (x.Квант * x.Цена).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                                        Price = (x.Квант * (x.ЦенаДоСкидки > 0 ? x.ЦенаДоСкидки : x.Цена)).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                                    }
                                }
                            }).ToList(),
                            stoppingToken);
                        if (result.UpdatedIds != null)
                        {
                            uploadIds.AddRange(priceData.Where(x => result.UpdatedIds.Contains(x.ProductId)).Select(x => x.Id));
                            if (!string.IsNullOrEmpty(result.ErrorMessage))
                                _logger.LogError(result.ErrorMessage);
                        }
                        else
                        {
                            if (_context.Database.CurrentTransaction != null)
                                _context.Database.CurrentTransaction.Rollback();
                            if (!string.IsNullOrEmpty(result.ErrorMessage))
                                _logger.LogError(result.ErrorMessage);
                            uploadIds.Clear();
                        }
                    }
                    else if (priceData.Key.Тип == "WILDBERRIES")
                    {
                        var result = await WbClasses.Functions.UpdatePrice(_httpService, _firmProxy[priceData.Key.FirmaId], priceData.Key.AuthToken,
                            priceData.Select(x => 
                            {
                                long.TryParse(x.ProductId, out long nmId);
                                return new WbClasses.PriceRequest
                                {
                                    NmId = nmId,
                                    Price = decimal.ToInt32(x.Квант * x.Цена)
                                };
                            }).ToList(),
                            stoppingToken);
                        if (result.Item1)
                        {
                            uploadIds.AddRange(priceData.Select(x => x.Id));
                            if (!string.IsNullOrEmpty(result.Item2))
                                _logger.LogError(result.Item2);
                        }
                        else
                        {
                            if (_context.Database.CurrentTransaction != null)
                                _context.Database.CurrentTransaction.Rollback();
                            if (!string.IsNullOrEmpty(result.Item2))
                                _logger.LogError(result.Item2);
                            uploadIds.Clear();
                        }
                    }
                    if (uploadIds.Count > 0)
                    {
                        int tryCount = 5;
                        TimeSpan sleepPeriod = TimeSpan.FromSeconds(1);
                        while (true)
                        {
                            using var tran = await _context.Database.BeginTransactionAsync();
                            try
                            {
                                _context.VzUpdatingPrices
                                    .Where(x => uploadIds.Contains(x.MuId))
                                    .ToList()
                                    .ForEach(x =>
                                    {
                                        x.Flag = false;
                                        x.Updated = DateTime.Now;
                                    });
                                await _context.SaveChangesAsync(stoppingToken);

                                if (_context.Database.CurrentTransaction != null)
                                    tran.Commit();
                                break;
                            }
                            catch (DbUpdateException db_ex)
                            {
                                if (_context.Database.CurrentTransaction != null)
                                    _context.Database.CurrentTransaction.Rollback();
                                if (--tryCount == 0)
                                {
                                    _logger.LogError(db_ex.InnerException.ToString());
                                    break;
                                }
                                await Task.Delay(sleepPeriod);
                            }
                            catch (Exception ex)
                            {
                                if (_context.Database.CurrentTransaction != null)
                                    _context.Database.CurrentTransaction.Rollback();
                                if (--tryCount == 0)
                                {
                                    _logger.LogError(ex.Message);
                                    break;
                                }
                                await Task.Delay(sleepPeriod);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                _logger.LogError(ex.Message);
            }
        }
        private async Task UpdateQuantum(string firmaId, string campaignId, string clientId, string authToken, string marketplaceId, 
            EncodeVersion encoding, 
            IList<YandexClasses.OfferMappingEntry> offerEntries,
            CancellationToken cancellationToken)
        {
            var codes = offerEntries.Select(x => x.Offer.ShopSku.Decode(encoding));
            var localData = await (from markUse in _context.Sc14152s
                                   join nom in _context.Sc84s on markUse.Parentext equals nom.Id
                                   where codes.Contains(nom.Code) &&
                                     markUse.Sp14147 == marketplaceId
                                   select new
                                   {
                                       NomCode = nom.Code.Encode(encoding),
                                       Quantum = nom.Sp14188
                                   }).ToListAsync(cancellationToken);
            if ((localData != null) && (localData.Count > 0))
            {
                var updateData = localData
                                 .Join(offerEntries,
                                    local => local.NomCode,
                                    ome => ome.Offer.ShopSku,
                                    (local, ome) => new { Quantum = (long)local.Quantum, ome })
                                 .Where(x => x.Quantum != x.ome.Offer.QuantumOfSupply)
                                 .Select(x =>
                                 {
                                     x.ome.Offer.QuantumOfSupply = x.Quantum;
                                     x.ome.Offer.Availability = null;
                                     x.ome.Offer.ProcessingState = null;
                                     x.ome.Mapping.ModelId = null;
                                     x.ome.Mapping.CategoryId = null;
                                     x.ome.AwaitingModerationMapping = null;
                                     x.ome.RejectedMapping = null;
                                     return x.ome;
                                 })
                                 .ToList();
                if ((updateData != null) && (updateData.Count > 0))
                {
                    var result = await YandexClasses.YandexOperators.UpdateOfferEntries(_httpService, _firmProxy[firmaId], campaignId, clientId, authToken, updateData, cancellationToken);
                    if (!string.IsNullOrEmpty(result.Item2))
                        _logger.LogError(result.Item2);
                }
            }
        }
        private async Task UpdateCatalogInfo(object data, string marketplaceId, EncodeVersion encoding, CancellationToken stoppingToken)
        {
            var skus = new List<string>();
            var productIdToSku = new Dictionary<string, string>();
            if (data is IList<YandexClasses.OfferMappingEntry>)
            {
                foreach (var entry in (data as IList<YandexClasses.OfferMappingEntry>))
                    if ((entry.Offer != null) && (entry.Offer.ProcessingState != null))
                    {
                        var nomCode = entry.Offer.ShopSku.Decode(encoding);
                        if (string.IsNullOrEmpty(nomCode))
                        {
                            _logger.LogError("UpdateCatalogInfo : Yandex wrong encoded sku " + entry.Offer.ShopSku);
                            continue;
                        }
                        skus.Add(nomCode);
                        string краткоеОписание = "";
                        if ((entry.Offer.Urls != null) && (entry.Offer.Urls.Count > 0))
                            краткоеОписание = entry.Offer.Urls.FirstOrDefault();
                        string подробноеОписание = entry.Offer.Description;
                        string комментарий = "";
                        if (entry.Offer.GuaranteePeriod != null)
                            комментарий = комментарий.ConditionallyAppend("Гарантия " + entry.Offer.GuaranteePeriod.TimePeriod.ToString() + " " + Enum.GetName(entry.Offer.GuaranteePeriod.TimeUnit).Склонять((int)entry.Offer.GuaranteePeriod.TimePeriod));
                        if (entry.Offer.LifeTime != null)
                            комментарий = комментарий.ConditionallyAppend("Срок службы " + entry.Offer.LifeTime.TimePeriod.ToString() + " " + Enum.GetName(entry.Offer.LifeTime.TimeUnit).Склонять((int)entry.Offer.LifeTime.TimePeriod));
                        decimal весБруттоКг = 0;
                        decimal длинаМ = 0;
                        decimal ширинаМ = 0;
                        decimal высотаМ = 0;
                        if (entry.Offer.WeightDimensions != null)
                        {
                            весБруттоКг = (decimal)entry.Offer.WeightDimensions.Weight;
                            длинаМ = (decimal)entry.Offer.WeightDimensions.Length / 100;
                            ширинаМ = (decimal)entry.Offer.WeightDimensions.Width / 100;
                            высотаМ = (decimal)entry.Offer.WeightDimensions.Height / 100;
                        }
                        await _номенклатура.ОбновитьЗначенияПараметров(nomCode, краткоеОписание, подробноеОписание, комментарий,
                            весБруттоКг, длинаМ, ширинаМ, высотаМ,
                            _httpService, entry.Offer.Pictures,
                            stoppingToken);
                    }
            }
            else if (data is IList<OzonClasses.Item>)
            {
                foreach (var item in (data as IList<OzonClasses.Item>))
                    if (!string.IsNullOrWhiteSpace(item.Offer_id) && (item.Offer_id != "0"))
                    {
                        var nomCode = item.Offer_id.Decode(encoding);
                        if (string.IsNullOrEmpty(nomCode))
                        {
                            _logger.LogError("UpdateCatalogInfo : Ozon wrong encoded sku " + item.Offer_id);
                            continue;
                        }
                        skus.Add(nomCode);
                    }
            }
            //else if (data is IList<AliExpressClasses.Item_display_dto>)
            //{
            //    foreach(var item in (data as IList<AliExpressClasses.Item_display_dto>))
            //    {
            //        var decodeSku = hexEncoding ? x.Code.DecodeHexString() : item.sk;
            //        skus.AddRange(decodeSkus);
            //        productIdToSku.Add(entry.Id, decodeSkus.FirstOrDefault() ?? "");
            //    }
            //}
            else if (data is IList<AliExpressClasses.CatalogInfo>)
            {
                foreach (var entry in (data as IList<AliExpressClasses.CatalogInfo>))
                    if ((entry.Sku != null) && (entry.Sku.Count > 0))
                    {
                        var decodeSkus = entry.Sku.Select(x => 
                        {
                            var nomCode = x.Code.Decode(encoding);
                            if (string.IsNullOrEmpty(nomCode))
                            {
                                _logger.LogError("UpdateCatalogInfo : Aliexpress wrong encoded sku " + x.Code);
                                nomCode = "";
                            }
                            return nomCode;
                        });
                        skus.AddRange(decodeSkus);
                        productIdToSku.Add(entry.Id, decodeSkus.FirstOrDefault() ?? "");
                    }
            }
            else if (data is WbClasses.CardListResponse.CardListData wbData)
            {
                if (wbData.Cards?.Count > 0)
                    foreach (var entry in wbData.Cards)
                    {
                        string nmId = entry.NmID.ToString();
                        string nomCode = entry.VendorCode.Decode(encoding);
                        if (string.IsNullOrEmpty(nomCode))
                        {
                            _logger.LogError("UpdateCatalogInfo : Wildberries wrong encoded sku " + entry.VendorCode);
                            continue;
                        }
                        skus.Add(nomCode);
                        productIdToSku.Add(nmId, nomCode);
                    }
            }
            try
            {
                foreach (var sku in skus)
                {
                    if (!string.IsNullOrWhiteSpace(sku))
                    {
                        var entities = await (from markUse in _context.Sc14152s
                                              join nom in _context.Sc84s on markUse.Parentext equals nom.Id
                                              where nom.Code == sku && markUse.Sp14147 == marketplaceId
                                              select markUse).ToListAsync(stoppingToken);
                        var productId = productIdToSku.Where(v => v.Value == sku).Select(k => k.Key).FirstOrDefault() ?? String.Empty;
                        if ((entities == null) || (entities.Count == 0))
                        {
                            var parentId = await _context.Sc84s
                                .Where(x => x.Code == sku)
                                .Select(x => x.Id)
                                .FirstOrDefaultAsync();
                            if (!string.IsNullOrWhiteSpace(parentId))
                            {
                                var entity = new Sc14152
                                {
                                    Id = _context.GenerateId(14152),
                                    Parentext = parentId,
                                    Ismark = false,
                                    Verstamp = 0,
                                    Sp14147 = marketplaceId,
                                    Sp14148 = 0, //ФиксЦена
                                    Sp14149 = 0, //КоэфПроверки
                                    Sp14150 = 0, //Флаг - пусть цены обновятся
                                    Sp14158 = 1, //Есть в каталоге
                                    Sp14174 = Common.min1cDate, //UpdatedAt = Min1C !!!
                                    Sp14178 = Common.min1cDate, //StockUpdatedAt
                                    Sp14179 = 0, //StockUpdated - пусть stock обновится
                                    Sp14187 = 0, //Quantum = 0
                                    Sp14190 = productId,
                                    Sp14198 = 0, //Комиссия
                                    Sp14213 = 0, //КоррЦенПроцент
                                    Sp14214 = 0, //КоррОстатков
                                    Sp14229 = 0, //VolumeWeight
                                    Sp14323 = 0, //FrosenPrice
                                };
                                await _context.Sc14152s.AddAsync(entity, stoppingToken);
                                _context.РегистрацияИзмененийРаспределеннойИБ(14152, entity.Id);
                            }
                        }
                        else
                            foreach (var entity in entities.Where(x => (x.Sp14158 != 1) || ((productIdToSku.Count > 0) && (x.Sp14190.Trim() != productId)))) 
                            {
                                entity.Sp14158 = 1;
                                if (productIdToSku.Count > 0)
                                    entity.Sp14190 = productId;
                                _context.Update(entity);
                                _context.РегистрацияИзмененийРаспределеннойИБ(14152, entity.Id);
                            }
                        await _context.SaveChangesAsync(stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
        private async Task ParseNextPageCatalogRequest(string proxyHost, string campaignId, string clientId, string authToken, int limit, string nextPageToken, string marketplaceId, string marketplaceModel, EncodeVersion encoding, CancellationToken stoppingToken)
        {
            var result = await YandexClasses.YandexOperators.Exchange<YandexClasses.OfferMappingEntriesResponse>(_httpService,
                $"https://{proxyHost}api.partner.market.yandex.ru/v2/campaigns/{campaignId}/offer-mapping-entries.json?limit={limit}"
                + (string.IsNullOrEmpty(nextPageToken) ? "" : "&page_token=" + nextPageToken),
                HttpMethod.Get,
                clientId,
                authToken,
                null,
                stoppingToken);

            if ((result.Item1 == YandexClasses.ResponseStatus.ERROR) && !string.IsNullOrEmpty(result.Item3))
            {
                _logger.LogError(result.Item3);
            }
            else
            {
                if (result.Item2 != null)
                    try
                    {
                        var catalogResponse = result.Item2;
                        if ((catalogResponse.Status == YandexClasses.ResponseStatus.OK) && (catalogResponse.Result != null))
                        {
                            //if ((marketplaceModel == "FBY") || (marketplaceModel == "FBS"))
                            //    await UpdateQuantum(campaignId,clientId,authToken, marketplaceId, hexEncoding, catalogResponse.Result.OfferMappingEntries, stoppingToken);
                            await UpdateCatalogInfo(catalogResponse.Result.OfferMappingEntries, marketplaceId, encoding, stoppingToken);
                            if ((catalogResponse.Result.Paging != null) && (!string.IsNullOrEmpty(catalogResponse.Result.Paging.NextPageToken)))
                            {
                                await ParseNextPageCatalogRequest(proxyHost, campaignId, clientId, authToken, limit, catalogResponse.Result.Paging.NextPageToken, marketplaceId, marketplaceModel, encoding, stoppingToken);
                            }
                        }
                        else
                        {
                            _logger.LogError("Internal : Catalog response ERROR status or RESULT is null");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Internal (ParseNextPageCatalogRequest) : " + ex.Message);
                    }
            }
        }
        private async Task ParseNextPageCatalogOzon(string proxyHost, string clientId, string authToken, int limit, string nextPageToken, string marketplaceId, EncodeVersion encoding, CancellationToken stoppingToken)
        {
            var result = await OzonClasses.OzonOperators.ParseCatalog(_httpService, proxyHost, clientId, authToken, nextPageToken, limit, stoppingToken);
            if (result.Item2 != null && !string.IsNullOrEmpty(result.Item2))
            {
                _logger.LogError(result.Item2);
            }
            if (result.Item1 != null)
                try
                {
                    await UpdateCatalogInfo(result.Item1.Items, marketplaceId, encoding, stoppingToken);
                    if (!string.IsNullOrEmpty(result.Item1.Last_id))
                    {
                        await ParseNextPageCatalogOzon(proxyHost, clientId, authToken, limit, result.Item1.Last_id, marketplaceId, encoding, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Internal (ParseNextPageCatalogOzon) : " + ex.Message);
                }
        }
        public async Task ParseNextPageCatalogAliExpressGlobal(string appKey, string appSecret, string authToken, int currentPage, int limit, string marketplaceId, EncodeVersion encoding, CancellationToken stoppingToken)
        {
            var result = await AliExpressClasses.Functions.GetCatalogInfoGlobal(_httpService, appKey, appSecret, authToken,
                currentPage, limit,
                stoppingToken);
            if (result.Item2 != null && !string.IsNullOrEmpty(result.Item2))
            {
                _logger.LogError(result.Item2);
            }
            if (result.Item1 != null)
                try
                {
                    if ((result.Item1.Aeop_a_e_product_display_d_t_o_list != null) &&
                        (result.Item1.Aeop_a_e_product_display_d_t_o_list.Item_display_dto != null) &&
                        (result.Item1.Aeop_a_e_product_display_d_t_o_list.Item_display_dto.Count > 0))
                    {
                        await UpdateCatalogInfo(result.Item1.Aeop_a_e_product_display_d_t_o_list.Item_display_dto.Where(x => x.Product_id.HasValue && (x.Product_id.Value > 0)).ToList(),
                            marketplaceId, encoding, stoppingToken);
                    }
                    var totalPages = result.Item1.Total_page.HasValue ? result.Item1.Total_page.Value : 1;
                    if (totalPages > currentPage)
                    {
                        await ParseNextPageCatalogAliExpressGlobal(appKey, appSecret, authToken, currentPage+1, limit, marketplaceId, encoding, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("ParseMextPageCatalogAliExpress internal : " + ex.Message);
                }
        }
        public async Task ParseNextPageCatalogAliExpress(string proxyHost, string authToken, string lastProductId, int limit, string marketplaceId, EncodeVersion encoding, CancellationToken stoppingToken)
        {
            var result = await AliExpressClasses.Functions.GetCatalogInfo(_httpService, proxyHost, authToken, lastProductId, limit, stoppingToken);
            if (result.Item2 != null && !string.IsNullOrEmpty(result.Item2))
            {
                _logger.LogError(result.Item2);
            }
            if ((result.Item1 != null) && (result.Item1.Count > 0))
                try
                {
                    await UpdateCatalogInfo(result.Item1, marketplaceId, encoding, stoppingToken);
                    lastProductId = result.Item1.LastOrDefault()?.Id;
                    if (!string.IsNullOrEmpty(lastProductId))
                    {
                        await ParseNextPageCatalogAliExpress(proxyHost, authToken, lastProductId, limit, marketplaceId, encoding, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("ParseMextPageCatalogAliExpress internal : " + ex.Message);
                }
        }
        void SaveXmlFile(string path, string marketplaceId, int fileNo, List<ParentTree> categories, object offers)
        {
            if ((offers as IList).Count > 0)
            {
                var xmlStructureData = Enumerable.Repeat(new
                {
                    FeedDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                    Categories = fileNo > 1 ? new List<ParentTree>() : categories,
                    Offers = offers
                }, 1).FirstOrDefault();
                byte[] file = xmlStructureData.CreateFromTemplate("SberProductFeed");
                path = Path.Combine(path, "feed_" + marketplaceId + "_" + fileNo.ToString("000") + ".xml");
                try
                {
                    File.WriteAllBytes(path, file);
                }
                catch(Exception e)
                {
                    _logger.LogError("SaveXmlFile: " + e.Message);
                }
                //string xmlFeed = XmlHelper.CreateFromTemplate("SberProductFeed", xmlStructureData);
                //Console.Write(xmlFeed);
            }
        }
        private async Task ParseCatalogSber(string firmaId, string marketplaceId, string campaignId, int limit, EncodeVersion encoding, decimal multiplayer, CancellationToken cancellationToken)
        {
            int productsPerFile = 20000;
            var configSections = _configuration.GetSection("Catalog:productFeed").GetChildren();
            string path = "";
            foreach (var item in configSections)
            {
                var configData = item.AsEnumerable();
                var configItem = configData.Where(x => x.Key.EndsWith("FirmaId") && x.Value == firmaId).Count();
                if (configItem == 1)
                {
                    path = configData.Where(x => x.Key.EndsWith("Path")).Select(x => x.Value).FirstOrDefault();
                    break;
                }
            }
            if (decimal.TryParse(_configuration["Pricer:checkCoefficient"], out decimal checkCoeff))
                checkCoeff = Math.Max(checkCoeff, 1);
            else
                checkCoeff = 1.2m;
            bool needNDS = (await _фирма.ПолучитьУчитыватьНДСAsync(firmaId)) != 0;
            var totalRecords = _context.Sc14152s
                .Where(x => x.Sp14147 == marketplaceId)
                .Count();
            var query = from markUse in _context.Sc14152s
                        join nom in _context.Sc84s on markUse.Parentext equals nom.Id
                        join ed in _context.Sc75s on nom.Sp94 equals ed.Id
                        join brend in _context.Sc8840s on nom.Sp8842 equals brend.Id into _brend
                        from brend in _brend.DefaultIfEmpty()
                        join nomGr1 in _context.Sc84s on nom.Parentid equals nomGr1.Id into _nomGr1
                        from nomGr1 in _nomGr1.DefaultIfEmpty()
                        join nomGr2 in _context.Sc84s on nomGr1.Parentid equals nomGr2.Id into _nomGr2
                        from nomGr2 in _nomGr2.DefaultIfEmpty()
                        join nomGr3 in _context.Sc84s on nomGr2.Parentid equals nomGr3.Id into _nomGr3
                        from nomGr3 in _nomGr3.DefaultIfEmpty()
                        join vzTovar in _context.VzTovars on nom.Id equals vzTovar.Id into _vzTovar
                        from vzTovar in _vzTovar.DefaultIfEmpty()
                        where markUse.Sp14147 == marketplaceId
                          //&& nom.Code == "D00045010"
                        select new
                        {
                            Id = markUse.Id,
                            InCatalog = markUse.Sp14158 == 1,
                            Deleted = markUse.Ismark,
                            Sku = nom.Code.Encode(encoding),
                            Name = nom.Descr.Trim(),
                            Brend = brend != null ? brend.Descr.Trim() : "",
                            Vendor = nom.Sp85.Trim(),
                            Description = nom.Sp101,
                            Barcode = ed.Sp80.Trim(),
                            Weight = ed.Sp14056, //кг
                            Width = ed.Sp14036 * 100, //см
                            Length = ed.Sp14037 * 100, //см
                            Height = ed.Sp14035 * 100, //см
                            CategoryId = nomGr1 != null ? nomGr1.Code.Encode(encoding) : "",
                            GroupLevel1 = nomGr3 != null ? new ParentTree { Sku = nomGr3.Code.Encode(encoding), ParentSku = "", Name = nomGr3.Descr.Trim() } :
                                nomGr2 != null ? new ParentTree { Sku = nomGr2.Code.Encode(encoding), ParentSku = "", Name = nomGr2.Descr.Trim() } :
                                nomGr1 != null ? new ParentTree { Sku = nomGr1.Code.Encode(encoding), ParentSku = "", Name = nomGr1.Descr.Trim() } : null,
                            GroupLevel2 = nomGr3 != null ? new ParentTree { Sku = nomGr2.Code.Encode(encoding), ParentSku = nomGr3.Code.Encode(encoding), Name = nomGr2.Descr.Trim() } :
                                nomGr2 != null ? new ParentTree { Sku = nomGr1.Code.Encode(encoding), ParentSku = nomGr2.Code.Encode(encoding), Name = nomGr1.Descr.Trim() } : null,
                            GroupLevel3 = ((nomGr3 != null) && (nomGr2 != null)) ? new ParentTree { Sku = nomGr1.Code.Encode(encoding), ParentSku = nomGr2.Code.Encode(encoding), Name = nomGr1.Descr.Trim() } : null,
                            НДС_Id = nom.Sp103,
                            Квант = nom.Sp14188 == 0 ? 1 : (int)nom.Sp14188,
                            DeltaPrice = markUse.Sp14213,
                            ЦенаРозн = vzTovar != null ? vzTovar.Rozn ?? 0 : 0,
                            ЦенаСп = vzTovar != null ? vzTovar.RoznSp ?? 0 : 0,
                            ЦенаЗакуп = vzTovar != null ? vzTovar.Zakup ?? 0 : 0,
                            ЦенаФикс = markUse.Sp14148,
                            Коэф = markUse.Sp14149,
                        };
            var categories = new List<ParentTree>();
            var offersStructure = Enumerable.Repeat(new
            {
                Sku = "",
                Available = "",
                Name = "",
                Price = "",
                //OldPrice = 0m,
                CategoryId = "",
                Vat = 0,
                Brend = "",
                Vendor = "",
                Description = "",
                Barcode = "",
                Weight = "",
                Width = "",
                Height = "",
                Length = "",
                Квант = 0
            }, 0);
            var offers = offersStructure.ToList();
            var offersFirstFile = offersStructure.ToList();
            int fileNo = 0;
            for (int i = 0; i < totalRecords; i = i + limit)
            {
                if (i % productsPerFile == 0)
                {
                    if (fileNo == 1)
                        offersFirstFile = offers.Select(x => x with { }).ToList();
                    else
                        SaveXmlFile(path, campaignId, fileNo, categories, offers);
                    offers.Clear();
                    fileNo++;
                }
                var data = await query
                    .OrderBy(x => x.Name)
                    .Skip(i)
                    .Take(limit)
                    .ToListAsync(cancellationToken);
                bool needUpdate = false;
                foreach (var r in data)
                {
                    if ((r.GroupLevel1 != null) && (!categories.Contains(r.GroupLevel1)))
                        categories.Add(r.GroupLevel1);
                    if ((r.GroupLevel2 != null) && (!categories.Contains(r.GroupLevel2)))
                        categories.Add(r.GroupLevel2);
                    if ((r.GroupLevel3 != null) && (!categories.Contains(r.GroupLevel3)))
                        categories.Add(r.GroupLevel3);

                    var Цена = GetPriceMarketplace(r.ЦенаРозн, r.ЦенаСп, r.ЦенаЗакуп, checkCoeff, r.ЦенаФикс, r.Коэф, multiplayer, r.DeltaPrice);
                    var ЦенаДоСкидки = Цена < r.ЦенаРозн ? r.ЦенаРозн : 0.00m;

                    var oldPrice = ЦенаДоСкидки * r.Квант;
                    var price = Цена * r.Квант;
                    if (price <= 0)
                        continue;

                    offers.Add(new
                    {
                        Sku = r.Sku,
                        Available = r.Deleted ? "false" : "true",
                        Name = r.Name + (r.Квант > 1 ? " - " + r.Квант.ToString() + " шт." : ""),
                        Price = price.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                        //OldPrice = oldPrice,
                        CategoryId = r.CategoryId,
                        Vat = SberClasses.Functions.NDS(needNDS ? _номенклатура.GetСтавкаНДС(r.НДС_Id).Процент : -1),
                        Brend = r.Brend,
                        Vendor = r.Vendor + (r.Квант > 1 ? "-" + r.Квант.ToString() : ""),
                        Description = r.Description + (r.Квант > 1 ? " - " + r.Квант.ToString() + " шт." : ""),
                        Barcode = r.Квант > 1 ? "" : r.Barcode,
                        Weight = r.Weight.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                        Width = r.Width.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                        Height = r.Height.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                        Length = r.Length.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                        Квант = r.Квант
                    });
                    if (!r.InCatalog)
                    {
                        var entity = await _context.Sc14152s.FirstOrDefaultAsync(x => x.Id == r.Id, cancellationToken);
                        entity.Sp14158 = 1;
                        _context.Update(entity);
                        _context.РегистрацияИзмененийРаспределеннойИБ(14152, entity.Id);
                        needUpdate = true;
                    }
                }
                if (needUpdate)
                    await _context.SaveChangesAsync(cancellationToken);
            }

            SaveXmlFile(path, campaignId, fileNo, categories, offers);
            SaveXmlFile(path, campaignId, 1, categories, offersFirstFile);
            categories.Clear();
            offers.Clear();
            offersFirstFile.Clear();
        }
        private async Task ParseNextPageWildberriesCatalog(string proxyHost, string authToken, int limit, DateTime updatedAt, long nmId, string marketplaceId, EncodeVersion encoding, CancellationToken stoppingToken)
        {
            var result = await WbClasses.Functions.GetCatalogInfo(_httpService, proxyHost, authToken, limit, updatedAt, nmId, stoppingToken);
            if (!string.IsNullOrEmpty(result.error))
                _logger.LogError(result.error);
            if (result.data != null)
                try
                {
                    await UpdateCatalogInfo(result.data, marketplaceId, encoding, stoppingToken);
                    var lastUpdatedAt = result.data.Cursor?.UpdatedAt ?? DateTime.MinValue;
                    var lastNmId = result.data.Cursor?.NmID ?? 0;
                    var total = result.data.Cursor?.Total ?? 0;
                    if (total >= limit)
                        await ParseNextPageWildberriesCatalog(proxyHost, authToken, limit, lastUpdatedAt, lastNmId, marketplaceId, encoding, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError("ParseWildberriesCatalog internal : " + ex.Message);
                }
        }
        public async Task CheckCatalog(CancellationToken stoppingToken)
        {
            if (int.TryParse(_configuration["Catalog:maxEntriesResponse"], out int maxResponseEntries))
                maxResponseEntries = Math.Max(maxResponseEntries, 1);
            else
                maxResponseEntries = 100;
            try
            {
                var marketplaceIds = await (from market in _context.Sc14042s 
                                            where !market.Ismark
                                                //&& (market.Sp14155.Trim() == "Яндекс")
                                                //&& (market.Sp14155.Trim() == "Wildberries")
                                                //&& (market.Code.Trim() == "43956")
                                                //&& (market.Code.Trim() == "D0000000000000000001")
                                            select new
                                            {
                                                Id = market.Id,
                                                Тип = market.Sp14155.Trim().ToUpper(),
                                                Модель = market.Sp14164.Trim().ToUpper(),
                                                Наименование = market.Descr.Trim(),
                                                CampaignId = market.Code.Trim(),
                                                ClientId = market.Sp14053.Trim(),
                                                AuthToken = market.Sp14054.Trim(),
                                                AuthSecret = market.Sp14195.Trim(),
                                                Authorization = market.Sp14077.Trim(),
                                                FeedId = market.Sp14154.Trim(),
                                                Encoding = (EncodeVersion)market.Sp14153,
                                                Multiplayer = market.Sp14165,
                                                FirmaId = market.Parentext
                                                //NeedStockRefresh = market.Sp14177 == 1
                                            })
                                            .ToListAsync();
                foreach (var marketplace in marketplaceIds)
                {
                    if (marketplace.Тип == "ЯНДЕКС")
                        await ParseNextPageCatalogRequest(
                            _firmProxy[marketplace.FirmaId], 
                            marketplace.CampaignId, 
                            marketplace.ClientId, 
                            marketplace.AuthToken, 
                            maxResponseEntries, 
                            null,
                            marketplace.Id, 
                            marketplace.Модель,
                            marketplace.Encoding, 
                            stoppingToken);
                    else if (marketplace.Тип == "OZON")
                    {
                        await ParseNextPageCatalogOzon(_firmProxy[marketplace.FirmaId],
                            marketplace.ClientId, marketplace.AuthToken, maxResponseEntries, null,
                            marketplace.Id, marketplace.Encoding, stoppingToken);
                    }
                    else if (marketplace.Тип == "ALIEXPRESS")
                    {
                        //await ParseMextPageCatalogAliExpressGlobal(marketplace.ClientId, marketplace.Authorization, marketplace.AuthToken,
                        //    1, 50, marketplace.Id, marketplace.HexEncoding, stoppingToken);
                        await ParseNextPageCatalogAliExpress(_firmProxy[marketplace.FirmaId],
                            marketplace.AuthToken, "0", 50, marketplace.Id, marketplace.Encoding, stoppingToken);
                    }
                    else if (marketplace.Тип == "SBER")
                    {
                        await ParseCatalogSber(marketplace.FirmaId, marketplace.Id, marketplace.CampaignId, 100, marketplace.Encoding, marketplace.Multiplayer, stoppingToken);
                    }
                    else if (marketplace.Тип == "WILDBERRIES")
                    {
                        await ParseNextPageWildberriesCatalog(_firmProxy[marketplace.FirmaId], marketplace.AuthToken, 1000, DateTime.MinValue, 0, marketplace.Id, marketplace.Encoding, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            //_logger.LogError("Done");
        }
        public async Task UpdateStock(bool regular, CancellationToken stoppingToken)
        {
            var defFirmaId = _configuration["Stocker:" + _configuration["Stocker:Firma"] + ":FirmaId"];
            try
            {
                var marketplaceIds = await (from market in _context.Sc14042s
                                            where !market.Ismark
                                                && (market.Sp14177 == 1)
                                                && (string.IsNullOrEmpty(defFirmaId) ? true : market.Parentext == defFirmaId)
                                                //&& (market.Code == "22498162235000") 
                                                //&& (market.Code == "43956")
                                                //&& (market.Code == "45715133
                                                //&& (market.Code == "23005267")
                                                //&& (market.Sp14155.Trim() == "Wildberries")
                                                //&& (market.Sp14155.Trim() == "AliExpress")
                                            select new
                                            {
                                                Id = market.Id,
                                                Тип = market.Sp14155.Trim().ToUpper(),
                                                Модель = market.Sp14164.Trim().ToUpper(),
                                                //Наименование = market.Descr.Trim(),
                                                FirmaId = market.Parentext,
                                                ClientId = market.Sp14053.Trim(),
                                                AuthToken = market.Sp14054.Trim(),
                                                AuthSecret = market.Sp14195.Trim(),
                                                Authorization = market.Sp14077.Trim(),
                                                Code = market.Code.Trim(),
                                                Encoding = (EncodeVersion)market.Sp14153,
                                                СкладId = market.Sp14241 == Common.ПустоеЗначение ? "" : market.Sp14241,
                                                StockOriginal = market.Sp14216 == 1
                                            })
                                            .ToListAsync(stoppingToken);
                foreach (var marketplace in marketplaceIds)
                    await UpdateStockMarketplace(regular, marketplace.Тип, marketplace.Модель, 100, marketplace.Id, marketplace.ClientId, marketplace.AuthToken, marketplace.AuthSecret,
                        marketplace.FirmaId, marketplace.Encoding, marketplace.СкладId, marketplace.StockOriginal, marketplace.Code, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
        private async Task UpdateStockMarketplace(bool regular, string marketType, string marketModel, int limit, string marketplaceId,
            string clientId, string authToken, string authSecret,
            string firmaId, EncodeVersion encoding, string складId, bool stockOriginal,
            string marketplaceCode,
            CancellationToken cancellationToken)
        {
            var data = await ((from markUse in _context.Sc14152s
                               join nom in _context.Sc84s on markUse.Parentext equals nom.Id
                               join sc75 in _context.Sc75s on nom.Sp94 equals sc75.Id
                               join updStock in _context.VzUpdatingStocks on markUse.Id equals updStock.MuId
                               where (markUse.Sp14147 == marketplaceId) &&
                                 (markUse.Sp14158 == 1) && //Есть в каталоге 
                                 (((regular ? updStock.Flag : updStock.IsError) &&
                                   (updStock.Updated < DateTime.Now.AddMinutes(-2))) ||
                                  (updStock.Updated.Date != DateTime.Today))
                                  //nom.Id == "  1KTKD  "
                               select new
                               {
                                   Id = markUse.Id,
                                   Locked = markUse.Ismark,
                                   NomId = markUse.Parentext,
                                   OfferId = nom.Code.Encode(encoding),
                                   ProductId = markUse.Sp14190.Trim(),
                                   Barcode = sc75.Sp80.Trim(),
                                   Квант = nom.Sp14188,
                                   DeltaStock = stockOriginal ? 0 : nom.Sp14215, //markUse.Sp14214,
                                   UpdatedAt = updStock.Updated,
                                   UpdatedFlag = updStock.Flag
                               })
            .OrderByDescending(x => x.UpdatedFlag)
            .ThenBy(x => x.UpdatedAt)
            .Take(limit))
            .ToListAsync(cancellationToken);
            if (data?.Count > 0)
            {
                bool fullLock = false;
                var notReadyIds = new List<string>();
                if (marketType == "OZON")
                {
                    var offersDictionary = data.ToDictionary(k => k.OfferId, v => v.Id);
                    notReadyIds = await GetOzonNotReadyProducts(firmaId, clientId, authToken, offersDictionary, cancellationToken);
                }
                else if (marketType == "WILDBERRIES")
                {
                    fullLock = await _склад.ЭтоРабочийДень(Common.SkladEkran, 1) != 1;
                }
                var listIds = data.Where(x => !notReadyIds.Contains(x.Id)).Select(x => x.Id).ToList();
                if (MarkVzUpdateStock(listIds, notReadyIds, null, null, null) && (listIds.Count > 0))
                {
                    var разрешенныеФирмы = await _фирма.ПолучитьСписокРазрешенныхФирмAsync(firmaId);
                    List<string> списокСкладов = null;
                    if (string.IsNullOrEmpty(складId))
                    {
                        if (marketModel == "DBS")
                            списокСкладов = await _склад.ПолучитьСкладIdОстатковMarketplace();
                        else
                            списокСкладов = new List<string> { Common.SkladEkran }; 
                    }
                    else
                        списокСкладов = new List<string> { складId };

                    var списокНоменклатуры = await _номенклатура.ПолучитьСвободныеОстатки(разрешенныеФирмы, списокСкладов, data.Where(x => !notReadyIds.Contains(x.Id)).Select(x => x.NomId).ToList(), false);
                    var резервыМаркета = await _номенклатура.GetReserveByMarketplace(marketplaceId, data.Where(x => !notReadyIds.Contains(x.Id)).Select(x => x.NomId));
                    var stockData = new List<(string productId, string offerId, string barcode, int stock)>();
                    foreach (var item in data.Where(x => !notReadyIds.Contains(x.Id)))
                    {
                        long остаток = 0;
                        if (!fullLock)
                        {
                            var номенклатура = списокНоменклатуры.Where(x => x.Id == item.NomId).FirstOrDefault();
                            if (номенклатура != null)
                            {
                                резервыМаркета.TryGetValue(item.NomId, out decimal резервМаркета);
                                if (item.Квант > 1)
                                {
                                    var остатокРегистр = номенклатура.Остатки
                                        .Where(x => x.СкладId == Common.SkladEkran)
                                        .Sum(x => x.СвободныйОстаток);
                                    if (marketType == "ЯНДЕКС")
                                        остатокРегистр += резервМаркета;
                                    остаток = (int)(((остатокРегистр / номенклатура.Единица.Коэффициент) - item.DeltaStock) / item.Квант);
                                    if (marketType == "ЯНДЕКС")
                                        остаток = остаток * (int)item.Квант;
                                }
                                else
                                {
                                    var остатокРегистр = номенклатура.Остатки.Sum(x => x.СвободныйОстаток);
                                    if (marketType == "ЯНДЕКС")
                                        остатокРегистр += резервМаркета;
                                    остаток = (long)((остатокРегистр - item.DeltaStock) / номенклатура.Единица.Коэффициент);
                                }
                                остаток = Math.Max(остаток, 0);
                            }
                        }
                        stockData.Add((productId: item.ProductId, offerId: item.OfferId, barcode: item.Barcode, stock: (item.Locked ? 0 : (int)остаток)));
                    }
                    if (stockData.Count > 0)
                    {
                        List<string> uploadIds = new List<string>();
                        List<string> tooManyIds = new List<string>();
                        List<string> errorIds = new List<string>();
                        switch (marketType)
                        {
                            case "OZON":
                                var resultOzon = await OzonClasses.OzonOperators.UpdateStock(_httpService, _firmProxy[firmaId], clientId, authToken,
                                    stockData.Select(x =>
                                    {
                                        if (!long.TryParse(string.IsNullOrWhiteSpace(x.productId) ? marketplaceCode : x.productId, out long WarehouseId))
                                            WarehouseId = 0;
                                        return new OzonClasses.StockRequest
                                        {
                                            Offer_id = x.offerId,
                                            Stock = x.stock,
                                            Warehouse_id = WarehouseId
                                        };
                                    }).ToList(),
                                    cancellationToken);
                                if (resultOzon.errorMessage != null && !string.IsNullOrEmpty(resultOzon.errorMessage))
                                    _logger.LogError(resultOzon.errorMessage);
                                if (resultOzon.updatedOfferIds?.Count > 0)
                                    uploadIds = data.Where(x => resultOzon.updatedOfferIds.Contains(x.OfferId)).Select(x => x.Id).ToList();
                                if (resultOzon.tooManyRequests?.Count > 0)
                                    tooManyIds = data.Where(x => resultOzon.tooManyRequests.Contains(x.OfferId)).Select(x => x.Id).ToList();
                                if (resultOzon.errorOfferIds?.Count > 0)
                                    errorIds = data.Where(x => resultOzon.errorOfferIds.Contains(x.OfferId)).Select(x => x.Id).ToList();
                                break;
                            case "SBER":
                                var resultSber = await SberClasses.Functions.UpdateStock(_httpService, _firmProxy[firmaId], authToken,
                                    stockData.ToDictionary(k => k.offerId, v => v.stock),
                                    cancellationToken);
                                if (!string.IsNullOrEmpty(resultSber.error))
                                    _logger.LogError(resultSber.error);
                                if (resultSber.success)
                                    uploadIds = listIds;
                                break;
                            case "ALIEXPRESS":
                                var resultAli = await AliExpressClasses.Functions.UpdateStock(_httpService, _firmProxy[firmaId], authToken,
                                    stockData.Select(x => new AliExpressClasses.Product
                                    {
                                        Product_id = x.productId,
                                        Skus = new List<AliExpressClasses.StockSku>
                                        {
                                            new AliExpressClasses.StockSku
                                            {
                                                Sku_code = x.offerId,
                                                Inventory = x.stock.ToString()
                                            }
                                        }
                                    }).ToList(),
                                    cancellationToken);
                                if (resultAli.ErrorMessage != null && !string.IsNullOrEmpty(resultAli.ErrorMessage))
                                    _logger.LogError(resultAli.ErrorMessage);
                                if (resultAli.UpdatedIds?.Count > 0)
                                    uploadIds.AddRange(data
                                        .Where(x => resultAli.UpdatedIds.Contains(x.ProductId))
                                        .Select(x => x.Id));
                                if (resultAli.ErrorIds?.Count > 0)
                                    errorIds = data
                                        .Where(x => resultAli.ErrorIds.Contains(x.ProductId))
                                        .Select(x => x.Id)
                                        .ToList();
                                break;
                            case "WILDBERRIES":
                                int.TryParse(marketplaceCode, out int warehouseId);
                                var resultWb = await WbClasses.Functions.UpdateStock(_httpService, _firmProxy[firmaId], authToken, warehouseId,
                                    stockData.ToDictionary(k => k.barcode, v => v.stock),
                                    cancellationToken);
                                var commonErrorTags = new List<string> { "common", "errorText", "additionalError" };
                                if (resultWb.errors?.Count > 0)
                                {
                                    foreach (var item in resultWb.errors)
                                        _logger.LogError("WILDBERRIES" + item.Key + ": " + item.Value);
                                    var errorOffers = resultWb.errors.Where(x => !commonErrorTags.Contains(x.Key)).Select(x => x.Key);
                                    errorIds = data.Where(x => errorOffers.Contains(x.Barcode)).Select(x => x.Id).ToList();
                                    uploadIds = data.Where(x => !errorOffers.Contains(x.Barcode)).Select(x => x.Id).ToList();
                                }
                                else
                                    uploadIds = data.Where(x => stockData.Select(y => y.barcode).Contains(x.Barcode)).Select(x => x.Id).ToList();
                                break;
                            case "ЯНДЕКС":
                                var resultYandex = await YandexClasses.YandexOperators.UpdateStock(_httpService, _firmProxy[firmaId], marketplaceCode, clientId, authToken, authSecret,
                                    stockData.ToDictionary(k => k.offerId, v => v.stock.ToString()),
                                    cancellationToken);
                                if (!string.IsNullOrEmpty(resultYandex.error))
                                    _logger.LogError(resultYandex.error);
                                if (resultYandex.success)
                                    uploadIds = listIds;
                                break;
                        }
                        if ((uploadIds.Count > 0) || (tooManyIds.Count > 0) || (errorIds.Count > 0))
                            MarkVzUpdateStock(null,null, uploadIds, tooManyIds, errorIds);
                    }
                }
            }
        }
        private async Task<List<string>> GetOzonNotReadyProducts(string firmaId, string clientId, string authToken, Dictionary<string,string> offersDictionary, CancellationToken cancellationToken)
        {
            var checkResult = await OzonClasses.OzonOperators.ProductNotReady(_httpService, _firmProxy[firmaId], clientId, authToken,
                offersDictionary.Select(x => x.Key).ToList(),
                cancellationToken);
            if (checkResult.Item2 != null && !string.IsNullOrEmpty(checkResult.Item2))
                _logger.LogError(checkResult.Item2);

            if ((checkResult.Item1 != null) && (checkResult.Item1.Count > 0))
                return offersDictionary.Where(x => checkResult.Item1.Contains(x.Key)).Select(x => x.Value).ToList();
            return new List<string>();
        }
        private bool MarkVzUpdateStock(List<string> markIds, List<string> notReadyIds, List<string> uploadIds, List<string> tooManyIds, List<string> errorIds)
        {
            int tryCount = 5;
            TimeSpan sleepPeriod = TimeSpan.FromSeconds(1);
            while (true)
            {
                using var tran = _context.Database.BeginTransaction();
                try
                {
                    if (markIds?.Count> 0) 
                        _context.VzUpdatingStocks
                            .Where(x => markIds.Contains(x.MuId))
                            .ToList()
                            .ForEach(x =>
                            {
                                x.Flag = false;
                                x.Taken = true;
                                x.IsError = false;
                            });
                    if (notReadyIds?.Count > 0)
                        _context.VzUpdatingStocks
                            .Where(x => notReadyIds.Contains(x.MuId))
                            .ToList()
                            .ForEach(x =>
                            {
                                x.Flag = false;
                                x.Taken = false;
                                x.IsError = true;
                                x.Updated = DateTime.Now;
                            });
                    if (uploadIds?.Count > 0)
                        _context.VzUpdatingStocks
                            .Where(x => uploadIds.Contains(x.MuId) && x.Taken)
                            .ToList()
                            .ForEach(x =>
                            {
                                x.Flag = false;
                                x.Taken = false;
                                x.IsError = false;
                                x.Updated = DateTime.Now;
                            });
                    if (tooManyIds?.Count > 0)
                        _context.VzUpdatingStocks
                            .Where(x => tooManyIds.Contains(x.MuId) && x.Taken)
                            .ToList()
                            .ForEach(x =>
                            {
                                x.Flag = true;
                                x.Taken = false;
                                x.IsError = false;
                                x.Updated = DateTime.Now;
                            });
                    if (errorIds?.Count > 0)
                        _context.VzUpdatingStocks
                            .Where(x => errorIds.Contains(x.MuId) && x.Taken)
                            .ToList()
                            .ForEach(x =>
                            {
                                x.Flag = false;
                                x.Taken = false;
                                x.IsError = true;
                                x.Updated = DateTime.Now;
                            });
                    _context.SaveChanges();
                    tran.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    if (_context.Database.CurrentTransaction != null)
                        _context.Database.CurrentTransaction.Rollback();
                    if (--tryCount == 0)
                    {
                        _logger.LogError(ex.Message);
                        break;
                    }
                    Task.Delay(sleepPeriod);
                }
            }
            return false;
        }
        public async Task RefreshOrders(CancellationToken stoppingToken)
        {
            try
            {
                var marketplaceIds = await (from market in _context.Sc14042s
                                        where !market.Ismark
                                            //&& (market.Sp14177 == 1)
                                            //&& (market.Sp14155.Trim() == "Яндекс")
                                            //&& (market.Sp14155.Trim() == "AliExpress")
                                            //&& (market.Sp14155.Trim() == "Sber")
                                            //&& (market.Sp14155.Trim() == "Wildberries")
                                            //&& (market.Code.Trim() == "22498162235000")
                                            //&& (market.Code.Trim() == "23503334320000")
                                            //&& (market.Code.Trim() == "1020000171757000")
                                            //&& (market.Code.Trim() == "45715133")
                                        select new
                                        {
                                            Id = market.Id,
                                            Code = market.Code.Trim(),
                                            Тип = market.Sp14155.ToUpper().Trim(),
                                            FirmaId = market.Parentext,
                                            CustomerId = market.Sp14175,
                                            DogovorId = market.Sp14176,
                                            ClientId = market.Sp14053.Trim(),
                                            AuthToken = market.Sp14054.Trim(),
                                            AuthSecret = market.Sp14195.Trim(),
                                            Authorization = market.Sp14077.Trim(),
                                            Encoding = (EncodeVersion)market.Sp14153
                                        })
                                        .ToListAsync();
                foreach (var marketplace in marketplaceIds)
                {
                    try
                    {
                        if (marketplace.Тип == "OZON")
                        {
                            await GetOzonNewOrders(marketplace.ClientId, marketplace.AuthToken,
                                marketplace.Id, marketplace.FirmaId, marketplace.CustomerId, marketplace.DogovorId,
                                marketplace.Encoding, stoppingToken);
                        }
                        else if (marketplace.Тип == "ЯНДЕКС")
                        {
                        }
                        else if (marketplace.Тип == "ALIEXPRESS")
                        {
                        }
                        else if (marketplace.Тип == "SBER")
                        {
                        }
                        else if (marketplace.Тип == "WILDBERRIES")
                        {
                            await GetWbNewOrders(marketplace.AuthToken,
                                marketplace.Id, marketplace.Authorization, marketplace.FirmaId, marketplace.CustomerId, marketplace.DogovorId,
                                marketplace.Encoding, stoppingToken);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("RefreshOrders single market : " + e.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("RefreshOrders : " + ex.Message);
            }
        }
        public async Task RefreshSlowOrders(string marketplaceId, CancellationToken stoppingToken)
        {
            try
            {
                bool periodOpened = !_заявкаПокупателя.NeedToOpenPeriod();
                var marketplaceIds = await (from market in _context.Sc14042s
                                            where !market.Ismark
                                            && string.IsNullOrEmpty(marketplaceId) ? true : market.Id == marketplaceId
                                            //&& (market.Sp14177 == 1)
                                            //&& (market.Sp14155.Trim() == "Яндекс")
                                            //&& (market.Sp14155.Trim() == "AliExpress")
                                            //&& (market.Sp14155.Trim() == "Sber")
                                            //&& (market.Sp14155.Trim() == "Wildberries")
                                            //&& (market.Code.Trim() == "22498162235000")
                                            //&& (market.Code.Trim() == "23503334320000")
                                            //&& (market.Code.Trim() == "45715133")
                                            
                                            select new
                                            {
                                                Id = market.Id,
                                                Code = market.Code.Trim(),
                                                Тип = market.Sp14155.ToUpper().Trim(),
                                                FirmaId = market.Parentext,
                                                CustomerId = market.Sp14175,
                                                DogovorId = market.Sp14176,
                                                ClientId = market.Sp14053.Trim(),
                                                AuthToken = market.Sp14054.Trim(),
                                                AuthSecret = market.Sp14195.Trim(),
                                                Authorization = market.Sp14077.Trim(),
                                                Encoding = (EncodeVersion)market.Sp14153
                                            })
                                        .ToListAsync();
                foreach (var marketplace in marketplaceIds)
                {
                    try
                    {
                        if (marketplace.Тип == "OZON")
                        {
                            await GetOzonCancelOrders(marketplace.FirmaId, marketplace.ClientId, marketplace.AuthToken,
                                marketplace.Id, stoppingToken);
                            if (periodOpened && !_sleepPeriods.Any(x => x.IsSleeping()))
                            {
                                await GetOzonDeliveringOrders(marketplace.Id, marketplace.FirmaId, marketplace.ClientId, marketplace.AuthToken, stoppingToken);
                                await GetOzonDeliveredOrders(marketplace.Id, marketplace.FirmaId, marketplace.ClientId, marketplace.AuthToken, stoppingToken);
                            }
                        }
                        else if (marketplace.Тип == "ЯНДЕКС")
                        {
                            await GetYandexNewDeliveringOrders(marketplace.Code, marketplace.ClientId, marketplace.AuthToken, marketplace.Authorization, marketplace.Id,
                                marketplace.FirmaId, marketplace.CustomerId, marketplace.DogovorId, marketplace.Encoding, stoppingToken);
                            await GetYandexOrders(periodOpened, marketplace.Code, marketplace.ClientId, marketplace.AuthToken, marketplace.Id,
                                marketplace.FirmaId, stoppingToken);
                            //await GetYandexCancelOrders(marketplace.FirmaId, marketplace.Code, marketplace.ClientId, marketplace.AuthToken, marketplace.Id, stoppingToken);
                            //if (periodOpened && !_sleepPeriods.Any(x => x.IsSleeping()))
                            //    await GetYandexDeliveredOrders(marketplace.Code, marketplace.ClientId, marketplace.AuthToken, marketplace.Id, marketplace.FirmaId, stoppingToken);
                        }
                        else if (marketplace.Тип == "ALIEXPRESS")
                        {
                            //await GetAliExpressOrdersGlobal(marketplace.ClientId, marketplace.AuthSecret, marketplace.Authorization,
                            //    marketplace.Id,
                            //    marketplace.FirmaId, marketplace.CustomerId, marketplace.DogovorId, marketplace.Encoding,
                            //    stoppingToken);
                            await GetAliExpressOrders(marketplace.AuthToken, marketplace.Id, marketplace.Authorization,
                                marketplace.FirmaId, marketplace.CustomerId, marketplace.DogovorId, marketplace.Encoding,
                                stoppingToken);
                        }
                        else if (marketplace.Тип == "SBER")
                        {
                            await GetSberOrders(periodOpened, marketplace.AuthToken, marketplace.Id, marketplace.Authorization,
                                marketplace.FirmaId, marketplace.CustomerId, marketplace.DogovorId, marketplace.Encoding,
                                stoppingToken);
                        }
                        else if (marketplace.Тип == "WILDBERRIES")
                        {
                            await RefreshWbOrders(marketplace.AuthToken, marketplace.Id, marketplace.FirmaId, stoppingToken);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("RefreshSlowOrders single market : " + e.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("RefreshSlowOrders : " + ex.Message);
            }
            //_logger.LogError("Done");
        }
        async Task GetYandexOrders(bool periodOpened, string campaignId, string clientId, string authToken, string marketplaceId,
            string firmaId, CancellationToken cancellationToken)
        {
            string pageToken = "";
            bool nextPage = true;
            while (nextPage)
            {
                nextPage = false;
                var result = await YandexClasses.YandexOperators.OrdersStats(_httpService,
                    _firmProxy[firmaId],
                    campaignId,
                    clientId,
                    authToken,
                    new List<string> { "UNPAID", "PROCESSING", "DELIVERY", "DELIVERED", "CANCELLED_BEFORE_PROCESSING", "CANCELLED_IN_PROCESSING", "PARTIALLY_DELIVERED", "PARTIALLY_RETURNED" },
                    DateTime.Today.AddDays(-60),
                    200,
                    pageToken,
                    cancellationToken);
                pageToken = result.NextPageToken;
                nextPage = !string.IsNullOrEmpty(pageToken);
                if (result.Orders?.Count > 0)
                {
                    var readyToShipOrders = await ActiveOrders(marketplaceId, cancellationToken);
                    foreach (var detailOrder in result.Orders)
                    {
                        var order = await _order.ПолучитьOrderByMarketplaceId(marketplaceId, detailOrder.Id.ToString());
                        if (order != null)
                        {
                            switch (detailOrder.Status)
                            {
                                case YandexClasses.StatusYandex.CANCELLED:
                                case YandexClasses.StatusYandex.CANCELLED_BEFORE_PROCESSING:
                                case YandexClasses.StatusYandex.CANCELLED_IN_PROCESSING:
                                    if ((order.InternalStatus != 5) && (order.InternalStatus != 6) && (order.InternalStatus != 14) && (order.InternalStatus != 16))
                                        await _docService.OrderCancelled(order);
                                    break;
                                case YandexClasses.StatusYandex.DELIVERY:
                                    TimeSpan ts = DateTime.Now - (detailOrder.StatusUpdateDate ?? DateTime.MinValue);
                                    if ((ts.TotalMinutes > 10) && readyToShipOrders.Contains(detailOrder.Id.ToString()) && 
                                        (order.DeliveryPartnerType != StinDeliveryPartnerType.SHOP) &&
                                        (order.InternalStatus < 14) && (order.InternalStatus != 6) && (order.InternalStatus != 5) && periodOpened && !_sleepPeriods.Any(x => x.IsSleeping()))
                                        await _docService.OrderDeliveried(order, true);
                                    break;
                                case YandexClasses.StatusYandex.DELIVERED:
                                case YandexClasses.StatusYandex.PARTIALLY_DELIVERED:
                                case YandexClasses.StatusYandex.PARTIALLY_RETURNED:
                                    if (((order.InternalStatus == 14) || (order.InternalStatus == 16)) && periodOpened && !_sleepPeriods.Any(x => x.IsSleeping()))
                                        await _docService.OrderFromTransferDeliveried(order);
                                    break;
                                case YandexClasses.StatusYandex.PROCESSING:
                                    if (order.InternalStatus == 0)
                                        await _order.ОбновитьOrderStatus(order.Id, 8);
                                    break;
                            }
                        }
                    }
                }
            }
        }
        async Task GetSberOrders(bool periodOpened, string authToken, string marketplaceId, string authorization,
            string firmaId, string customerId, string dogovorId, EncodeVersion encoding,
            CancellationToken cancellationToken)
        {
            var orderListResult = await SberClasses.Functions.SearchOrders(_httpService, _firmProxy[firmaId], authToken, cancellationToken);
            if (!string.IsNullOrEmpty(orderListResult.error))
                _logger.LogError(orderListResult.error);

            if (orderListResult.orders?.Count > 0)
            {
                var readyToShipOrders = await ActiveOrders(marketplaceId, cancellationToken);

                foreach (var sberOrder in orderListResult.orders)
                {
                    var order = await _order.ПолучитьOrderByMarketplaceId(marketplaceId, sberOrder.ShipmentId);
                    if (order == null)
                    {
                        //new order
                        if (sberOrder.Items.Any(x => (x.Status == SberClasses.SberStatus.CONFIRMED)))
                        {
                            var номенклатураCodes = sberOrder.Items.Select(x => x.OfferId.Decode(encoding)).ToList();
                            var разрешенныеФирмы = await _фирма.ПолучитьСписокРазрешенныхФирмAsync(firmaId);
                            var списокСкладов = await _склад.ПолучитьСкладIdОстатковMarketplace();
                            var НоменклатураList = await _номенклатура.ПолучитьСвободныеОстатки(
                                разрешенныеФирмы,
                                списокСкладов,
                                номенклатураCodes,
                                true);
                            var nomQuantums = await _номенклатура.ПолучитьКвант(номенклатураCodes, cancellationToken);
                            bool нетВНаличие = НоменклатураList.Count == 0;
                            foreach (var номенклатура in НоменклатураList)
                            {
                                decimal остаток = номенклатура.Остатки
                                            .Sum(z => z.СвободныйОстаток) / номенклатура.Единица.Коэффициент;
                                var quantum = (int)nomQuantums.Where(q => q.Key == номенклатура.Id).Select(q => q.Value).FirstOrDefault();
                                if (quantum == 0)
                                    quantum = 1;
                                var asked = sberOrder.Items?
                                                    .Where(b => b.OfferId.Decode(encoding) == номенклатура.Code)
                                                    .Select(c => c.Quantity * quantum)
                                                    .FirstOrDefault();
                                if (остаток < asked)
                                {
                                    нетВНаличие = true;
                                    break;
                                }
                            }
                            if (нетВНаличие)
                            {
                                _logger.LogError("Sber запуск процедуры отмены");
                                var sberResult = await SberClasses.Functions.OrderReject(_httpService, _firmProxy[firmaId], authToken, sberOrder.ShipmentId, SberClasses.SberReason.OUT_OF_STOCK,
                                    sberOrder.Items.Select(x => new KeyValuePair<string, string>(x.ItemIndex, x.OfferId)).ToList(), cancellationToken);
                                if (!string.IsNullOrEmpty(sberResult.error))
                                    _logger.LogError(sberResult.error);
                                continue;
                            }
                            bool delivery = !string.IsNullOrEmpty(sberOrder.DeliveryId);
                            var orderItems = new List<OrderItem>();
                            foreach (var item in sberOrder.Items)
                            {
                                orderItems.Add(new OrderItem
                                {
                                    Id = item.ItemIndex,
                                    Sku = item.OfferId.Decode(encoding),
                                    Количество = item.Quantity ?? 0,
                                    Цена = item.Price ?? 0,
                                    ЦенаСоСкидкой = item.FinalPrice ?? 0,
                                    Вознаграждение = (item.Price ?? 0) - (item.FinalPrice ?? 0),
                                    Доставка = delivery,
                                    //ДопПараметры = x.Params,
                                    ИдентификаторПоставщика = sberOrder.CustomerFullName,
                                    ИдентификаторСклада = "",
                                    ИдентификаторСкладаПартнера = sberOrder.ShippingPoint.ToString()
                                });
                            }
                            double deliveryPrice = 0;
                            double deliverySubsidy = 0;
                            await _docService.NewOrder(
                               "SBER",
                               firmaId,
                               customerId,
                               dogovorId,
                               authorization,
                               encoding,
                               Common.SkladEkran,
                               "",
                               sberOrder.DeliveryId,
                               sberOrder.DeliveryMethodId,
                               StinDeliveryPartnerType.SBER_MEGA_MARKET,
                               StinDeliveryType.DELIVERY,
                               deliveryPrice,
                               deliverySubsidy,
                               StinPaymentType.NotFound,
                               StinPaymentMethod.NotFound,
                               "0",
                               "",
                               null,
                               sberOrder.ShipmentId,
                               sberOrder.DeliveryId,
                               sberOrder.ShipmentDateFrom,
                               orderItems,
                               cancellationToken);
                        }
                    }
                    else
                    {
                        if (sberOrder.Items.Any(x => x.Status == SberClasses.SberStatus.CUSTOMER_CANCELED) &&
                                (order.InternalStatus != 5) && (order.InternalStatus != 6) && (order.InternalStatus != 14) && (order.InternalStatus != 16))
                            await _docService.OrderCancelled(order);
                        else if (readyToShipOrders.Contains(sberOrder.ShipmentId) && (sberOrder.Items?.All(x => x.Status == SberClasses.SberStatus.SHIPPED) ?? false) &&
                            (order.InternalStatus < 14) && (order.InternalStatus != 6) && (order.InternalStatus != 5) && periodOpened && !_sleepPeriods.Any(x => x.IsSleeping()))
                        {
                            await _docService.OrderDeliveried(order, true);
                        }
                        else if ((sberOrder.Items?.All(x => x.Status == SberClasses.SberStatus.DELIVERED) ?? false) &&
                            ((order.InternalStatus == 14) || (order.InternalStatus == 16)) && periodOpened && !_sleepPeriods.Any(x => x.IsSleeping()))
                        {
                            await _docService.OrderFromTransferDeliveried(order);
                        }
                    }
                }
            }

            var scannedOrders = orderListResult.orders?.Select(x => x.ShipmentId).ToList() ?? new List<string>();

            var activeOrders_14 = await _context.Sc13994s
                .Where(x => ((x.Sp13982 == 14) || (x.Sp13982 == 16)) &&
                    ((StinDeliveryPartnerType)x.Sp13985 == StinDeliveryPartnerType.SBER_MEGA_MARKET) &&
                    (x.Sp14038 == marketplaceId) &&
                    !scannedOrders.Contains(x.Code.Trim()))
                .Select(x => x.Code.Trim())
                .ToListAsync(cancellationToken);
            if (activeOrders_14?.Count > 0)
            {
                orderListResult = await SberClasses.Functions.GetOrders(_httpService, _firmProxy[firmaId], authToken, activeOrders_14, cancellationToken);
                if (orderListResult.orders?.Count > 0)
                {
                    foreach (var sberOrder in orderListResult.orders)
                    {
                        var order = await _order.ПолучитьOrderByMarketplaceId(marketplaceId, sberOrder.ShipmentId);
                        if (order != null)
                        {
                            if ((sberOrder.Items?.All(x => x.Status == SberClasses.SberStatus.DELIVERED) ?? false) &&
                            ((order.InternalStatus == 14) || (order.InternalStatus == 16)) && periodOpened && !_sleepPeriods.Any(x => x.IsSleeping()))
                            {
                                await _docService.OrderFromTransferDeliveried(order);
                            }
                        }
                    }
                }
            }
        }
        async Task<bool> SameWorkingDay(DateTime lastDate)
        {
            if (!TimeSpan.TryParse("17:00", out TimeSpan limitTime))
                limitTime = TimeSpan.MaxValue;
            var supplyDateIsWorking = await _склад.ЭтоРабочийДень(Common.SkladEkran, 0, lastDate) == 0;
            if (lastDate.Date == DateTime.Today)
            {
                if (supplyDateIsWorking)
                {
                    if (((lastDate.TimeOfDay < limitTime) && (DateTime.Now.TimeOfDay >= limitTime)) ||
                        ((lastDate.TimeOfDay >= limitTime) && (DateTime.Now.TimeOfDay < limitTime)))
                        return false;
                }
                return true;
            }
            else
            {
                if (supplyDateIsWorking && (lastDate.TimeOfDay < limitTime))
                    return false;
                return true;
            }
        }
        async Task<(bool success, string supplyId)> GetWbActiveSupplyId(string proxyHost, string authToken, CancellationToken cancellationToken)
        {
            var supplyListResult = await WbClasses.Functions.GetSuppliesList(_httpService, proxyHost, authToken, cancellationToken);
            if (!string.IsNullOrEmpty(supplyListResult.error))
            {
                _logger.LogError(supplyListResult.error);
                return (success: false, supplyId: "");
            }
            var supplyId = supplyListResult.supplyIds.FirstOrDefault();
            if (!string.IsNullOrEmpty(supplyId))
            {
                //var supplyOrdersResult = await WbClasses.Functions.GetSupplyOrders(_httpService, proxyHost, authToken, supplyId, cancellationToken);
                //if (!string.IsNullOrEmpty(supplyOrdersResult.error))
                //{
                //    _logger.LogError(supplyOrdersResult.error);
                //    return (success: false, supplyId: "");
                //}
                //DateTime? lastOrderCreated = supplyOrdersResult.orders?.Max(x => x.CreatedAt);
                //if (!lastOrderCreated.HasValue || (lastOrderCreated.HasValue && await SameWorkingDay(lastOrderCreated.Value)))
                    return (success: true, supplyId);
            }
            else
            {
                var supplyCreateResult = await WbClasses.Functions.CreateSupply(_httpService, proxyHost, authToken, cancellationToken);
                if (!string.IsNullOrEmpty(supplyCreateResult.error))
                {
                    _logger.LogError(supplyCreateResult.error);
                    return (success: false, supplyId: "");
                }
                return (success: true, supplyId: supplyCreateResult.supplyId ?? "");
            }
            //return (success: true, supplyId: "");
        }
        private async Task CreateLogisticsOrder(string firmaId, string orderId, AliExpressClasses.AliOrder aliOrder, string authToken, CancellationToken cancellationToken)
        {
            var firstOrderLine = aliOrder.Order_lines.FirstOrDefault();
            var logisticsOrderRequest = new AliExpressClasses.LogisticsOrderRequest(Convert.ToInt64(aliOrder.Id), firstOrderLine.Length ?? 0, firstOrderLine.Width ?? 0, firstOrderLine.Height ?? 0, (firstOrderLine.Weight ?? 0) / 1000);
            logisticsOrderRequest.Items.Add(new AliExpressClasses.LogisticsItem { Sku_id = Convert.ToInt64(firstOrderLine.Sku_id), Quantity = (int)firstOrderLine.Quantity });
            var result = await AliExpressClasses.Functions.CreateLogisticsOrder(_httpService, _firmProxy[firmaId], authToken, logisticsOrderRequest, cancellationToken);
            if (!string.IsNullOrEmpty(result.error))
                _logger.LogError(result.error);
            if (result.logisticsOrderId.HasValue && (result.logisticsOrderId.Value > 0))
                await _order.RefreshOrderDeliveryServiceId(orderId, result.logisticsOrderId.Value, result.trackNumber ?? "", cancellationToken);
        }
        private async Task GetAliExpressOrders(string authToken, string marketplaceId, string authorization,
            string firmaId, string customerId, string dogovorId, EncodeVersion encoding,
            CancellationToken cancellationToken)
        {
            bool periodOpened = !_заявкаПокупателя.NeedToOpenPeriod();
            int limit = 20;
            int pageNumber = 0;
            bool nextPage = true;
            while (nextPage)
            {
                pageNumber++;
                nextPage = false;
                var result = await AliExpressClasses.Functions.GetOrders(_httpService, _firmProxy[firmaId], authToken, pageNumber, limit, cancellationToken);
                if (!string.IsNullOrEmpty(result.error))
                    _logger.LogError(result.error);
                if (result.data != null)
                {
                    nextPage = result.data.Total_count > limit * pageNumber;
                    foreach (var aliOrder in result.data.Orders)
                    {
                        var order = await _order.ПолучитьOrderByMarketplaceId(marketplaceId, aliOrder.Id);
                        if (order == null)
                        {
                            //new order
                            if ((aliOrder.Order_display_status == AliExpressClasses.OrderDisplayStatus.WaitSendGoods) ||
                                (aliOrder.Order_display_status == AliExpressClasses.OrderDisplayStatus.PartialSendGoods))
                            {
                                var номенклатураCodes = aliOrder.Order_lines?.Select(x => x.Sku_code.Decode(encoding)).ToList();
                                var разрешенныеФирмы = await _фирма.ПолучитьСписокРазрешенныхФирмAsync(firmaId);
                                var списокСкладов = await _склад.ПолучитьСкладIdОстатковMarketplace();
                                var НоменклатураList = await _номенклатура.ПолучитьСвободныеОстатки(
                                    разрешенныеФирмы,
                                    списокСкладов,
                                    номенклатураCodes,
                                    true);
                                var nomQuantums = await _номенклатура.ПолучитьКвант(номенклатураCodes, cancellationToken);
                                bool нетВНаличие = НоменклатураList.Count == 0;
                                foreach (var номенклатура in НоменклатураList)
                                {
                                    decimal остаток = номенклатура.Остатки
                                                .Sum(z => z.СвободныйОстаток) / номенклатура.Единица.Коэффициент;
                                    var quantum = (int)nomQuantums.Where(q => q.Key == номенклатура.Id).Select(q => q.Value).FirstOrDefault();
                                    if (quantum == 0)
                                        quantum = 1;
                                    var asked = aliOrder.Order_lines?
                                                        .Where(b => b.Sku_code.Decode(encoding) == номенклатура.Code)
                                                        .Select(c => c.Quantity * quantum)
                                                        .FirstOrDefault();
                                    if (остаток < asked)
                                    {
                                        нетВНаличие = true;
                                        break;
                                    }
                                }
                                if (нетВНаличие)
                                {
                                    _logger.LogError("AliExpress запуск процедуры отмены");
                                    continue;
                                }
                                var orderItems = new List<OrderItem>();
                                foreach (var item in aliOrder.Order_lines)
                                {
                                    decimal цена = 0;
                                    if (item.Item_price > 0)
                                        цена = item.Item_price;
                                    else if (item.Total_amount > 0)
                                        цена = item.Total_amount / item.Quantity;
                                    orderItems.Add(new OrderItem
                                    {
                                        Id = item.Item_id,
                                        НоменклатураId = item.Sku_code.Decode(encoding),
                                        Количество = item.Quantity,
                                        Цена = цена,
                                    });
                                }
                                DateTime shipmentDate;
                                if (aliOrder.Cut_off_date > DateTime.MinValue)
                                    shipmentDate = aliOrder.Cut_off_date.AddDays(await _склад.ЭтоРабочийДень(Common.SkladEkran, -1, aliOrder.Cut_off_date));
                                else
                                    shipmentDate = DateTime.Now.AddDays(await _склад.ЭтоРабочийДень(Common.SkladEkran, 1));
                                long deliveryServiceId = aliOrder.Logistic_orders?.Select(x => x.Id).FirstOrDefault() ?? 0;
                                string deliveryServiceName = aliOrder.Logistic_orders?.Select(x => x.Track_number).FirstOrDefault() ?? "";
                                await _docService.NewOrder(
                                   "ALIEXPRESS",
                                   firmaId,
                                   customerId,
                                   dogovorId,
                                   authorization,
                                   encoding,
                                   Common.SkladEkran,
                                   string.Join(", ", aliOrder.Order_lines?.Select(x => x.Buyer_comment)),
                                   deliveryServiceId.ToString(),
                                   deliveryServiceName,
                                   StinDeliveryPartnerType.ALIEXPRESS_LOGISTIC,
                                   StinDeliveryType.DELIVERY,
                                   (double)aliOrder.Pre_split_postings?.Sum(x => x.Delivery_fee),
                                   0,
                                   StinPaymentType.NotFound,
                                   StinPaymentMethod.NotFound,
                                   "0",
                                   "",
                                   null,
                                   aliOrder.Id,
                                   aliOrder.Id,
                                   shipmentDate,
                                   orderItems,
                                   cancellationToken);
                                //if (aliOrder.Logistic_orders?.Count == 0)
                                //    await CreateLogisticsOrder(order.Id, aliOrder, authToken, cancellationToken);
                            }
                        }
                        else
                        {
                            var logisticsDelivering = new List<AliExpressClasses.LogisticsStatus> 
                            {
                                AliExpressClasses.LogisticsStatus.OrderReceivedFromSeller,
                                AliExpressClasses.LogisticsStatus.CrossDocSorting,
                                AliExpressClasses.LogisticsStatus.CrossDocSent,
                                AliExpressClasses.LogisticsStatus.ProviderPostingReceive,
                                AliExpressClasses.LogisticsStatus.ProviderPostingLeftTheReception,
                                AliExpressClasses.LogisticsStatus.ProviderPostingArrivedAtSorting,
                                AliExpressClasses.LogisticsStatus.ProviderPostingSorting,
                                AliExpressClasses.LogisticsStatus.ProviderPostingLeftTheSorting,
                                AliExpressClasses.LogisticsStatus.ProviderPostingArrived,
                                //AliExpressClasses.LogisticsStatus.ProviderPostingDelivered,
                                AliExpressClasses.LogisticsStatus.ProviderPostingUnsuccessfulAttemptOfDelivery,
                                AliExpressClasses.LogisticsStatus.ProviderPostingTemporaryStorage
                            };
                            var logisticsDelivered = new List<AliExpressClasses.LogisticsStatus>
                            {
                                AliExpressClasses.LogisticsStatus.ProviderPostingDelivered
                            };
                            if ((((aliOrder.Status == AliExpressClasses.OrderStatus.Finished) &&
                                !string.IsNullOrEmpty(aliOrder.Finish_reason) &&
                                ((aliOrder.Finish_reason == "ConfirmedByBuyer") ||
                                 (aliOrder.Finish_reason == "AutoConfirm") ||
                                 (aliOrder.Finish_reason == "ConfirmedByLogistic"))) || 
                                ((aliOrder.Logistic_orders?.Count > 0) && (aliOrder.Logistic_orders?.All(x => logisticsDelivered.Contains(x.Status)) ?? false))) &&
                                ((order.InternalStatus == 14) || (order.InternalStatus == 16)) && periodOpened && !_sleepPeriods.Any(x => x.IsSleeping()))
                            {
                                await _docService.OrderFromTransferDeliveried(order);
                            }
                            else if ((aliOrder.Logistic_orders?.Count > 0) && (aliOrder.Logistic_orders?.All(x => logisticsDelivering.Contains(x.Status)) ?? false) &&
                                (order.InternalStatus < 14) && (order.InternalStatus != 6) && (order.InternalStatus != 5) && periodOpened && !_sleepPeriods.Any(x => x.IsSleeping()))
                            {
                                await _docService.OrderDeliveried(order, true);
                            }
                            else if ((aliOrder.Order_display_status == AliExpressClasses.OrderDisplayStatus.WaitSendGoods) ||
                                 (aliOrder.Order_display_status == AliExpressClasses.OrderDisplayStatus.PartialSendGoods))
                            {
                                if (order.InternalStatus == 0)
                                {
                                    DateTime shipmentDate = aliOrder.Cut_off_date;
                                    if (aliOrder.Cut_off_date > DateTime.MinValue)
                                        shipmentDate = aliOrder.Cut_off_date.AddDays(await _склад.ЭтоРабочийДень(Common.SkladEkran, -1, aliOrder.Cut_off_date));
                                    else
                                        shipmentDate = DateTime.Now.AddDays(await _склад.ЭтоРабочийДень(Common.SkladEkran, 1));
                                    if (order.ShipmentDate != shipmentDate)
                                    {
                                        order.ShipmentDate = shipmentDate;
                                        await _order.ОбновитьOrderShipmentDate(order.Id, shipmentDate);
                                        await _docService.ОбновитьНомерМаршрута(order);
                                    }
                                    await _order.ОбновитьOrderStatus(order.Id, 8);
                                }
                                if (aliOrder.Logistic_orders?.Count == 0)
                                    await CreateLogisticsOrder(firmaId, order.Id, aliOrder, authToken, cancellationToken);
                            }
                            else if (((aliOrder.Status == AliExpressClasses.OrderStatus.Finished) || (aliOrder.Status == AliExpressClasses.OrderStatus.Cancelled)) &&
                                !string.IsNullOrEmpty(aliOrder.Finish_reason) &&
                                ((aliOrder.Finish_reason == "PaymentTimeout") || (aliOrder.Finish_reason == "CancelledByBuyer") ||
                                (aliOrder.Finish_reason == "SecurityClose") || (aliOrder.Finish_reason == "BuyerDoesNotWantOrder") ||
                                (aliOrder.Finish_reason == "BuyerChangeLogistic") || (aliOrder.Finish_reason == "BuyerCannotPayment") ||
                                (aliOrder.Finish_reason == "BuyerOtherReasons") || (aliOrder.Finish_reason == "BuyerCannotContactSeller") ||
                                (aliOrder.Finish_reason == "BuyerChangeCoupon") || (aliOrder.Finish_reason == "BuyerChangeMailAddress")) &&
                                (order.InternalStatus != 5) && (order.InternalStatus != 6) && (order.InternalStatus != 14) && (order.InternalStatus != 16))
                            {
                                await _docService.OrderCancelled(order);
                            }
                        }
                    }
                }
            }
        }
        private async Task GetAliExpressOrdersGlobal(string appKey, string appSecret, string authorization,
            string marketplaceId,
            string firmaId, string customerId, string dogovorId, EncodeVersion encoding,
            CancellationToken cancellationToken)
        {
            //await AliExpressClasses.Functions.GetLogisticDetails(_httpService, appKey, appSecret, authorization,
            //    cancellationToken);
            int limit = 20;
            int pageNumber = 0;
            bool nextPage = true;
            while (nextPage)
            {
                pageNumber++;
                nextPage = false;
                var result = await AliExpressClasses.Functions.GetOrdersGlobal(_httpService, appKey, appSecret, authorization,
                pageNumber, limit,
                cancellationToken);
                if (!string.IsNullOrEmpty(result.Item2))
                    _logger.LogError(result.Item2);
                if (result.Item1 != null)
                {
                    nextPage = result.Item1.Total_page > result.Item1.Current_page;
                    if ((result.Item1.Target_list != null) && 
                        (result.Item1.Target_list.Order_dto != null) &&
                        (result.Item1.Target_list.Order_dto.Count > 0))
                    {
                        foreach (var orderDto in result.Item1.Target_list.Order_dto)
                        {
                            var order = await _order.ПолучитьOrderByMarketplaceId(marketplaceId, orderDto.Order_id.ToString());
                            if (order == null)
                            {
                                if ((orderDto.Order_status == AliExpressClasses.OrderStatus.PLACE_ORDER_SUCCESS) ||
                                    (orderDto.Order_status == AliExpressClasses.OrderStatus.WAIT_SELLER_SEND_GOODS) ||
                                    (orderDto.Order_status == AliExpressClasses.OrderStatus.SELLER_PART_SEND_GOODS) ||
                                    (orderDto.Order_status == AliExpressClasses.OrderStatus.WAIT_SELLER_EXAMINE_MONEY))
                                {
                                    if ((orderDto.Product_list != null) && (orderDto.Product_list.Order_product_dto != null) &&
                                        (orderDto.Product_list.Order_product_dto.Count > 0))
                                    {
                                        //create new order
                                        var номенклатураCodes = orderDto.Product_list.Order_product_dto.Select(x => x.Sku_code.Decode(encoding)).ToList();
                                        var разрешенныеФирмы = await _фирма.ПолучитьСписокРазрешенныхФирмAsync(firmaId);
                                        var списокСкладов = await _склад.ПолучитьСкладIdОстатковMarketplace();
                                        var НоменклатураList = await _номенклатура.ПолучитьСвободныеОстатки(
                                            разрешенныеФирмы,
                                            списокСкладов,
                                            номенклатураCodes,
                                            true);
                                        var nomQuantums = await _номенклатура.ПолучитьКвант(номенклатураCodes, cancellationToken);
                                        bool нетВНаличие = false;
                                        foreach (var номенклатура in НоменклатураList)
                                        {
                                            decimal остаток = номенклатура.Остатки
                                                        .Sum(z => z.СвободныйОстаток) / номенклатура.Единица.Коэффициент;
                                            var quantum = (int)nomQuantums.Where(q => q.Key == номенклатура.Id).Select(q => q.Value).FirstOrDefault();
                                            if (quantum == 0)
                                                quantum = 1;
                                            var asked = orderDto.Product_list.Order_product_dto
                                                                .Where(b => b.Sku_code.Decode(encoding) == номенклатура.Code)
                                                                .Select(c => c.Product_count * quantum)
                                                                .FirstOrDefault();
                                            if (остаток < asked)
                                            {
                                                нетВНаличие = true;
                                                break;
                                            }
                                        }
                                        if (нетВНаличие)
                                        {
                                            _logger.LogError("AliExpress запуск процедуры отмены");
                                            //запуск процедуры отмены
                                            //var cancelResult = await OzonClasses.OzonOperators.CancelOrder(_httpService, clientId, authToken,
                                            //    posting.Posting_number, 352, "Product is out of stock", null, stoppingToken);
                                            //if (!string.IsNullOrEmpty(cancelResult))
                                            //    _logger.LogError(cancelResult);
                                            continue;
                                        }
                                        var orderItems = new List<OrderItem>();
                                        foreach (var item in orderDto.Product_list.Order_product_dto)
                                        {
                                            decimal цена = 0;
                                            if (item.Product_unit_price != null)
                                                цена = Convert.ToDecimal(item.Product_unit_price.Amount, new System.Globalization.NumberFormatInfo { NumberDecimalSeparator = "." });
                                            else if (item.Total_product_amount != null)
                                                цена = Convert.ToDecimal(item.Total_product_amount.Amount, new System.Globalization.NumberFormatInfo { NumberDecimalSeparator = "." }) / item.Product_count;
                                            orderItems.Add(new OrderItem
                                            {
                                                Id = item.Product_id.ToString(),
                                                НоменклатураId = item.Sku_code.Decode(encoding),
                                                Количество = item.Product_count,
                                                Цена = цена,
                                            });
                                        }
                                        DateTime shipmentDate = DateTime.MinValue;
                                        if (orderDto.Order_status == AliExpressClasses.OrderStatus.PLACE_ORDER_SUCCESS)
                                        {
                                            shipmentDate = DateTime.Now.AddDays(await _склад.ЭтоРабочийДень(Common.SkladEkran, 1));
                                        }
                                        else
                                        {
                                            var limitDate = DateTime.Now.AddMilliseconds(orderDto.Timeout_left_time).Date;
                                            int колДнейПередLimitDate = await _склад.ЭтоРабочийДень(Common.SkladEkran, -1, limitDate);
                                            shipmentDate = limitDate.AddDays(колДнейПередLimitDate);
                                        }
                                        await _docService.NewOrder(
                                           "ALIEXPRESS",
                                           firmaId,
                                           customerId,
                                           dogovorId,
                                           authorization,
                                           encoding,
                                           Common.SkladEkran,
                                           "",
                                           "0",
                                           "",
                                           StinDeliveryPartnerType.ALIEXPRESS_LOGISTIC,
                                           StinDeliveryType.DELIVERY,
                                           0,
                                           0,
                                           StinPaymentType.NotFound,
                                           StinPaymentMethod.NotFound,
                                           "0",
                                           "",
                                           null,
                                           orderDto.Order_id.ToString(),
                                           orderDto.Order_id.ToString(),
                                           shipmentDate,
                                           orderItems,
                                           cancellationToken);
                                    }
                                    else
                                    {
                                        _logger.LogError("Aliexpress GetOrdersGlobalResponse : No products in response");
                                    }
                                }
                            }
                            else
                            {
                                if (((orderDto.Order_status == AliExpressClasses.OrderStatus.FINISH) &&
                                    !string.IsNullOrEmpty(orderDto.End_reason) &&
                                    (orderDto.End_reason == "buyer_confirm_goods") &&
                                    (order.InternalStatus != 6)) ||
                                    ((orderDto.Order_status == AliExpressClasses.OrderStatus.WAIT_BUYER_ACCEPT_GOODS) &&
                                     (order.InternalStatus != 6)))
                                {
                                    await _docService.OrderDeliveried(order);
                                }
                                else if (((orderDto.Order_status == AliExpressClasses.OrderStatus.WAIT_SELLER_SEND_GOODS) ||
                                     (orderDto.Order_status == AliExpressClasses.OrderStatus.SELLER_PART_SEND_GOODS)) &&
                                    (order.InternalStatus == 0))
                                {
                                    var limitDate = DateTime.Now.AddMilliseconds(orderDto.Timeout_left_time).Date;
                                    int колДнейПередLimitDate = await _склад.ЭтоРабочийДень(Common.SkladEkran, -1, limitDate);
                                    var shipmentDate = limitDate.AddDays(колДнейПередLimitDate);
                                    if (order.ShipmentDate != shipmentDate)
                                    {
                                        order.ShipmentDate = shipmentDate;
                                        await _order.ОбновитьOrderShipmentDate(order.Id, shipmentDate);
                                        await _docService.ОбновитьНомерМаршрута(order);
                                    }
                                    await _order.ОбновитьOrderStatus(order.Id, 8);
                                }
                                else if ((orderDto.Order_status == AliExpressClasses.OrderStatus.FINISH) &&
                                    !string.IsNullOrEmpty(orderDto.End_reason) &&
                                    ((orderDto.End_reason == "pay_timeout") || (orderDto.End_reason == "buyer_cancel_notpay_order")) &&
                                    (order.InternalStatus != 5) && (order.InternalStatus != 6))
                                {
                                    await _docService.OrderCancelled(order);
                                }
                            }
                        }
                    }
                }
            }
        }
        private async Task GetWbNewOrders(string authToken, string id, string authorization, string firmaId, string customerId, string dogovorId, EncodeVersion encoding, CancellationToken stoppingToken)
        {
            var result = await WbClasses.Functions.GetNewOrders(_httpService, _firmProxy[firmaId], authToken, stoppingToken);
            if (!string.IsNullOrEmpty(result.error))
                _logger.LogError(result.error);
            if (result.data?.Orders?.Count > 0)
            {
                using var scope = _serviceProvider.CreateScope();
                var wbHelper = scope.ServiceProvider.GetRequiredService<IWildberriesHelper>();
                foreach (var wbOrder in result.data.Orders)
                {
                    var order = await _order.ПолучитьOrderByMarketplaceId(id, wbOrder.Id.ToString());
                    if (order == null)
                    {
                        var разрешенныеФирмы = await _фирма.ПолучитьСписокРазрешенныхФирмAsync(firmaId);
                        var списокСкладов = await _склад.ПолучитьСкладIdОстатковMarketplace();
                        var номенклатураIds = await _номенклатура.GetНоменклатураIdByListBarcodeAsync(wbOrder.Skus, stoppingToken);
                        var НоменклатураList = await _номенклатура.ПолучитьСвободныеОстатки(
                            разрешенныеФирмы,
                            списокСкладов,
                            номенклатураIds,
                            false);
                        bool нетВНаличие = НоменклатураList.Count == 0;
                        var marketplace = await _marketplace.ПолучитьMarketplace(id);
                        var orderItems = new List<OrderItem>();
                        foreach (var номенклатура in НоменклатураList)
                        {
                            var nomQuant = await _номенклатура.ПолучитьКвант(номенклатура.Id, stoppingToken);
                            decimal остаток = номенклатура.Остатки
                                        .Sum(z => z.СвободныйОстаток) / номенклатура.Единица.Коэффициент;
                            var asked = 1 * nomQuant;
                            if (остаток < asked)
                            {
                                нетВНаличие = true;
                                break;
                            }
                            var markUsingParams = _marketplace.GetMarketUsingParams(id, номенклатура.Id);
                            var priceMarketplace = GetPriceMarketplace(номенклатура.Цена.Розничная, номенклатура.Цена.РозСП, номенклатура.Цена.Закупочная, 1.2m,
                                markUsingParams.fixPrice, markUsingParams.coeff, marketplace.КоэфПроверкиЦен, markUsingParams.deltaPrice)
                                * nomQuant;
                            orderItems.Add(new OrderItem
                            {
                                Id = номенклатура.Единица.Barcode,
                                НоменклатураId = номенклатура.Code,
                                Количество = asked,
                                Цена = priceMarketplace > 0 ? priceMarketplace : wbOrder.Price,
                                ЦенаСоСкидкой = wbOrder.Price,
                                Вознаграждение = (priceMarketplace > 0 ? priceMarketplace : wbOrder.Price) - (wbOrder.Price),
                            });
                        }
                        if (нетВНаличие)
                        {
                            //запуск процедуры отмены
                            var cancelResult = await WbClasses.Functions.CancelOrder(_httpService, _firmProxy[firmaId], authToken,
                                wbOrder.Id.ToString(), stoppingToken);
                            if (!string.IsNullOrEmpty(cancelResult.error))
                                _logger.LogError(cancelResult.error);
                            if (cancelResult.success)
                                _logger.LogError("Wb order: " + wbOrder.Id.ToString() + " cancelled");
                            else
                                _logger.LogError("Wb order: " + wbOrder.Id.ToString() + " can't be cancelled");
                            continue;
                        }
                        DateTime shipmentDate = await wbHelper.GetActiveSupplyShipmentDate(_firmProxy[firmaId], authToken, id, stoppingToken);
                        if (shipmentDate <= Common.min1cDate)
                        {
                            if (!TimeSpan.TryParse("09:00", out TimeSpan limitTime))
                                limitTime = TimeSpan.MinValue;
                            var leavingDate = DateTime.Now.TimeOfDay > limitTime ? 1 : 0;
                            shipmentDate = DateTime.Today.AddDays(await _склад.ЭтоРабочийДень(Common.SkladEkran, leavingDate));
                        }
                        string deliveryServiceName = ""; //wbOrder.ScOfficesNames.FirstOrDefault() ?? string.Empty;
                        var address = new OrderRecipientAddress
                        {
                            City = wbOrder.Address?.City,
                            Street = wbOrder.Address?.Street,
                            House = wbOrder.Address?.Home,
                            Apartment = wbOrder.Address?.Flat,
                            Entrance = wbOrder.Address?.Entrance,
                            Subway = "",
                            Block = "",
                            Country = "",
                            Entryphone = "",
                            Floor = "",
                            Postcode = ""
                        };
                        await _docService.NewOrder(
                           "WILDBERRIES",
                           firmaId,
                           customerId,
                           dogovorId,
                           authorization,
                           encoding,
                           Common.SkladEkran,
                           "",
                           "0",
                           deliveryServiceName,
                           StinDeliveryPartnerType.WILDBERRIES,
                           StinDeliveryType.DELIVERY,
                           0,
                           0,
                           StinPaymentType.NotFound,
                           StinPaymentMethod.NotFound,
                           "0",
                           "",
                           address,
                           wbOrder.Id.ToString(),
                           wbOrder.Id.ToString(),
                           shipmentDate,
                           orderItems,
                           stoppingToken);
                    }
                }
            }
        }
        private async Task RefreshWbOrders(string authToken, string marketplaceId, string firmaId, CancellationToken cancellationToken)
        {
            bool periodOpened = !_заявкаПокупателя.NeedToOpenPeriod();
            var activeOrders = _context.Sc13994s
                .Where(x => (x.Sp13982 > 0) && (x.Sp13982 < 15) && (x.Sp13982 != 5) && (x.Sp13982 != 6) && 
                    ((StinDeliveryPartnerType)x.Sp13985 == StinDeliveryPartnerType.WILDBERRIES) &&
                    (x.Sp14038 == marketplaceId))
                .Select(x => Convert.ToInt64(x.Code.Trim()))
                .OrderBy(x => x);
            int limit = 1000;
            for (int i = 0; i < activeOrders.Count(); i = i + limit)
            {
                var data = await activeOrders.Skip(i).Take(limit).ToListAsync(cancellationToken);
                if (data?.Count > 0)
                {
                    var result = await WbClasses.Functions.GetOrderStatuses(_httpService, _firmProxy[firmaId], authToken, data, cancellationToken);
                    if (!string.IsNullOrEmpty(result.error))
                        _logger.LogError(result.error);
                    var cancelOrderIds = result.orders?.Where(x => (x.Value == WbClasses.WbStatus.canceled) || (x.Value == WbClasses.WbStatus.canceled_by_client))
                        .Select(x => x.Key);
                    foreach (var wbOrderId in cancelOrderIds) 
                    {
                        var order = await _order.ПолучитьOrderByMarketplaceId(marketplaceId, wbOrderId.ToString());
                        if ((order != null) && (order.InternalStatus != 5) && (order.InternalStatus != 6) && (order.InternalStatus != 14) && (order.InternalStatus != 16))
                            await _docService.OrderCancelled(order);
                    }
                    if (periodOpened && !_sleepPeriods.Any(x => x.IsSleeping()))
                    {
                        var deliveringOrderIds = result.orders?.Where(x => x.Value == WbClasses.WbStatus.sorted)
                            .Select(x => x.Key);
                        foreach (var wbOrderId in deliveringOrderIds)
                        {
                            var order = await _order.ПолучитьOrderByMarketplaceId(marketplaceId, wbOrderId.ToString());
                            if ((order != null) && (order.InternalStatus != 5) && (order.InternalStatus != 6) && (order.InternalStatus != 14))
                                await _docService.OrderDeliveried(order, true);
                        }
                        var deliveredOrderIds = result.orders?.Where(x => x.Value == WbClasses.WbStatus.sold)
                            .Select(x => x.Key);
                        foreach (var wbOrderId in deliveredOrderIds)
                        {
                            var order = await _order.ПолучитьOrderByMarketplaceId(marketplaceId, wbOrderId.ToString());
                            if ((order != null) && ((order.InternalStatus == 14) || (order.InternalStatus == 16)))
                                await _docService.OrderFromTransferDeliveried(order);
                        }
                    }
                }
            }
        }
        private async Task GetOzonNewOrders(string clientId, string authToken, string id, string firmaId, string customerId, string dogovorId, EncodeVersion encoding, CancellationToken stoppingToken)
        {
            if (int.TryParse(_configuration["Orderer:maxPerRequest"], out int maxPerRequest))
                maxPerRequest = Math.Max(maxPerRequest, 1);
            else
                maxPerRequest = 100;
            var result = await OzonClasses.OzonOperators.UnfulfilledOrders(_httpService, _firmProxy[firmaId], clientId, authToken,
                maxPerRequest,
                stoppingToken);
            if (result.Item2 != null)
            {
                _logger.LogError(result.Item2);
            }
            if (result.Item1 != null)
            {
                foreach (var posting in result.Item1)
                {
                    if ((posting.Products != null) && (posting.Products.Count > 0))
                    {
                        bool needCreateDoc = await _order.ПолучитьOrderByMarketplaceId(id, posting.Posting_number) == null;
                        if (needCreateDoc)
                        {
                            var разрешенныеФирмы = await _фирма.ПолучитьСписокРазрешенныхФирмAsync(firmaId);
                            var списокСкладов = await _склад.ПолучитьСкладIdОстатковMarketplace();
                            var номенклатураCodes = posting.Products.Select(x => x.Offer_id.Decode(encoding)).ToList();
                            var НоменклатураList = await _номенклатура.ПолучитьСвободныеОстатки(
                                разрешенныеФирмы,
                                списокСкладов,
                                номенклатураCodes,
                                true);
                            var nomQuantums = await _номенклатура.ПолучитьКвант(номенклатураCodes, stoppingToken);
                            bool нетВНаличие = НоменклатураList.Count == 0;
                            foreach (var номенклатура in НоменклатураList)
                            {
                                decimal остаток = номенклатура.Остатки
                                            .Sum(z => z.СвободныйОстаток) / номенклатура.Единица.Коэффициент;
                                var quantum = (int)nomQuantums.Where(q => q.Key == номенклатура.Id).Select(q => q.Value).FirstOrDefault();
                                if (quantum == 0)
                                    quantum = 1;
                                var asked = posting.Products
                                                    .Where(b => b.Offer_id.Decode(encoding) == номенклатура.Code)
                                                    .Select(c => c.Quantity * quantum)
                                                    .FirstOrDefault();
                                if (остаток < asked)
                                {
                                    нетВНаличие = true;
                                    break;
                                }
                            }
                            if (нетВНаличие)
                            {
                                //запуск процедуры отмены
                                var cancelResult = await OzonClasses.OzonOperators.CancelOrder(_httpService, _firmProxy[firmaId], clientId, authToken,
                                    posting.Posting_number, 352, "Product is out of stock", null, stoppingToken);
                                if (!string.IsNullOrEmpty(cancelResult))
                                    _logger.LogError(cancelResult);
                                continue;
                            }
                        }
                        var orderItems = new List<OrderItem>();
                        if (posting.Status == Enum.GetName(OzonClasses.OrderStatus.awaiting_packaging))
                        {
                            orderItems = await OzonMakeOrdersPosting(_firmProxy[firmaId], clientId, authToken, encoding, posting, stoppingToken);
                        }
                        else
                        {
                            //заказ уже awaiting_deliver или delivering
                            //..на всякий случай проверим, что уже создали заказ

                            foreach (var item in posting.Products)
                            {
                                orderItems.Add(new OrderItem
                                {
                                    ИдентификаторПоставщика = posting.Posting_number,
                                    Id = item.Sku.ToString(),
                                    НоменклатураId = item.Offer_id.Decode(encoding),
                                    Количество = item.Quantity,
                                    Цена = Convert.ToDecimal(item.Price, new System.Globalization.NumberFormatInfo { NumberDecimalSeparator = "." }),
                                });
                            }
                        }
                        if (needCreateDoc)
                        {
                            foreach (var orderItem in orderItems.GroupBy(x => x.ИдентификаторПоставщика))
                            {
                                await _docService.NewOrder(
                                   "OZON",
                                   firmaId,
                                   customerId,
                                   dogovorId,
                                   authToken,
                                   encoding,
                                   Common.SkladEkran,
                                   "",
                                   posting.Delivery_method != null ? posting.Delivery_method.Id.ToString() : "0",
                                   posting.Delivery_method != null ? posting.Delivery_method.Name : "",
                                   StinDeliveryPartnerType.OZON_LOGISTIC,
                                   StinDeliveryType.DELIVERY,
                                   0,
                                   0,
                                   StinPaymentType.NotFound,
                                   StinPaymentMethod.NotFound,
                                   "0",
                                   posting.Analytics_data != null ? posting.Analytics_data.Region : "",
                                   posting.Analytics_data != null ? new OrderRecipientAddress { City = posting.Analytics_data.City } : null,
                                   posting.Order_number,
                                   orderItem.Key,
                                   posting.Shipment_date,
                                   orderItem.Select(x => x).ToList(),
                                   stoppingToken);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogError("OzonUnfulfilledResponse : No products in response");
                    }
                }
            }
        }
        private async Task<List<OrderItem>> OzonMakeOrdersPosting(string proxyHost, string clientId, string authToken, EncodeVersion encoding, OzonClasses.FbsPosting posting, CancellationToken stoppingToken)
        {
            var resultCreateOrGetExemplar = await OzonClasses.OzonOperators.CreateOrGetExemplar(_httpService, proxyHost, clientId, authToken,
                posting.Posting_number,
                stoppingToken);
            if (!string.IsNullOrEmpty(resultCreateOrGetExemplar.error))
            {
                _logger.LogError(resultCreateOrGetExemplar.error);
            }
            var orderItems = new List<OrderItem>();
            var postingPackageData = new List<OzonClasses.PostingPackage>();
            var totalPieces = 0;
            foreach (var product in posting.Products)
            {
                var code = product.Offer_id.Decode(encoding);
                string nomId = await _номенклатура.GetIdByCode(code);
                decimal колМест = await _номенклатура.ПолучитьКоличествоМест(nomId);
                колМест = колМест == 0 ? 1 : колМест;
                totalPieces += (int)колМест;
                string gtd = "";
                bool gtdNeeded = false;
                if (posting.Requirements != null)
                {
                    if ((posting.Requirements.Products_requiring_country != null) &&
                        (posting.Requirements.Products_requiring_country.Count > 0) &&
                        (posting.Requirements.Products_requiring_country.Contains(product.Sku)))
                    {
                        string страна = "";
                        if (!string.IsNullOrWhiteSpace(nomId))
                        {
                            var странаId = await _context.ПолучитьЗначениеПериодическогоРеквизита(nomId, 5012);
                            if (!string.IsNullOrWhiteSpace(странаId))
                                страна = await _context.Sc566s.Where(x => x.Id == странаId).Select(x => x.Descr.ToUpper().Trim()).FirstOrDefaultAsync();
                        }
                        string countryCode = "RU";
                        switch (страна)
                        {
                            case "": countryCode = "RU"; break;
                            case "РОССИЯ": countryCode = "RU"; break;
                            case "КИТАЙ": countryCode = "CN"; break;
                            default:
                                {
                                    var countryCodeResult = await OzonClasses.OzonOperators.GetCountryCode(
                                        _httpService, proxyHost, clientId, authToken, страна, stoppingToken);
                                    if (countryCodeResult.Item2 != null && !string.IsNullOrEmpty(countryCodeResult.Item2))
                                        _logger.LogError(countryCodeResult.Item2);
                                    if (string.IsNullOrEmpty(countryCodeResult.Item1))
                                        countryCode = "RU";
                                    else
                                        countryCode = countryCodeResult.Item1;
                                    break;
                                }
                        }
                        var setCountryResult = await OzonClasses.OzonOperators.SetCountryCode(
                            _httpService, proxyHost, clientId, authToken,
                            posting.Posting_number,
                            product.Sku,
                            string.IsNullOrWhiteSpace(countryCode) ? "RU" : countryCode,
                            stoppingToken);
                        if (setCountryResult.Item2 != null && !string.IsNullOrEmpty(setCountryResult.Item2))
                            _logger.LogError(setCountryResult.Item2);
                        if (setCountryResult.Item1 != null)
                            gtdNeeded = (bool)setCountryResult.Item1;
                    }

                    if (gtdNeeded || ((posting.Requirements.Products_requiring_gtd != null) &&
                        (posting.Requirements.Products_requiring_gtd.Count > 0) &&
                        (posting.Requirements.Products_requiring_gtd.Contains(product.Sku))))
                    {
                        var gtdId = await _context.ПолучитьЗначениеПериодическогоРеквизита(nomId, 5013);
                        if (!string.IsNullOrWhiteSpace(gtdId))
                        {
                            gtd = await _context.Sc568s.Where(x => x.Id == gtdId).Select(x => x.Descr.Trim()).FirstOrDefaultAsync();
                            var gtdInfo = gtd.Split("/", StringSplitOptions.TrimEntries);
                            if ((gtdInfo != null) && (gtdInfo.Length > 0))
                            {
                                string calcGtd = ""; 
                                foreach (var part in gtdInfo)
                                {
                                    if (!string.IsNullOrEmpty(calcGtd))
                                        calcGtd += "/";
                                    calcGtd += part;
                                    if (calcGtd.Length >= 21 + calcGtd.Count(f => (f == '/')))
                                        break;
                                }
                                if (calcGtd.Length == 21 + calcGtd.Count(f => (f == '/')))
                                    gtd = calcGtd;
                            }
                        }
                    }
                }
                if (resultCreateOrGetExemplar.data != null 
                    && resultCreateOrGetExemplar.data.TryGetValue(product.Sku, out var productExemplarCreateOrGet)
                    && productExemplarCreateOrGet.Is_gtd_needed)
                {
                    var exemplarProducts = productExemplarCreateOrGet.Exemplars.Select(x => new OzonClasses.ProductExemplarRequest
                    {
                        Exemplars = new List<OzonClasses.ProductExemplarRequestItem>
                        {
                            new OzonClasses.ProductExemplarRequestItem
                            {
                                Exemplar_id = x.Exemplar_id,
                                Gtd = string.IsNullOrWhiteSpace(gtd) ? null : gtd,
                                Is_gtd_absent = string.IsNullOrWhiteSpace(gtd),
                                Is_rnpt_absent = true,
                                Mandatory_mark = null,
                                Rnpt = null,
                                Jw_uin = null,
                            }
                        },
                        Is_gtd_needed = string.IsNullOrWhiteSpace(gtd),
                        Is_mandatory_mark_needed = false,
                        Is_rnpt_needed = false,
                        Product_id = product.Sku,
                        Quantity = product.Quantity,
                    }).ToList();
                    if (exemplarProducts.Count > 0)
                    {
                        var resultExemplar = await OzonClasses.OzonOperators.SetExemplar(_httpService, proxyHost, clientId, authToken,
                            posting.Posting_number,
                            totalPieces,
                            exemplarProducts,
                            stoppingToken);
                        if (!string.IsNullOrEmpty(resultExemplar.error))
                        {
                            _logger.LogError(resultExemplar.error);
                        }
                    }
                }
                for (int i = 0; i < product.Quantity; i++)
                {
                    postingPackageData.Add(new OzonClasses.PostingPackage
                    {
                        Products = new List<OzonClasses.PostingPackageProduct>
                        {
                            new OzonClasses.PostingPackageProduct
                            {
                                //Exemplar_info = new List<OzonClasses.PostingExemplarInfo>
                                //{
                                //    new OzonClasses.PostingExemplarInfo
                                //    {
                                //        Gtd = string.IsNullOrWhiteSpace(gtd) ? null : gtd,
                                //        Is_gtd_absent = string.IsNullOrWhiteSpace(gtd),
                                //    }
                                //},
                                Product_id = product.Sku,
                                Quantity = 1
                            }
                        }
                    });
                }
            }
            var result = await OzonClasses.OzonOperators.SetOrderPosting(_httpService, proxyHost, clientId, authToken,
                posting.Posting_number,
                postingPackageData,
                stoppingToken);
            if (result.Item2 != null && !string.IsNullOrEmpty(result.Item2))
            {
                _logger.LogError(result.Item2);
            }
            if (result.Item1 != null)
            {
                foreach (var data in result.Item1)
                {
                    foreach (var item in data.Products)
                    {
                        orderItems.Add(new OrderItem
                        {
                            ИдентификаторПоставщика = data.Posting_number,
                            Id = item.Sku.ToString(),
                            НоменклатураId = item.Offer_id.Decode(encoding),
                            Количество = item.Quantity,
                            Цена = Convert.ToDecimal(item.Price, new System.Globalization.NumberFormatInfo { NumberDecimalSeparator = "." }),
                        });
                    }
                }
            }
            return orderItems;
        }
        private async Task GetYandexDeliveredOrders(string campaignId, string clientId, string authToken, string marketplaceId, string firmaId, CancellationToken cancellationToken)
        {
            string pageToken = "";
            bool nextPage = true;
            while (nextPage)
            {
                nextPage = false;
                var result = await YandexClasses.YandexOperators.OrdersStats(_httpService,
                    _firmProxy[firmaId],
                    campaignId,
                    clientId,
                    authToken,
                    new List<string> { "DELIVERED" },
                    DateTime.Today.AddDays(-60),
                    200,
                    pageToken,
                    cancellationToken);
                pageToken = result.NextPageToken;
                nextPage = !string.IsNullOrEmpty(pageToken);
                if (result.Orders?.Count > 0)
                {
                    foreach (var detailOrder in result.Orders)
                    {
                        var order = await _order.ПолучитьOrderByMarketplaceId(marketplaceId, detailOrder.Id.ToString());
                        if ((order != null) && (detailOrder.Status == YandexClasses.StatusYandex.DELIVERED) &&
                            (order.InternalStatus == 14))
                        {
                            await _docService.OrderFromTransferDeliveried(order);
                        }
                    }
                }
            }
            //int pageNumber = 0;
            //bool nextPage = true;
            //while (nextPage)
            //{
            //    pageNumber++;
            //    nextPage = false;
            //    var result = await YandexClasses.YandexOperators.OrdersList(_httpService,
            //        _firmProxy[firmaId],
            //        campaignId,
            //        clientId,
            //        authToken,
            //        "DELIVERED",
            //        DateTime.Today.AddDays(-60),
            //        pageNumber,
            //        cancellationToken);
            //    nextPage = result.NextPage;
            //    if (result.Orders?.Count > 0)
            //    {
            //        foreach (var detailOrder in result.Orders)
            //        {
            //            var order = await _order.ПолучитьOrderByMarketplaceId(marketplaceId, detailOrder.Id.ToString());
            //            if ((order != null) && (detailOrder.Status == YandexClasses.StatusYandex.DELIVERED) &&
            //                (order.InternalStatus == 14))
            //            {
            //                await _docService.OrderFromTransferDeliveried(order);
            //            }
            //        }
            //    }
            //}
        }
        private async Task GetYandexNewDeliveringOrders(string campaignId, string clientId, string authToken, string authorization,
            string marketplaceId,
            string firmaId, string customerId, string dogovorId, EncodeVersion encoding, 
            CancellationToken cancellationToken)
        {
            bool periodOpened = !_заявкаПокупателя.NeedToOpenPeriod();
            var readyToShipOrders = await ActiveOrders(marketplaceId, cancellationToken);
            var разрешенныеФирмы = await _фирма.ПолучитьСписокРазрешенныхФирмAsync(firmaId);
            var списокСкладов = await _склад.ПолучитьСкладIdОстатковMarketplace();
            int pageNumber = 0;
            bool nextPage = true;
            while (nextPage)
            {
                pageNumber++;
                nextPage = false;
                var result = await YandexClasses.YandexOperators.OrdersList(_httpService,
                    _firmProxy[firmaId],
                    campaignId,
                    clientId,
                    authToken,
                    "UNPAID",
                    DateTime.Today.AddDays(-1),
                    pageNumber,
                    cancellationToken);
                nextPage = result.NextPage;
                if (result.Orders?.Count > 0)
                {
                    foreach (var detailOrder in result.Orders)
                    {
                        var order = await _order.ПолучитьOrderByMarketplaceId(marketplaceId, detailOrder.Id.ToString());
                        if (order == null)
                        {
                            var номенклатураCodes = detailOrder.Items.Select(x => x.OfferId.Decode(encoding)).ToList();
                            var НоменклатураList = await _номенклатура.ПолучитьСвободныеОстатки(
                                разрешенныеФирмы,
                                списокСкладов,
                                номенклатураCodes,
                                true);
                            //var nomQuantums = await _номенклатура.ПолучитьКвант(номенклатураCodes, stoppingToken);
                            bool нетВНаличие = НоменклатураList.Count == 0;
                            foreach (var номенклатура in НоменклатураList)
                            {
                                decimal остаток = номенклатура.Остатки
                                            .Sum(z => z.СвободныйОстаток) / номенклатура.Единица.Коэффициент;
                                //var quantum = (int)nomQuantums.Where(q => q.Key == номенклатура.Id).Select(q => q.Value).FirstOrDefault();
                                //if (quantum == 0)
                                //    quantum = 1;
                                var asked = detailOrder.Items
                                                    .Where(b => b.OfferId.Decode(encoding) == номенклатура.Code)
                                                    .Select(c => c.Count)
                                                    .FirstOrDefault();
                                if (остаток < asked)
                                {
                                    нетВНаличие = true;
                                    break;
                                }
                            }
                            if (нетВНаличие)
                            {
                                _logger.LogError("Yandex запуск процедуры отмены");
                                //запуск процедуры отмены

                                //var cancelResult = await OzonClasses.OzonOperators.CancelOrder(_httpService, clientId, authToken,
                                //    posting.Posting_number, 352, "Product is out of stock", null, stoppingToken);
                                //if (!string.IsNullOrEmpty(cancelResult))
                                //    _logger.LogError(cancelResult);
                                continue;
                            }
                            var orderItems = new List<OrderItem>();
                            foreach (var item in detailOrder.Items)
                            {
                                orderItems.Add(new OrderItem
                                {
                                    Id = item.Id.ToString(),
                                    НоменклатураId = item.OfferId.Decode(encoding),
                                    Количество = item.Count,
                                    Цена = (decimal)item.Price,
                                    ЦенаСоСкидкой = (decimal)item.BuyerPrice,
                                    Вознаграждение = (decimal)item.Subsidy,
                                    Доставка = item.Delivery,
                                    ДопПараметры = item.Params,
                                    ИдентификаторПоставщика = item.FulfilmentShopId.ToString(),
                                    ИдентификаторСклада = item.WarehouseId.ToString(),
                                    ИдентификаторСкладаПартнера = item.PartnerWarehouseId
                                });
                            }
                            string складОтгрузкиId = detailOrder.Delivery?.Outlet?.Code.FormatTo1CId() ?? Common.SkladEkran;
                            string orderDeliveryShipmentId = "nothing";
                            DateTime orderShipmentDate = DateTime.MinValue;
                            if (detailOrder.Delivery.Shipments?.Count > 0)
                            {
                                orderDeliveryShipmentId = detailOrder.Delivery.Shipments[0].Id.ToString();
                                orderShipmentDate = detailOrder.Delivery.Shipments[0].ShipmentDate;
                            }
                            if (orderShipmentDate == DateTime.MinValue)
                            {
                                if (detailOrder.Delivery.Dates?.FromDate > DateTime.MinValue)
                                    orderShipmentDate = detailOrder.Delivery.Dates.FromDate;
                                else
                                    orderShipmentDate = DateTime.Today.AddDays(await _склад.ЭтоРабочийДень(складОтгрузкиId, 0));
                            }
                            OrderRecipientAddress address = detailOrder.Delivery.Address != null ? new OrderRecipientAddress
                            {
                                Postcode = detailOrder.Delivery.Address.Postcode ?? "",
                                Country = detailOrder.Delivery.Address.Country ?? "",
                                City = detailOrder.Delivery.Address.City ?? "",
                                Subway = detailOrder.Delivery.Address.Subway ?? "",
                                Street = detailOrder.Delivery.Address.Street ?? "",
                                House = detailOrder.Delivery.Address.House ?? "",
                                Block = detailOrder.Delivery.Address.Block ?? "",
                                Entrance = detailOrder.Delivery.Address.Entrance ?? "",
                                Entryphone = detailOrder.Delivery.Address.Entryphone ?? "",
                                Floor = detailOrder.Delivery.Address.Floor ?? "",
                                Apartment = detailOrder.Delivery.Address.Apartment ?? ""
                            } : null;
                            double deliveryPrice = 0;
                            double deliverySubsidy = 0;
                            if ((detailOrder.Delivery?.DeliveryPartnerType == YandexClasses.DeliveryPartnerType.SHOP) &&
                                (detailOrder.Delivery?.Type != YandexClasses.DeliveryType.PICKUP))
                            {
                                deliveryPrice = detailOrder.Delivery?.Price ?? 0 + detailOrder.Delivery?.Subsidy ?? 0;
                                deliverySubsidy = detailOrder.Delivery?.Subsidy ?? 0;
                            }
                            await _docService.NewOrder(
                                "YANDEX",
                                firmaId,
                                customerId,
                                dogovorId,
                                authorization,
                                encoding,
                                складОтгрузкиId,
                                detailOrder.Notes ?? "",
                                detailOrder.Delivery?.DeliveryServiceId ?? "0",
                                detailOrder.Delivery?.ServiceName ?? "",
                                (StinDeliveryPartnerType)detailOrder.Delivery?.DeliveryPartnerType,
                                (StinDeliveryType)detailOrder.Delivery?.Type,
                                deliveryPrice,
                                deliverySubsidy,
                                (StinPaymentType)detailOrder.PaymentType,
                                (StinPaymentMethod)detailOrder.PaymentMethod,
                                detailOrder.Delivery?.Region?.Id.ToString(),
                                detailOrder.Delivery?.Region?.Name ?? "",
                                address,
                                orderDeliveryShipmentId,
                                detailOrder.Id.ToString(),
                                orderShipmentDate,
                                orderItems,
                                cancellationToken);
                        }
                    }
                }
            }
            pageNumber = 0;
            nextPage = true;
            while (nextPage)
            {
                pageNumber++;
                nextPage = false;
                var result = await YandexClasses.YandexOperators.OrdersList(_httpService,
                    _firmProxy[firmaId],
                    campaignId, 
                    clientId, 
                    authToken,
                    "PROCESSING",
                    DateTime.Today.AddDays(-1),
                    pageNumber, 
                    cancellationToken);
                nextPage = result.NextPage;
                if (result.Orders?.Count > 0)
                {
                    foreach (var detailOrder in result.Orders)
                    {
                        var order = await _order.ПолучитьOrderByMarketplaceId(marketplaceId, detailOrder.Id.ToString());
                        if (order == null)
                        {
                            var номенклатураCodes = detailOrder.Items.Select(x => x.OfferId.Decode(encoding)).ToList();
                            var НоменклатураList = await _номенклатура.ПолучитьСвободныеОстатки(
                                разрешенныеФирмы,
                                списокСкладов,
                                номенклатураCodes,
                                true);
                            //var nomQuantums = await _номенклатура.ПолучитьКвант(номенклатураCodes, stoppingToken);
                            bool нетВНаличие = НоменклатураList.Count == 0;
                            foreach (var номенклатура in НоменклатураList)
                            {
                                decimal остаток = номенклатура.Остатки
                                            .Sum(z => z.СвободныйОстаток) / номенклатура.Единица.Коэффициент;
                                //var quantum = (int)nomQuantums.Where(q => q.Key == номенклатура.Id).Select(q => q.Value).FirstOrDefault();
                                //if (quantum == 0)
                                //    quantum = 1;
                                var asked = detailOrder.Items
                                                    .Where(b => b.OfferId.Decode(encoding) == номенклатура.Code)
                                                    .Select(c => c.Count)
                                                    .FirstOrDefault();
                                if (остаток < asked)
                                {
                                    нетВНаличие = true;
                                    break;
                                }
                            }
                            if (нетВНаличие)
                            {
                                _logger.LogError("Yandex запуск процедуры отмены");
                                //запуск процедуры отмены

                                //var cancelResult = await OzonClasses.OzonOperators.CancelOrder(_httpService, clientId, authToken,
                                //    posting.Posting_number, 352, "Product is out of stock", null, stoppingToken);
                                //if (!string.IsNullOrEmpty(cancelResult))
                                //    _logger.LogError(cancelResult);
                                continue;
                            }
                            var orderItems = new List<OrderItem>();
                            foreach (var item in detailOrder.Items)
                            {
                                orderItems.Add(new OrderItem
                                {
                                    Id = item.Id.ToString(),
                                    НоменклатураId = item.OfferId.Decode(encoding),
                                    Количество = item.Count,
                                    Цена = (decimal)item.Price,
                                    ЦенаСоСкидкой = (decimal)item.BuyerPrice,
                                    Вознаграждение = (decimal)item.Subsidy,
                                    Доставка = item.Delivery,
                                    ДопПараметры = item.Params,
                                    ИдентификаторПоставщика = item.FulfilmentShopId.ToString(),
                                    ИдентификаторСклада = item.WarehouseId.ToString(),
                                    ИдентификаторСкладаПартнера = item.PartnerWarehouseId
                                });
                            }
                            string складОтгрузкиId = detailOrder.Delivery?.Outlet?.Code.FormatTo1CId() ?? Common.SkladEkran;
                            string orderDeliveryShipmentId = "nothing";
                            DateTime orderShipmentDate = DateTime.MinValue;
                            if (detailOrder.Delivery.Shipments?.Count > 0)
                            {
                                orderDeliveryShipmentId = detailOrder.Delivery.Shipments[0].Id.ToString();
                                orderShipmentDate = detailOrder.Delivery.Shipments[0].ShipmentDate;
                            }
                            if (orderShipmentDate == DateTime.MinValue)
                            {
                                if (detailOrder.Delivery.Dates?.FromDate > DateTime.MinValue)
                                    orderShipmentDate = detailOrder.Delivery.Dates.FromDate;
                                else
                                    orderShipmentDate = DateTime.Today.AddDays(await _склад.ЭтоРабочийДень(складОтгрузкиId, 0));
                            }
                            OrderRecipientAddress address = detailOrder.Delivery.Address != null ? new OrderRecipientAddress
                            {
                                Postcode = detailOrder.Delivery.Address.Postcode ?? "",
                                Country = detailOrder.Delivery.Address.Country ?? "",
                                City = detailOrder.Delivery.Address.City ?? "",
                                Subway = detailOrder.Delivery.Address.Subway ?? "",
                                Street = detailOrder.Delivery.Address.Street ?? "",
                                House = detailOrder.Delivery.Address.House ?? "",
                                Block = detailOrder.Delivery.Address.Block ?? "",
                                Entrance = detailOrder.Delivery.Address.Entrance ?? "",
                                Entryphone = detailOrder.Delivery.Address.Entryphone ?? "",
                                Floor = detailOrder.Delivery.Address.Floor ?? "",
                                Apartment = detailOrder.Delivery.Address.Apartment ?? ""
                            } : null;
                            double deliveryPrice = 0;
                            double deliverySubsidy = 0;
                            if ((detailOrder.Delivery?.DeliveryPartnerType == YandexClasses.DeliveryPartnerType.SHOP) &&
                                (detailOrder.Delivery?.Type != YandexClasses.DeliveryType.PICKUP))
                            {
                                deliveryPrice = detailOrder.Delivery?.Price ?? 0 + detailOrder.Delivery?.Subsidy ?? 0;
                                deliverySubsidy = detailOrder.Delivery?.Subsidy ?? 0;
                            }
                            await _docService.NewOrder(
                                "YANDEX",
                                firmaId,
                                customerId,
                                dogovorId,
                                authorization,
                                encoding,
                                складОтгрузкиId,
                                detailOrder.Notes ?? "",
                                detailOrder.Delivery?.DeliveryServiceId ?? "0",
                                detailOrder.Delivery?.ServiceName ?? "",
                                (StinDeliveryPartnerType)detailOrder.Delivery?.DeliveryPartnerType,
                                (StinDeliveryType)detailOrder.Delivery?.Type,
                                deliveryPrice,
                                deliverySubsidy,
                                (StinPaymentType)detailOrder.PaymentType,
                                (StinPaymentMethod)detailOrder.PaymentMethod,
                                detailOrder.Delivery?.Region?.Id.ToString(),
                                detailOrder.Delivery?.Region?.Name ?? "",
                                address,
                                orderDeliveryShipmentId,
                                detailOrder.Id.ToString(),
                                orderShipmentDate,
                                orderItems,
                                cancellationToken);
                        }
                        else
                        {
                            if (order.InternalStatus == 0)
                                await _order.ОбновитьOrderStatus(order.Id, 8);
                            else if (readyToShipOrders.Contains(detailOrder.Id.ToString()) && 
                                (detailOrder.Status == YandexClasses.StatusYandex.PROCESSING) && (detailOrder.SubStatus == YandexClasses.SubStatusYandex.SHIPPED) &&
                                (order.InternalStatus < 14) && (order.InternalStatus != 6) && (order.InternalStatus != 5) && periodOpened && !_sleepPeriods.Any(x => x.IsSleeping()))
                            {
                                await _docService.OrderDeliveried(order, true);
                            }
                        }
                    }
                }
            }
            //pageNumber = 0;
            //nextPage = true;
            //while (nextPage)
            //{
            //    pageNumber++;
            //    nextPage = false;
            //    var result = await YandexClasses.YandexOperators.OrdersList(_httpService,
            //        _firmProxy[firmaId],
            //        campaignId,
            //        clientId,
            //        authToken,
            //        "DELIVERY",
            //        DateTime.Today.AddDays(-30),
            //        pageNumber,
            //        cancellationToken);
            //    nextPage = result.NextPage;
            //    if (result.Orders?.Count > 0)
            //    {
            //        foreach (var detailOrder in result.Orders)
            //        {
            //            TimeSpan ts = DateTime.Now - detailOrder.CreationDate.AddHours(1);
            //            if (ts.TotalMinutes > 10)
            //            {
            //                var order = await _order.ПолучитьOrderByMarketplaceId(marketplaceId, detailOrder.Id.ToString());
            //                if ((order != null) && (readyToShipOrders.Contains(detailOrder.Id.ToString()) &&
            //                    (detailOrder.Status == YandexClasses.StatusYandex.DELIVERY) &&
            //                    (order.DeliveryPartnerType != StinDeliveryPartnerType.SHOP) &&
            //                    (order.InternalStatus < 14) && (order.InternalStatus != 6) && (order.InternalStatus != 5) && 
            //                    periodOpened && !_sleepPeriods.Any(x => x.IsSleeping())))
            //                { 
            //                        await _docService.OrderDeliveried(order, true);
            //                }
            //            }
            //        }
            //    }
            //}
        }
        private async Task GetYandexCancelOrders(string firmaId, string campaignId, string clientId, string authToken, string marketplaceId, CancellationToken cancellationToken)
        {
            int pageNumber = 0;
            bool nextPage = true;
            while (nextPage)
            {
                pageNumber++;
                nextPage = false;
                var result = await YandexClasses.YandexOperators.OrderCancelList(_httpService, _firmProxy[firmaId], campaignId, clientId, authToken, pageNumber, cancellationToken);
                if ((result != null) && (result.Count > 0))
                {
                    nextPage = result[0].Item4;
                    foreach (var item in result)
                    {
                        var order = await _order.ПолучитьOrderByMarketplaceId(marketplaceId, item.Item1);
                        if ((order != null) && (order.InternalStatus != 5) && (order.InternalStatus != 6))
                        {
                            await _docService.OrderCancelled(order);
                        }
                    }
                }
            }
        }
        private async Task<List<Order>> GetOzonDetailOrders(string firmaProxy, string clientId, string authToken, string marketplaceId, OzonClasses.OrderStatus status, CancellationToken stoppingToken)
        {
            var result = new List<Order>();

            int limit = 1000;
            int numberCount = 0;
            bool nextPage = true;
            while (nextPage)
            {
                var resultWeb = await OzonClasses.OzonOperators.DetailOrders(_httpService, firmaProxy, clientId, authToken,
                    status,
                    60,
                    limit,
                    limit * numberCount,
                    stoppingToken);
                if (resultWeb.Item3 != null)
                    _logger.LogError(resultWeb.Item3);
                if (resultWeb.Item1 != null)
                {
                    foreach (var posting in resultWeb.Item1)
                    {
                        var order = await _order.ПолучитьOrderByMarketplaceId(marketplaceId, posting.Posting_number);
                        if (order != null)
                            result.Add(order);
                    }
                }
                numberCount++;
                nextPage = resultWeb.Item2.HasValue ? resultWeb.Item2.Value : false;
            }
            return result;
        }
        private async Task GetOzonCancelOrders(string firmaId, string clientId, string authToken, string id, CancellationToken stoppingToken)
        {
            var orders = await GetOzonDetailOrders(_firmProxy[firmaId], clientId, authToken, id, OzonClasses.OrderStatus.cancelled, stoppingToken);
            foreach (var order in orders)
            {
                if ((order.InternalStatus != 5) && (order.InternalStatus != 6) && (order.InternalStatus != 14) && (order.InternalStatus != 16))
                {
                    await _docService.OrderCancelled(order);
                }
            }
            //int limit = 1000;
            //int numberCount = 0;
            //bool nextPage = true;
            //while (nextPage)
            //{
            //    var result = await OzonClasses.OzonOperators.DetailOrders(_httpService, _firmProxy[firmaId], clientId, authToken,
            //        OzonClasses.OrderStatus.cancelled,
            //        30,
            //        limit,
            //        limit * numberCount,
            //        stoppingToken);
            //    if (result.Item3 != null)
            //        _logger.LogError(result.Item3);
            //    if (result.Item1 != null)
            //    {
            //        foreach (var posting in result.Item1)
            //        {
            //            var order = await _order.ПолучитьOrderByMarketplaceId(id, posting.Posting_number);
            //            if ((order != null) && (order.InternalStatus != 5) && (order.InternalStatus != 6) && (order.InternalStatus != 14))
            //            {
            //                await _docService.OrderCancelled(order);
            //            }
            //        }
            //    }
            //    numberCount++;
            //    nextPage = result.Item2.HasValue ? result.Item2.Value : false;
            //}
        }
        private async Task GetOzonDeliveringOrders(string marketplaceId, string firmaId, string clientId, string authToken, CancellationToken stoppingToken)
        {
            var activeOrders = await ActiveOrders(marketplaceId, stoppingToken);
            if (activeOrders?.Count > 0)
            {
                var orders = await GetOzonDetailOrders(_firmProxy[firmaId], clientId, authToken, marketplaceId, OzonClasses.OrderStatus.delivering, stoppingToken);
                orders.AddRange(await GetOzonDetailOrders(_firmProxy[firmaId], clientId, authToken, marketplaceId, OzonClasses.OrderStatus.driver_pickup, stoppingToken));
                var checkedNumbers = orders.Select(x => x.MarketplaceId).ToList();
                foreach (var order in orders.Where(x => activeOrders.Contains(x.MarketplaceId)))
                    await _docService.OrderDeliveried(order, true);

                orders = await GetOzonDetailOrders(_firmProxy[firmaId], clientId, authToken, marketplaceId, OzonClasses.OrderStatus.arbitration, stoppingToken);
                orders.AddRange(await GetOzonDetailOrders(_firmProxy[firmaId], clientId, authToken, marketplaceId, OzonClasses.OrderStatus.client_arbitration, stoppingToken));
                checkedNumbers.AddRange(orders.Select(x => x.MarketplaceId));
                foreach (var order in orders.Where(x => activeOrders.Contains(x.MarketplaceId)))
                    await _order.ОбновитьOrderStatus(order.Id, (int)StinOrderStatus.ARBITRATION);

                //foreach (var postingNumber in activeOrders.Where(x => !checkedNumbers.Contains(x)))
                //{
                //    var result = await OzonClasses.OzonOperators.OrderDetails(_httpService, _firmProxy[firmaId], clientId, authToken,
                //        postingNumber,
                //        stoppingToken);
                //    if (result.Item2 != null)
                //        _logger.LogError(result.Item2);
                //    if (result.Item1 != null)
                //    {
                //        if ((result.Item1 == OzonClasses.OrderStatus.driver_pickup) ||
                //            (result.Item1 == OzonClasses.OrderStatus.delivering))
                //        {
                //            var order = await _order.ПолучитьOrderByMarketplaceId(marketplaceId, postingNumber);
                //            if (order != null)
                //                await _docService.OrderDeliveried(order, true);
                //        }
                //        else if ((result.Item1 == OzonClasses.OrderStatus.arbitration) ||
                //            (result.Item1 == OzonClasses.OrderStatus.client_arbitration))
                //        {
                //            var order = await _order.ПолучитьOrderByMarketplaceId(marketplaceId, postingNumber);
                //            if (order != null)
                //                await _order.ОбновитьOrderStatus(order.Id, (int)StinOrderStatus.ARBITRATION);
                //        }
                //    }    
                //}
            }
        }
        private async Task GetOzonDeliveredOrders(string marketplaceId, string firmaId, string clientId, string authToken, CancellationToken stoppingToken)
        {
            var activeOrders = await _context.Sc13994s
                .Where(x => ((x.Sp13982 == 14) || (x.Sp13982 == 16)) &&
                    ((StinDeliveryPartnerType)x.Sp13985 == StinDeliveryPartnerType.OZON_LOGISTIC) &&
                    (x.Sp14038 == marketplaceId))
                .Select(x => x.Code.Trim())
                .ToListAsync(stoppingToken);
            if (activeOrders?.Count > 0)
            {
                var orders = await GetOzonDetailOrders(_firmProxy[firmaId], clientId, authToken, marketplaceId, OzonClasses.OrderStatus.delivered, stoppingToken);
                var checkedNumbers = orders.Select(x => x.MarketplaceId);
                foreach (var order in orders.Where(x => activeOrders.Contains(x.MarketplaceId)))
                    await _docService.OrderFromTransferDeliveried(order);
                //foreach (var postingNumber in activeOrders.Where(x => !checkedNumbers.Contains(x)))
                //{
                //    var result = await OzonClasses.OzonOperators.OrderDetails(_httpService, _firmProxy[firmaId], clientId, authToken,
                //        postingNumber,
                //        stoppingToken);
                //    if (result.Item2 != null)
                //        _logger.LogError(result.Item2);
                //    if (result.Item1 != null)
                //    {
                //        if (result.Item1 == OzonClasses.OrderStatus.delivered)
                //        {
                //            var order = await _order.ПолучитьOrderByMarketplaceId(marketplaceId, postingNumber);
                //            if (order != null)
                //                await _docService.OrderFromTransferDeliveried(order);
                //        }
                //    }
                //}
            }
        }
        private async Task<List<string>> ActiveOrders(string marketplaceId, CancellationToken cancellationToken)
        {
            DateTime dateRegTA = _context.GetRegTA();
            return await 
                        (from r in _context.Rg11973s //НаборНаСкладе
                         join doc in _context.Dh11948s on r.Sp11970 equals doc.Iddoc
                         join o in _context.Sc13994s on doc.Sp14003 equals o.Id
                         where (r.Period == dateRegTA) && (r.Sp11972 != 0) &&
                              (doc.Sp11938 == 1) &&
                              (o.Sp14038 == marketplaceId)
                              //&& o.Code.Trim() == "71239208-0018-2"
                         group new { r, doc, o } by new { orderId = doc.Sp14003, orderCode = o.Code, docId = doc.Iddoc } into gr
                         where gr.Sum(x => x.r.Sp11972) != 0
                         select gr.Key.orderCode.Trim())
                   .ToListAsync(cancellationToken);
        }
        public async Task UpdateTariffs(CancellationToken cancellationToken)
        {
            try
            {
                var marketplaceIds = await (from market in _context.Sc14042s
                                            where !market.Ismark
                                            //&& market.Code.Trim() == "22498162235000" // "23503334320000" //
                                            //&& market.Sp14155.Trim().ToUpper() == "OZON" 
                                            //&& market.Sp14155.Trim().ToUpper() == "WILDBERRIES"
                                            //&& market.Sp14155.Trim().ToUpper() == "SBER"
                                            //&& market.Code.Trim() == "23005267" // Yandex DBS
                                            select new
                                            {
                                                Id = market.Id,
                                                Тип = market.Sp14155.Trim().ToUpper(),
                                                Модель = market.Sp14164.Trim().ToUpper(),
                                                CampaignId = market.Code.Trim(),
                                                FirmaId = market.Parentext,
                                                ClientId = market.Sp14053.Trim(),
                                                AuthToken = market.Sp14054.Trim(),
                                                AuthSecret = market.Sp14195.Trim(),
                                                Authorization = market.Sp14077.Trim(),
                                                Encoding = (EncodeVersion)market.Sp14153
                                            })
                                            .ToListAsync();
                foreach (var marketplace in marketplaceIds)
                {
                    if (marketplace.Тип == "ЯНДЕКС")
                    {
                        await UpdateTariffsYandex(marketplace.Id, marketplace.FirmaId, marketplace.CampaignId, marketplace.ClientId, marketplace.AuthToken, marketplace.Encoding, marketplace.Модель, cancellationToken);
                    }
                    else if (marketplace.Тип == "OZON")
                    {
                        await UpdateTariffsOzon(marketplace.Id, marketplace.FirmaId, marketplace.CampaignId, marketplace.ClientId, marketplace.AuthToken, marketplace.Encoding, marketplace.Модель, cancellationToken);
                    }
                    else if (marketplace.Тип == "SBER")
                    {
                        await UpdateTariffsSber(marketplace.Id, marketplace.CampaignId, marketplace.FirmaId, marketplace.Encoding, cancellationToken);
                    }
                    else if (marketplace.Тип == "ALIEXPRESS")
                    {
                        await UpdateTariffsAliexpress(marketplace.Id, cancellationToken);
                    }
                    else if (marketplace.Тип == "WILDBERRIES")
                    {
                        await UpdateTariffsWildberries(marketplace.Id, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            //_logger.LogError("Finished");
        }
        private async Task UpdateTariffsSber(string marketplaceId, string campaignId, string firmaId, EncodeVersion encoding, CancellationToken cancellationToken)
        {
            var configSections = _configuration.GetSection("Catalog:productFeed").GetChildren();
            string path = "";
            foreach (var item in configSections)
            {
                var configData = item.AsEnumerable();
                var configItem = configData.Where(x => x.Key.EndsWith("FirmaId") && x.Value == firmaId).Count();
                if (configItem == 1)
                {
                    path = configData.Where(x => x.Key.EndsWith("Path")).Select(x => x.Value).FirstOrDefault();
                    break;
                }
            }
            var feedFiles = Directory.GetFiles(path, "feed_" + campaignId + "_???.xml");
            foreach (var feedFile in feedFiles)
            {
                var doc = XDocument.Load(feedFile);
                var offers = doc.Descendants("offer")
                    .Select(x => 
                    {
                        var weight = x.Elements("param").Where(y => y.Attribute("name").Value == "Weight").Select(z => z.Value).FirstOrDefault();
                        double.TryParse(weight, out double value);
                        return new
                        {
                            OfferCode = x.Attribute("id").Value.Decode(encoding),
                            ParentCode = x.Element("categoryId").Value.Decode(encoding),
                            Weight = value
                        };
                    });
                var offerCodes = offers.Select(x => x.OfferCode).ToList();
                var categoryCodes = offers.Select(x => x.ParentCode).Distinct();
                var dbData = await _context.Sc84s
                    .Where(x => categoryCodes.Contains(x.Code.Trim())
                                && !string.IsNullOrEmpty(x.Sp95)
                                && x.Sp95.Contains("SBER"))
                    .Select(x => new { Code = x.Code.Trim(), Comment = x.Sp95 })
                    .ToListAsync(cancellationToken);
                var categoryData = dbData.Select(x => new { x.Code, Percent = x.Comment.Split(";", StringSplitOptions.TrimEntries).Where(y => y.StartsWith("SBER", StringComparison.InvariantCultureIgnoreCase)).Select(z => { double.TryParse(z.Substring(4).Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double p); return p; }).FirstOrDefault() });
                var komisData = from o in offers
                                join c in categoryData on o.ParentCode equals c.Code
                                select new
                                {
                                    o.OfferCode,
                                    o.Weight,
                                    c.Percent
                                };
                var query = from markUse in _context.Sc14152s
                            join nom in _context.Sc84s on markUse.Parentext equals nom.Id
                            join market in _context.Sc14042s on markUse.Sp14147 equals market.Id
                            join vzTovar in _context.VzTovars on nom.Id equals vzTovar.Id into _vzTovar
                            from vzTovar in _vzTovar.DefaultIfEmpty()
                            where (markUse.Sp14147 == marketplaceId) &&
                              (markUse.Sp14158 == 1) //Есть в каталоге 
                              && offerCodes.Contains(nom.Code)
                              //&& nom.Code == "K00037057"
                            select new
                            {
                                Id = markUse.Id,
                                Sku = nom.Code,
                                ЦенаЗакуп = vzTovar != null ? vzTovar.Zakup ?? 0 : 0,
                                Квант = nom.Sp14188 == 0 ? 1 : nom.Sp14188,
                            };
                bool needUpdate = false;
                foreach (var item in await query.ToListAsync(cancellationToken))
                {
                    Sc14152 entity = await _context.Sc14152s.FirstOrDefaultAsync(x => x.Id == item.Id, cancellationToken);
                    var komis = komisData.FirstOrDefault(x => x.OfferCode == item.Sku);
                    if ((entity != null) && (komis != null) && (komis.Percent > 0))
                    {
                        var КоэфМинНаценки = 8; // 
                        var Порог = (double)(item.ЦенаЗакуп * item.Квант) * (100 + КоэфМинНаценки) / 100;
                        var fixSort = 10d; //за сортировку 10 руб
                        var fixProgLoyalnost = 1d; //программа лояльности 1 руб 
                        var c_category = komis.Percent / 100;
                        var c_transaction = 1.8 / 100; //1.8% 
                        var tariffLogistics = (percent: 1.0, limMin: 30.0, limMax: 280.0);
                        var fixLogistics = 0d;
                        var c_logistics = tariffLogistics.percent / 100;
                        var tariffLastMile = (percent: 4.0, limMin: 30.0, limMax: 215.0);
                        var fixLastMile = 0d;
                        var c_lastmile = tariffLastMile.percent / 100;
                        double minPrice = (Порог + fixProgLoyalnost + fixSort + fixLogistics + fixLastMile) / (1 - c_category - c_transaction - c_logistics - c_lastmile);
                        var limits = LimitValues(tariffLogistics, tariffLastMile, minPrice);
                        foreach (var limit in limits)
                        {
                            switch (limit.Key)
                            {
                                case "MaxWeightLimit":
                                    if (minPrice > limit.Value)
                                    {
                                        fixLogistics = tariffLogistics.limMax;
                                        c_logistics = 0;
                                    }
                                    break;
                                case "MaxLastMileLimit":
                                    if (minPrice > limit.Value)
                                    {
                                        fixLastMile = tariffLastMile.limMax;
                                        c_lastmile = 0;
                                    }
                                    break;
                                case "MinWeightLimit":
                                    if (minPrice < limit.Value)
                                    {
                                        fixLogistics = tariffLogistics.limMin;
                                        c_logistics = 0;
                                    }
                                    break;
                                case "MinLastMileLimit":
                                    if (minPrice < limit.Value)
                                    {
                                        fixLastMile = tariffLastMile.limMin;
                                        c_lastmile = 0;
                                    }
                                    break;
                            }
                            minPrice = (Порог + fixProgLoyalnost + fixSort + fixLogistics + fixLastMile) / (1 - c_category - c_transaction - c_logistics - c_lastmile);
                        }
                        //double c_weight = 0;
                        //var tariffLight = (percent: 3.0, limMin: 50.0, limMax: 500.0);
                        //var tariffHard = (percent: 3.0, limMin: 500.0, limMax: 1000.0);
                        //var tariff = komis.Weight > 25 ? tariffHard : tariffLight;
                        //c_weight = tariff.percent / 100;
                        //var c_delivery = minPrice * c_weight;
                        //if (c_delivery < tariff.limMin)
                        //    minPrice += tariff.limMin;
                        //else if (c_delivery > tariff.limMax)
                        //    minPrice += tariff.limMax;
                        //else
                        //    minPrice += c_delivery;
                        //minPrice += 10; //за прием заказов в СЦ (фиксированная 10 руб за отправление)
                        //var c_deliveryLastMile = minPrice * tariffLastMile.percent / 100;
                        //if (c_deliveryLastMile < tariffLastMile.limMin)
                        //    minPrice += tariffLastMile.limMin;
                        //else if (c_deliveryLastMile > tariffLastMile.limMax)
                        //    minPrice += tariffLastMile.limMax;
                        //else
                        //    minPrice += c_deliveryLastMile;

                        decimal updateMinPrice = decimal.Round((decimal)minPrice / item.Квант, 2, MidpointRounding.AwayFromZero);
                        if (updateMinPrice != entity.Sp14198)
                        {
                            entity.Sp14198 = updateMinPrice;
                            _context.Update(entity);
                            _context.РегистрацияИзмененийРаспределеннойИБ(14152, entity.Id);
                            needUpdate = true;
                        }
                    }
                }
                if (needUpdate)
                    await _context.SaveChangesAsync(cancellationToken);
            }
        }
        private async Task UpdateTariffsWildberries(string marketplaceId, CancellationToken cancellationToken)
        {
            decimal baseLogistics = 55;
            decimal addPerLiter = 5.5m;
            decimal includeLiters = 5;
            decimal minHard = 1000;
            decimal maxSize = 1.2m;
            decimal maxSumSize = 2;
            decimal maxWeight = 25;

            int requestLimit = 500;
            var query = from markUse in _context.Sc14152s
                        join nom in _context.Sc84s on markUse.Parentext equals nom.Id
                        join nomParent in _context.Sc84s on nom.Parentid equals nomParent.Id 
                        join ed in _context.Sc75s on nom.Sp94 equals ed.Id
                        join vzTovar in _context.VzTovars on nom.Id equals vzTovar.Id into _vzTovar
                        from vzTovar in _vzTovar.DefaultIfEmpty()
                        where (markUse.Sp14147 == marketplaceId) &&
                          (markUse.Sp14158 == 1) //Есть в каталоге 
                          && !string.IsNullOrEmpty(nomParent.Sp95)
                          && nomParent.Sp95.Contains("WB")
                        orderby nom.Code
                        select new
                        {
                            Id = markUse.Id,
                            ParentComment = nomParent.Sp95,
                            WeightBrutto = ed.Sp14056,
                            Height = ed.Sp14035,
                            Width = ed.Sp14036,
                            Lenght = ed.Sp14037,
                            ЦенаЗакуп = vzTovar != null ? vzTovar.Zakup ?? 0 : 0,
                            Квант = nom.Sp14188 == 0 ? 1 : nom.Sp14188,
                        };
            for (int i = 0; i < query.Count(); i = i + requestLimit)
            {
                var data = await query
                    .Skip(i)
                    .Take(requestLimit)
                    .ToListAsync(cancellationToken);
                bool needUpdate = false;
                foreach (var dataItem in data)
                {
                    var entity = await _context.Sc14152s.FirstOrDefaultAsync(x => x.Id == dataItem.Id, cancellationToken);
                    if (entity != null)
                    {
                        var categoryPercent = dataItem.ParentComment
                            .Split(";", StringSplitOptions.TrimEntries)
                            .Where(y => y.StartsWith("WB", StringComparison.InvariantCultureIgnoreCase))
                            .Select(z => { decimal.TryParse(z.Substring(2).Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal p); return p; })
                            .FirstOrDefault();
                        decimal КоэфМинНаценки = 8; 
                        var Порог = (dataItem.ЦенаЗакуп * dataItem.Квант) * (100 + КоэфМинНаценки) / 100;
                        var minPrice = Порог / (1 - categoryPercent / 100);

                        decimal liters = dataItem.Lenght * dataItem.Width * dataItem.Height * 1000;
                        decimal oversizeLiters = Math.Max(liters - includeLiters, 0);
                        bool isHard = (dataItem.WeightBrutto > maxWeight)
                            || ((dataItem.Lenght > maxSize) || (dataItem.Width > maxSize) || (dataItem.Height > maxSize))
                            || (dataItem.Lenght + dataItem.Width + dataItem.Height > maxSumSize);
                        decimal sumLogistics = baseLogistics + (oversizeLiters * addPerLiter);
                        if (isHard)
                            sumLogistics = Math.Max(sumLogistics, minHard);
                        minPrice += sumLogistics;
                        decimal updateMinPrice = decimal.Round(minPrice / dataItem.Квант, 2, MidpointRounding.AwayFromZero);
                        if (updateMinPrice != entity.Sp14198)
                        {
                            entity.Sp14198 = updateMinPrice;
                            _context.Update(entity);
                            _context.РегистрацияИзмененийРаспределеннойИБ(14152, entity.Id);
                            needUpdate = true;
                        }
                    }
                }
                if (needUpdate)
                    await _context.SaveChangesAsync(cancellationToken);
            }
        }
        private async Task UpdateTariffsAliexpress(string marketplaceId, CancellationToken cancellationToken)
        {
            double baseTariff = 8; //%
            double deliveryForCustomer = 5; //%
            int requestLimit = 500;
            var query = from markUse in _context.Sc14152s
                        join nom in _context.Sc84s on markUse.Parentext equals nom.Id
                        join ed in _context.Sc75s on nom.Sp94 equals ed.Id
                        join vzTovar in _context.VzTovars on nom.Id equals vzTovar.Id into _vzTovar
                        from vzTovar in _vzTovar.DefaultIfEmpty()
                        where (markUse.Sp14147 == marketplaceId) &&
                          (markUse.Sp14158 == 1) //Есть в каталоге 
                          //&& (nom.Code == "D00040383")
                        orderby nom.Code
                        select new
                        {
                            Id = markUse.Id,
                            ЦенаЗакуп = vzTovar != null ? vzTovar.Zakup ?? 0 : 0,
                            Квант = nom.Sp14188 == 0 ? 1 : nom.Sp14188,
                            WeightBrutto = ed.Sp14056,
                        };
            for (int i = 0; i < query.Count(); i = i + requestLimit)
            {
                var data = await query
                    .Skip(i)
                    .Take(requestLimit)
                    .ToListAsync(cancellationToken);
                bool needUpdate = false;
                foreach (var dataItem in data)
                {
                    var entity = await _context.Sc14152s.FirstOrDefaultAsync(x => x.Id == dataItem.Id, cancellationToken);
                    if (entity != null)
                    {
                        var КоэфМинНаценки = 8;
                        var Порог = (double)(dataItem.ЦенаЗакуп * dataItem.Квант) * (100 + КоэфМинНаценки) / 100;
                        double delivery = dataItem.WeightBrutto switch
                        {
                            < 2 => 159,
                            < 5 => 359,
                            _ => 659
                        };
                        var minPrice = (Порог + delivery) / ((100 - baseTariff) / 100);
                        if (minPrice < 1000)
                        {
                            var deliveryCompensation = (49 * (100 + deliveryForCustomer) / 100) / ((100 - baseTariff) / 100); //сумма заказа от 499 до 999 руб - компенсация 49 руб
                            if (minPrice - deliveryCompensation > 499)
                                minPrice -= deliveryCompensation;
                            else
                                minPrice -= (99 * (100 + deliveryForCustomer) / 100) / ((100 - baseTariff) / 100); //сумма заказа до 499 руб - компенсация 99 руб
                        }
                        decimal updateMinPrice = decimal.Round((decimal)minPrice / dataItem.Квант, 2, MidpointRounding.AwayFromZero);

                        if (updateMinPrice != entity.Sp14198)
                        {
                            entity.Sp14198 = updateMinPrice;
                            _context.Update(entity);
                            _context.РегистрацияИзмененийРаспределеннойИБ(14152, entity.Id);
                            needUpdate = true;
                        }
                    }
                }
                if (needUpdate)
                    await _context.SaveChangesAsync(cancellationToken);
            }
        }
        double CommissionValuesVolumeWeightYandex(double volumeWeight, string model)
        {
            if (model == "FBS")
                return volumeWeight switch
                {
                    < 0.2 => 15,
                    < 0.5 => 25,
                    < 1 => 35,
                    < 2 => 45,
                    < 5 => 70,
                    < 10 => 120,
                    < 15 => 225,
                    < 25 => 350,
                    < 50 => 500,
                    _ => 600
                };
            //FBY
            return volumeWeight switch
            {
                < 0.2 => 10,
                < 0.5 => 20,
                < 1 => 30,
                < 2 => 40,
                < 5 => 60,
                < 10 => 100,
                < 15 => 200,
                < 25 => 325,
                < 50 => 400,
                _ => 550
            };
        }
        double CommissionYandexLightTariff(string model, double price, double порог, double приемПлатежа, double переводПлатежа)
        {
            double lightTariff = price switch
            {
                < 100 => 80,
                < 300 => 95,
                _ => 115
            };
            if (model == "FBY")
                lightTariff = price switch
                {
                    < 100 => 30,
                    < 300 => 40,
                    _ => 55
                };
            return (порог + приемПлатежа + lightTariff) / (1 - переводПлатежа);
        }
        private async Task UpdateTariffsYandex(string marketplaceId, string firmaId, 
            string campaignId,
            string clientId,
            string authToken,
            EncodeVersion encoding,
            string model,
            CancellationToken cancellationToken)
        {
            int requestLimit = 500;

            var query = from markUse in _context.Sc14152s
                        join nom in _context.Sc84s on markUse.Parentext equals nom.Id
                        join vzTovar in _context.VzTovars on nom.Id equals vzTovar.Id into _vzTovar
                        from vzTovar in _vzTovar.DefaultIfEmpty()
                        where (markUse.Sp14147 == marketplaceId) &&
                          (markUse.Sp14158 == 1) //Есть в каталоге 
                          //&& nom.Code == "D00028044"
                        select new
                        {
                            Id = markUse.Id,
                            Sku = nom.Code,
                            ЦенаЗакуп = vzTovar != null ? vzTovar.Zakup ?? 0 : 0,
                            Квант = nom.Sp14188 == 0 ? 1 : nom.Sp14188,
                        };
            for (int i = 0; i < query.Count(); i = i + requestLimit)
            {
                var data = await query
                    .OrderBy(x => x.Sku)
                    .Skip(i)
                    .Take(requestLimit)
                    .ToListAsync(cancellationToken);
                var request = new YandexClasses.SkuDetailsRequest { ShopSkus = data.Select(x => x.Sku.Encode(encoding)).ToList() };
                var result = await YandexClasses.YandexOperators.Exchange<YandexClasses.SkuDetailsResponse>(_httpService,
                    $"https://{_firmProxy[firmaId]}api.partner.market.yandex.ru/v2/campaigns/{campaignId}/stats/skus.json",
                    HttpMethod.Post,
                    clientId,
                    authToken,
                    request,
                    cancellationToken);
                if (((result.Item1 == YandexClasses.ResponseStatus.ERROR) ||
                    (result.Item2?.Status == YandexClasses.ResponseStatus.ERROR)) &&
                    !string.IsNullOrEmpty(result.Item3))
                {
                    _logger.LogError(result.Item3);
                }
                if (result.Item2?.Result?.ShopSkus?.Count > 0)
                {
                    bool needUpdate = false;
                    foreach (var item in result.Item2.Result.ShopSkus)
                    {
                        var dataItem = data.FirstOrDefault(x => x.Sku.Encode(encoding) == item.ShopSku);
                        if (dataItem != null)
                        {
                            Sc14152 entity = null;
                            if (!string.IsNullOrEmpty(dataItem.Id))
                                entity = await _context.Sc14152s.FirstOrDefaultAsync(x => x.Id == dataItem.Id, cancellationToken);
                            if (entity != null)
                            {
                                var minPrice = (decimal)item.Price * dataItem.Квант;
                                var sumPercentTariffs = item.Tariffs?.Sum(x => (decimal)x.Percent) ?? 0;
                                var dimensions = ((decimal?)item.WeightDimensions?.Width ?? 0)
                                    + ((decimal?)item.WeightDimensions?.Length ?? 0)
                                    + ((decimal?)item.WeightDimensions?.Height ?? 0);
                                var weight = ((decimal?)item.WeightDimensions?.Weight ?? 0);
                                var volumeWeight = (decimal)((item.WeightDimensions?.Width ?? 0) * (item.WeightDimensions?.Length ?? 0) * (item.WeightDimensions?.Height ?? 0)) / 5000;
                                var modelType = model switch
                                {
                                    "FBS" => ModelTypeYandex.FBS,
                                    "FBY" => ModelTypeYandex.FBY,
                                    _ => ModelTypeYandex.DBS
                                };
                                using (var helper = new CommissionHelperYandex(modelType, (int)dataItem.Квант, dataItem.ЦенаЗакуп, volumeWeight, sumPercentTariffs, (decimal)item.Price, weight, dimensions))
                                {
                                    minPrice = helper.MinPrice();
                                }

                                decimal updateMinPrice = decimal.Round(minPrice, 2, MidpointRounding.AwayFromZero);
                                if (updateMinPrice != entity.Sp14198)
                                {
                                    entity.Sp14198 = updateMinPrice;
                                    _context.Update(entity);
                                    _context.РегистрацияИзмененийРаспределеннойИБ(14152, entity.Id);
                                    needUpdate = true;
                                }
                            }
                        }
                    }
                    if (needUpdate)
                        await _context.SaveChangesAsync(cancellationToken);
                }
            }
        }
        double CommissionValuesFbsVolumeWeight(double volumeWeight, bool kgt)
        {
            if (kgt)
                return 1100;
            return volumeWeight switch
            {
                0.1d => 40, 
                0.2d => 41, 
                0.3d => 42,
                0.4d => 43, 
                0.5d => 43, 
                0.6d => 45, 
                0.7d => 45, 
                0.8d => 47, 
                0.9d => 49, 
                1d => 51, 
                1.1d => 55, 
                1.2d => 57,
                1.3d => 61,
                1.4d => 63,
                1.5d => 65, 
                1.6d => 67, 
                1.7d => 69, 
                1.8d => 70,
                1.9d => 71, 
                < 3d => 79,
                < 4d => 100, 
                < 5d => 120,
                < 6d => 135,
                < 7d => 160,
                < 8d => 185,
                < 9d => 210, 
                < 10d => 225,
                < 11d => 265,
                < 12d => 290,
                < 13d => 315,
                < 14d => 350,
                < 15d => 370,
                < 20d => 400,
                < 25d => 525,
                _ =>700
            };
        }
        double CommissionValuesFbsVolumeWeightFbo(double volumeWeight)
        {
            return volumeWeight switch
            {
                0.1d => 40, 
                0.2d => 41, 
                0.3d => 42, 
                0.4d => 43, 
                0.5d => 43, 
                0.6d => 45, 
                0.7d => 45, 
                0.8d => 47, 
                0.9d => 49, 
                1d => 51, 
                1.1d => 55, 
                1.2d => 57, 
                1.3d => 61, 
                1.4d => 63, 
                1.5d => 65,
                1.6d => 67, 
                1.7d => 69, 
                1.8d => 70, 
                1.9d => 71,
                < 3d => 79, 
                < 4d => 100, 
                < 5d => 120, 
                < 6d => 135, 
                < 7d => 160,
                < 8d => 185,
                < 9d => 210,
                < 10d => 225, 
                < 11d => 265, 
                < 12d => 290, 
                < 13d => 315, 
                < 14d => 350, 
                < 15d => 370, 
                < 20d => 400, 
                < 25d => 525,
                < 30d => 700, 
                < 35d => 800, 
                _ => 1000
            };
        }
        private Dictionary<string,double> LimitValues(
            (double percent, double limMin, double limMax) weightLim,
            (double percent, double limMin, double limMax) lastMileLim,
            double calcPrice)
        {
            Dictionary<string,double> values = new Dictionary<string, double>();
            var minLimit = weightLim.percent > 0 ? weightLim.limMin * 100 / weightLim.percent : double.MinValue;
            var maxLimit = weightLim.percent > 0 && weightLim.limMax < double.MaxValue ? weightLim.limMax * 100 / weightLim.percent : double.MaxValue;
            values.Add("MinWeightLimit", minLimit);
            values.Add("MaxWeightLimit", maxLimit);
            
            minLimit = lastMileLim.percent > 0 ? lastMileLim.limMin * 100 / lastMileLim.percent : double.MinValue;
            maxLimit = lastMileLim.percent > 0 && lastMileLim.limMax < double.MaxValue ? lastMileLim.limMax * 100 / lastMileLim.percent : double.MaxValue;
            values.Add("MinLastMileLimit", minLimit);
            values.Add("MaxLastMileLimit", maxLimit);

            return values.OrderByDescending(x => Math.Abs(x.Value - calcPrice)).ThenBy(x => x.Key).ToDictionary(k => k.Key, v => v.Value);
        }
        private async Task UpdateTariffsOzon(string marketplaceId, string firmaId,
            string campaignId,
            string clientId,
            string authToken,
            EncodeVersion encoding,
            string model,
            CancellationToken cancellationToken)
        {
            int requestLimit = 100;

            var query = from markUse in _context.Sc14152s
                        join nom in _context.Sc84s on markUse.Parentext equals nom.Id
                        join market in _context.Sc14042s on markUse.Sp14147 equals market.Id
                        join vzTovar in _context.VzTovars on nom.Id equals vzTovar.Id into _vzTovar
                        from vzTovar in _vzTovar.DefaultIfEmpty()
                        where (markUse.Sp14147 == marketplaceId) &&
                          (markUse.Sp14158 == 1) //Есть в каталоге 
                          //&& nom.Code == "D00044984"
                        select new
                        {
                            Id = markUse.Id,
                            Sku = nom.Code,
                            realFbs = !string.IsNullOrWhiteSpace(market.Sp14154) && (market.Sp14154.Trim() == markUse.Sp14190.Trim()),
                            ЦенаЗакуп = vzTovar != null ? vzTovar.Zakup ?? 0 : 0,
                            Квант = nom.Sp14188 == 0 ? 1 : nom.Sp14188,
                        };
            for (int i = 0; i < query.Count(); i = i + requestLimit)
            {
                var data = await query
                    .OrderBy(x => x.Sku)
                    .Skip(i)
                    .Take(requestLimit)
                    .ToListAsync(cancellationToken);
                bool needUpdate = false;
                foreach (var item in data)
                {
                    var comResult = await OzonClasses.OzonOperators.ProductComission(_httpService, _firmProxy[firmaId], clientId, authToken,
                        item.Sku.Encode(encoding),
                        model == "FBS" ? (item.realFbs ? "rfbs" : "fbs") : "fbo",
                        cancellationToken);
                    if (comResult.Error != null && !string.IsNullOrEmpty(comResult.Error))
                    {
                        _logger.LogError(comResult.Error);
                    }
                    if ((comResult.ComPercent > 0) && (comResult.ComAmount > 0) && (comResult.VolumeWeight > 0))
                    {
                        double.TryParse(comResult.Price ?? "", System.Globalization.NumberStyles.Number, new System.Globalization.NumberFormatInfo { NumberDecimalSeparator = "." }, out double price);
                        if (price > 0)
                        {
                            Sc14152 entity = await _context.Sc14152s.FirstOrDefaultAsync(x => x.Id == item.Id, cancellationToken);
                            if (entity != null)
                            {
                                var minPrice = (decimal)price;
                                ModelTypeOzon typeOzon = model == "FBS" ? (item.realFbs ? ModelTypeOzon.RealFBS : ModelTypeOzon.FBS) : ModelTypeOzon.FBO;
                                using (var helper = new CommissionHelperOzon(typeOzon, (int)item.Квант, item.ЦенаЗакуп, (decimal)comResult.VolumeWeight, (decimal)comResult.ComPercent))
                                {
                                    minPrice = helper.MinPrice();
                                }
                                decimal updateMinPrice = decimal.Round(minPrice, 2, MidpointRounding.AwayFromZero);
                                decimal updateVolumeWeight = decimal.Round((decimal)comResult.VolumeWeight / item.Квант, 3, MidpointRounding.AwayFromZero);
                                if ((updateMinPrice != entity.Sp14198) || (updateVolumeWeight != entity.Sp14229))
                                {
                                    entity.Sp14198 = updateMinPrice;
                                    entity.Sp14229 = updateVolumeWeight;
                                    _context.Update(entity);
                                    _context.РегистрацияИзмененийРаспределеннойИБ(14152, entity.Id);
                                    needUpdate = true;
                                }
                            }
                        }
                    }
                }
                if (needUpdate)
                    await _context.SaveChangesAsync(cancellationToken);
            }
        }
        public async Task CheckReturns(CancellationToken cancellationToken)
        {
            try
            {
                var marketplaceIds = await (from market in _context.Sc14042s
                                            where !market.Ismark
                                            //&& market.Code.Trim() == "23292582"
                                            //&& market.Sp14155.Trim().ToUpper() == "OZON"
                                            //&& market.Sp14155.Trim().ToUpper() == "WILDBERRIES"
                                            //&& market.Code.Trim() == "23005267" // Yandex DBS
                                            //&& market.Code.Trim() == "22498162235000" // Ozon ОнлайнМаркет
                                            //&& market.Code.Trim() == "1020000171757000" // Ozon ХардВоркер
                                            select new
                                            {
                                                Id = market.Id,
                                                Тип = market.Sp14155.Trim().ToUpper(),
                                                Модель = market.Sp14164.Trim().ToUpper(),
                                                CampaignId = market.Code.Trim(),
                                                FirmaId = market.Parentext,
                                                ClientId = market.Sp14053.Trim(),
                                                AuthToken = market.Sp14054.Trim(),
                                                AuthSecret = market.Sp14195.Trim(),
                                                Authorization = market.Sp14077.Trim(),
                                                Encoding = (EncodeVersion)market.Sp14153
                                            })
                                            .ToListAsync();
                foreach (var marketplace in marketplaceIds)
                {
                    if (marketplace.Тип == "ЯНДЕКС")
                    {
                        await CheckReturnsYandex(marketplace.Id, _firmProxy[marketplace.FirmaId], marketplace.CampaignId, marketplace.ClientId, marketplace.AuthToken, cancellationToken);
                    }
                    else if (marketplace.Тип == "OZON")
                    {
                        await CheckReturnsOzon(marketplace.Id, _firmProxy[marketplace.FirmaId], marketplace.ClientId, marketplace.AuthToken, 0, cancellationToken);
                    }
                    else if (marketplace.Тип == "SBER")
                    {
                    }
                    else if (marketplace.Тип == "ALIEXPRESS")
                    {
                    }
                    else if (marketplace.Тип == "WILDBERRIES")
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            //_logger.LogError("Finished");
        }
        async Task CheckReturnsYandex(string marketplaceId, string proxyHost, string campaignId, string clientId, string authToken, CancellationToken cancellationToken)
        {
            int requestLimit = 31;
            string pageToken = "";
            bool nextPage = true;
            while (nextPage)
            {
                nextPage = false;
                var result = await YandexClasses.YandexOperators.OrderReturns(_httpService, proxyHost, campaignId, clientId, authToken,
                DateTime.Today.AddDays(-60),
                requestLimit,
                pageToken,
                cancellationToken);
                pageToken = result.NextPageToken;
                nextPage = !string.IsNullOrEmpty(pageToken);
                if (result.Returns?.Length > 0)
                {
                    try
                    {
                        await UpdateReturns(result.Returns, marketplaceId, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Internal (CheckReturnsYandex) : " + ex.Message);
                    }
                }
            }
        }
        private async Task CheckReturnsOzon(string marketplaceId, string proxyHost,
            string clientId,
            string authToken,
            long offset,
            CancellationToken cancellationToken)
        {
            int requestLimit = 1000;

            var result = await OzonClasses.OzonOperators.ReturnOrders(_httpService, proxyHost, clientId, authToken, requestLimit, offset, cancellationToken);
            if (!string.IsNullOrEmpty(result.error))
                _logger.LogError(result.error);
            if (result.returns?.Count > 0)
                try
                {
                    await UpdateReturns(result.returns, marketplaceId, cancellationToken);
                    await CheckReturnsOzon(marketplaceId, proxyHost, clientId, authToken, result.count, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Internal (CheckReturnsOzon) : " + ex.Message);
                }
        }
        private async Task UpdateReturns(object data, string marketplaceId, CancellationToken cancellationToken)
        {
            //var updatingOrders = new Dictionary<string, int>();
            var returningOrders = Enumerable.Repeat(new 
            {
                OrderNo = "",
                Status = 0,
                LastWaitingDate = DateTime.MinValue
            }, 0).ToList();
            if (data is IList<OzonClasses.ReturnItem> ozonReturns)
            {
                foreach (var item in ozonReturns)
                    if (!string.IsNullOrWhiteSpace(item.Posting_number) && (item.Posting_number != "0"))
                    {
                        int status = item.Status switch
                        {
                            //OzonClasses.ReturnStatus.returned_to_seller => 17,
                            OzonClasses.ReturnStatus.cancelled_with_compensation => 18,
                            _ => 15
                        };
                        //updatingOrders.TryAdd(item.Posting_number, status);
                        if (!returningOrders.Any(x => x.OrderNo == item.Posting_number))
                            returningOrders.Add(new 
                            {
                                OrderNo = item.Posting_number,
                                Status = status,
                                LastWaitingDate = item.Last_free_waiting_day ?? DateTime.MinValue
                            });
                    }
            }
            else if (data is YandexClasses.Return[] yandexReturns)
            {
                foreach (var item in yandexReturns.Where(x => (x.RefundStatus != YandexClasses.RefundStatusType.CANCELLED) && (x.ShipmentStatus != YandexClasses.ReturnShipmentStatusType.PICKED)))
                {
                    if (!returningOrders.Any(x => x.OrderNo == item.OrderId.ToString()))
                        returningOrders.Add(new
                        {
                            OrderNo = item.OrderId.ToString(),
                            Status = 15,
                            LastWaitingDate = DateTime.MinValue
                        });
                }
            }
            foreach (var item in returningOrders)
            {
                var order = await _order.ПолучитьOrderByMarketplaceId(marketplaceId, item.OrderNo);
                if (order != null)
                {
                    if ((order.InternalStatus != item.Status) && (order.InternalStatus != 5) && (order.InternalStatus != 17) && !((order.InternalStatus == 16) && (item.Status == 15)))
                    {
                        var status = item.Status;
                        if ((order.InternalStatus == 14) && (status == 15))
                            status = 16;
                        await _order.ОбновитьOrderStatus(order.Id, status);
                    }
                    //Нужно?
                    //if ((item.LastWaitingDate != DateTime.MinValue) && (item.LastWaitingDate != order.ShipmentDate))
                    //    await _order.ОбновитьOrderShipmentDate(order.Id, item.LastWaitingDate);
                }
            }
        }
    }
}
