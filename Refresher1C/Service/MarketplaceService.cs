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

namespace Refresher1C.Service
{
    class MarketplaceService : IMarketplaceService
    {
        private readonly StinDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MarketplaceService> _logger;
        private IHttpService _httpService;
        private IDocCreateOrUpdate _docService;

        private IOrder _order;
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
                    _фирма.Dispose();
                    _номенклатура.Dispose();
                    _склад.Dispose();
                    _заявкаПокупателя.Dispose();
                    _набор.Dispose();
                    _отменаНабора.Dispose();
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
        public MarketplaceService(IConfiguration configuration, ILogger<MarketplaceService> logger, IDocCreateOrUpdate docService, StinDbContext context, IHttpService httpService)
        {
            _logger = logger;
            _docService = docService;
            _httpService = httpService;
            _context = context;
            _configuration = configuration;
            _order = new OrderEntity(context);
            _фирма = new ФирмаEntity(context);
            _номенклатура = new НоменклатураEntity(context);
            _склад = new СкладEntity(context);
            _заявкаПокупателя = new ЗаявкаПокупателя(context);
            _набор = new Набор(context);
            _отменаНабора = new ОтменаНабора(context);
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
                                       join r in _context.Rg4674s.Where(x => x.Period == dateRegTA) on new { IdDoc = docT.Iddoc, НоменклатураId = docT.Sp2446 } equals new { IdDoc = r.Sp4671, НоменклатураId = r.Sp4669 }
                                       //from r in _r.DefaultIfEmpty()
                                       //join market in _context.Sc14042s on order.Sp14038 equals market.Id
                                       where
                                           (nom.Sp2417 == ВидыНоменклатуры.Товар) &&
                                           (order.Sp13982 == 8) && //order статус = 8
                                           ((doc.Sp4760 == видОперацииЗаявкаОдобренная) || (doc.Sp4760 == видОперацииСчетНаОплату))
                                       //market.Sp14077.Trim() == _yandexFbsAuthToken
                                       group new { doc, docT, r } by new { ЗаявкаId = doc.Iddoc, OrderId = doc.Sp13995, НоменклатураId = docT.Sp2446, Коэффициент = docT.Sp2449 } into gr
                                       //where gr.Sum(x => x.r.Sp4672) != 0
                                       select new
                                       {
                                           ЗаявкаId = gr.Key.ЗаявкаId,
                                           OrderId = gr.Key.OrderId,
                                           НоменклатураId = gr.Key.НоменклатураId,
                                           КолДокумента = gr.Sum(x => x.docT.Sp2447) * gr.Key.Коэффициент,
                                           КолДоступно = gr.Sum(x => x.r.Sp4672)
                                       };
                var notReadyЗаявкиIds = остаткиРегЗаявки.Where(x => x.КолДокумента != x.КолДоступно).Select(x => new { x.ЗаявкаId, x.OrderId }).Distinct();
                var readyЗаявкиIds = await остаткиРегЗаявки.Select(x => new { x.ЗаявкаId, x.OrderId }).Distinct().Except(notReadyЗаявкиIds).ToListAsync();

                foreach (var data in readyЗаявкиIds)
                    await _docService.CreateNabor(data.ЗаявкаId, stoppingToken);
            }
        }
        public async Task PrepareYandexFbsBoxes(CancellationToken stoppingToken)
        {
            var urlBoxes = _configuration["Marketplace:urlBoxes"];
            using var tran = await _context.Database.BeginTransactionAsync(stoppingToken);
            try
            {
                DateTime dateRegTA = _context.GetRegTA();
                var data = await (from r in _context.Rg14021s
                                  join order in _context.Sc13994s on r.Sp14010 equals order.Id
                                  join market in _context.Sc14042s on order.Sp14038 equals market.Id
                                  join nom in _context.Sc84s on r.Sp14012 equals nom.Id
                                  join ed in _context.Sc75s on nom.Sp94 equals ed.Id
                                  //join markUse in _context.Sc14152s.Where(x => !x.Ismark) on new { NomId = nom.Id, MarketId = market.Id } equals new { NomId = markUse.Parentext, MarketId = markUse.Sp14147 } into _markUse
                                  //from markUse in _markUse.DefaultIfEmpty()
                                  where r.Period == dateRegTA && ((r.Sp14011 == 10) || (r.Sp14011 == 11)) &&
                                    order.Sp13982 == 8 //order статус = 8
                                    && (((StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.YANDEX_MARKET) ||
                                        ((StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.OZON_LOGISTIC))
                                  group new { r, order, market, nom, ed } by new
                                  {
                                      OrderId = order.Id,
                                      MarketplaceId = order.Code,
                                      ShipmentId = order.Sp13989,
                                      Тип = market.Sp14155,
                                      CampaignId = market.Code,
                                      ClientId = market.Sp14053,
                                      AuthToken = market.Sp14054,
                                      НоменклатураId = nom.Id,
                                      КоэффициентЕдиницы = ed.Sp78,
                                      КолМест = ed.Sp14063,
                                      Квант = nom.Sp14188
                                  } into gr
                                  where (gr.Sum(x => x.r.Sp14017) != 0)
                                  select new
                                  {
                                      OrderId = gr.Key.OrderId,
                                      MarketplaceId = gr.Key.MarketplaceId.Trim(),
                                      ShipmentId = gr.Key.ShipmentId.Trim(),
                                      Тип = gr.Key.Тип.ToUpper().Trim(),
                                      CampaignId = gr.Key.CampaignId.Trim(),
                                      ClientId = gr.Key.ClientId.Trim(),
                                      AuthToken = gr.Key.AuthToken.Trim(),
                                      НоменклатураId = gr.Key.НоменклатураId,
                                      КолМест = gr.Key.КолМест == 0 ? 1 : gr.Key.КолМест,
                                      Количество = gr.Sum(x => x.r.Sp14017) / gr.Key.КоэффициентЕдиницы,
                                      Квант = gr.Key.Квант
                                  })
                       .ToListAsync(stoppingToken);
                if ((data != null) && (data.Count > 0))
                {
                    var dataGrouped = data.GroupBy(x => new
                    {
                        OrderId = x.OrderId,
                        MarketplaceId = x.MarketplaceId,
                        ShipmentId = x.ShipmentId,
                        Тип = x.Тип,
                        CampaignId = x.CampaignId,
                        ClientId = x.ClientId,
                        AuthToken = x.AuthToken,
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
                                var result = await YandexClasses.YandexOperators.Exchange<YandexClasses.BoxesResponse>(_httpService,
                                    string.Format(urlBoxes, obj.Key.CampaignId, obj.Key.MarketplaceId, obj.Key.ShipmentId),
                                    HttpMethod.Put,
                                    obj.Key.ClientId,
                                    obj.Key.AuthToken,
                                    request, 
                                    stoppingToken);
                                //string context = request.SerializeObject();
                                //var result = YandexClasses.YandexOperators.YandexExchange(null, string.Format(urlBoxes, obj.Key.CampaignId, obj.Key.MarketplaceId, obj.Key.ShipmentId), HttpMethod.Put, obj.Key.ClientId, obj.Key.AuthToken, context).Result;
                                if (result.Item1 == YandexClasses.ResponseStatus.ERROR)
                                {
                                    if (!string.IsNullOrEmpty(result.Item3) && !result.Item3.Contains("INTERNAL_SERVER_ERROR"))
                                    //if ((result.Item3 != null) &&
                                    //    (result.Item3.Errors != null) &&
                                    //    (result.Item3.Errors.Count > 0) &&
                                    //    (!result.Item3.Errors.Any(x => x.Code.Trim().ToUpper() == "INTERNAL_SERVER_ERROR")))
                                    {
                                        //записать ошибку
                                        status = -1;
                                        //err = YandexClasses.YandexOperators.ParseErrorResponse(result.Item3);
                                        err = result.Item3;
                                    }
                                }
                                else
                                {
                                    if ((result.Item1 == YandexClasses.ResponseStatus.OK) && (result.Item2 != null))
                                        try
                                        {
                                            if (result.Item2.Status != YandexClasses.ResponseStatus.OK)
                                            //var boxesResponse = result.Item2.DeserializeObject<YandexClasses.BoxesResponse>();
                                            //if (boxesResponse.Status != YandexClasses.ResponseStatus.OK)
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
                            entity.Sp13982 = status;
                            if (!string.IsNullOrEmpty(err))
                                entity.Sp14055 = err;

                            _context.Update(entity);
                            _context.РегистрацияИзмененийРаспределеннойИБ(13994, entity.Id);
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
        public async Task PrepareFbsLabels(CancellationToken stoppingToken)
        {
            using var tran = await _context.Database.BeginTransactionAsync(stoppingToken);
            try
            {
                var data = await (from order in _context.Sc13994s
                                  join market in _context.Sc14042s on order.Sp14038 equals market.Id
                                  join binary in _context.VzOrderBinaries.Where(x => x.Extension.Trim().ToUpper() == "LABELS") on order.Id equals binary.Id into _binary
                                  from binary in _binary.DefaultIfEmpty()
                                  where ((order.Sp13982 == 1) || (order.Sp13982 == 3)) && //order статус = 1 (грузовые места сформированы)
                                        (((StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.YANDEX_MARKET) ||
                                        ((StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.OZON_LOGISTIC)) &&
                                        (binary == null)
                                  group new { order, market } by new { order.Id, order.Code, campaignId = market.Code, clientId = market.Sp14053, token = market.Sp14054, тип = market.Sp14155 } into gr
                                  select new
                                  {
                                      OrderId = gr.Key.Id,
                                      MarketplaceId = gr.Key.Code.Trim(),
                                      CampaignId = gr.Key.campaignId.Trim(),
                                      ClientId = gr.Key.clientId.Trim(),
                                      AuthToken = gr.Key.token.Trim(),
                                      Тип = gr.Key.тип.ToUpper().Trim()
                                  }).ToListAsync(stoppingToken);
                if ((data != null) && (data.Count() > 0))
                {
                    var urlLabels = _configuration["Marketplace:urlLabels"];
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
                                var result = await YandexClasses.YandexOperators.Exchange<byte[]>(_httpService,
                                    string.Format(urlLabels, order.CampaignId, order.MarketplaceId),
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
                                //var result = await YandexClasses.YandexOperators.YandexExchange(null, string.Format(urlLabels, order.CampaignId, order.MarketplaceId), HttpMethod.Get, order.ClientId, order.AuthToken, null);
                                //if (result.Item1 == YandexClasses.ResponseStatus.ERROR)
                                //{
                                //    if ((result.Item3 != null) &&
                                //        (result.Item3.Errors != null) &&
                                //        (result.Item3.Errors.Count > 0) &&
                                //        (!result.Item3.Errors.Any(x => x.Code.Trim().ToUpper() == "INTERNAL_SERVER_ERROR")))
                                //    {
                                //        status = -2;
                                //        err = YandexClasses.YandexOperators.ParseErrorResponse(result.Item3);
                                //    }
                                //}
                                //else
                                //{
                                //    if (result.Item2 != null)
                                //    {
                                //        label = result.Item2;
                                //    }
                                //}
                            }
                            else if (order.Тип == "OZON")
                            {
                                TimeSpan sleepPeriod = TimeSpan.FromSeconds(5);
                                int tryCount = 5;
                                while (true)
                                {
                                    var result = await OzonClasses.OzonOperators.GetLabels(_httpService, order.ClientId, order.AuthToken,
                                        new List<string> { order.MarketplaceId },
                                        stoppingToken);
                                    if (result.Item1 != null)
                                    {
                                        label = result.Item1;
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
                            if (!string.IsNullOrEmpty(err))
                                entity.Sp14055 = err;
                            if (((status > 0) && (entity.Sp13982 == 1)) || (status < 0))
                                entity.Sp13982 = status;
                            _context.Update(entity);
                            _context.РегистрацияИзмененийРаспределеннойИБ(13994, entity.Id);
                            if (label != null)
                            {
                                var orderBinary = await _context.VzOrderBinaries.OrderBy(x => x.RowId).FirstOrDefaultAsync(x => x.Id == order.OrderId && x.Extension.Trim().ToUpper() == "LABELS");
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
                            var result = await YandexClasses.YandexOperators.BuyerDetails(_httpService, row.CampaignId, row.ClientId, row.AuthToken, 
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
                                     join market in _context.Sc14042s on order.Sp14038 equals market.Id
                                     join items in _context.Sc14033s on order.Id equals items.Parentext
                                     join nom in _context.Sc84s on items.Sp14022 equals nom.Id
                                     join ed in _context.Sc75s on nom.Sp94 equals ed.Id
                                     join r in _context.Rg14021s on new { OrderId = order.Id, НоменклатураId = items.Sp14022 } equals new { OrderId = r.Sp14010, НоменклатураId = r.Sp14012 } into _r
                                     from r in _r.DefaultIfEmpty()
                                     where ((order.Sp13982 == 1) || (order.Sp13982 == 2)) && //order статус = 1 или 2 (грузовые места сформированы, лейблы скачены)
                                       (r.Period == dateRegTA) && (r.Sp14011 == 11) &&
                                       (((StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.YANDEX_MARKET) ||
                                       ((StinDeliveryPartnerType)order.Sp13985 == StinDeliveryPartnerType.OZON_LOGISTIC))
                                     group new { order, market, items, r, ed } by new { order.Id, order.Code, тип = market.Sp14155, campaignId = market.Code, clientId = market.Sp14053, token = market.Sp14054, edK = ed.Sp78 } into gr
                                     where gr.Sum(x => x.items.Sp14023) == (gr.Sum(x => x.r.Sp14017) / (gr.Key.edK == 0 ? 1 : gr.Key.edK))
                                     select new
                                     {
                                         OrderId = gr.Key.Id,
                                         MarketplaceId = gr.Key.Code.Trim(),
                                         Тип = gr.Key.тип.ToUpper().Trim(),
                                         CampaignId = gr.Key.campaignId.Trim(),
                                         ClientId = gr.Key.clientId.Trim(),
                                         AuthToken = gr.Key.token.Trim(),
                                     })
                       .Distinct()
                       .ToListAsync(stoppingToken);
                if ((fbsData != null) && (fbsData.Count > 0))
                    foreach (var row in fbsData)
                    {
                        var order = await _order.ПолучитьOrder(row.OrderId);
                        if ((order != null) && (order.Status == (int)StinOrderStatus.PROCESSING) && (order.SubStatus == (int)StinOrderSubStatus.READY_TO_SHIP))
                        {
                            needSaveChanges = true;
                            if (row.Тип == "ЯНДЕКС")
                            {
                                var statusResult = await YandexClasses.YandexOperators.OrderDetails(_httpService, row.CampaignId, row.ClientId, row.AuthToken, row.MarketplaceId, stoppingToken);
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
                                    var result = await YandexClasses.YandexOperators.ChangeStatus(_httpService,
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
                            else if (row.Тип == "OZON")
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
                            var statusResult = await YandexClasses.YandexOperators.OrderDetails(_httpService, row.CampaignId, row.ClientId, row.AuthToken, row.MarketplaceId, stoppingToken);
                            if ((statusResult.Item1 == YandexClasses.StatusYandex.DELIVERY) &&
                                (statusResult.Item2 == YandexClasses.SubStatusYandex.DELIVERY_SERVICE_RECEIVED))
                            {
                                if (order.DeliveryType == StinDeliveryType.PICKUP)
                                {
                                    var result = await YandexClasses.YandexOperators.ChangeStatus(_httpService,
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
                                var result = await YandexClasses.YandexOperators.ChangeStatus(_httpService,
                                    order.CampaignId, order.MarketplaceId,
                                    order.ClientId, order.AuthToken,
                                    YandexClasses.StatusYandex.DELIVERY, YandexClasses.SubStatusYandex.NotFound,
                                    (YandexClasses.DeliveryType)order.DeliveryType,
                                    stoppingToken);
                                if ((result != null) && result.Item1)
                                {
                                    if (order.DeliveryType == StinDeliveryType.PICKUP)
                                    {
                                        result = await YandexClasses.YandexOperators.ChangeStatus(_httpService,
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
                                      group new { r, order } by new
                                      {
                                          OrderId = order.Id,
                                          //MarketplaceId = order.Code, 
                                          НаборId = r.Sp11970,
                                          //CampaignId = market.Code,
                                          //ClientId = market.Sp14053,
                                          //Token = market.Sp14054
                                      } into gr
                                      where gr.Sum(x => x.r.Sp11972) != 0
                                      select new
                                      {
                                          OrderId = gr.Key.OrderId,
                                          //MarketplaceId = gr.Key.MarketplaceId.Trim(),
                                          НаборId = gr.Key.НаборId,
                                          //CampaignId = gr.Key.CampaignId.Trim(),
                                          //ClientId = gr.Key.ClientId.Trim(),
                                          //AuthToken = gr.Key.Token.Trim(),
                                      })
                                      .ToListAsync();
                    var реквизитыПроведенныхДокументов = new List<ОбщиеРеквизиты>();
                    foreach (var groupedData in data.GroupBy(x => x.OrderId))
                    {
                        var order = await _order.ПолучитьOrder(groupedData.Key);
                        var yandexResult = await YandexClasses.YandexOperators.ChangeStatus(_httpService,
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
                    if (реквизитыПроведенныхДокументов.Count > 0)
                        await _отменаНабора.ОбновитьАктивность(реквизитыПроведенныхДокументов);
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
        public async Task UpdatePrices(CancellationToken stoppingToken)
        {
            var yandexUrl = _configuration["Pricer:yandexUrl"];
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
                                            where !marketUsing.Ismark && 
                                                (marketUsing.Sp14158 == 1) && //Есть в каталоге
                                                ((marketUsing.Sp14150 == 1) || (marketUsing.Sp14174 < limitDate))
                                                //&& (market.Code.Trim() == "3530297616")
                                            select new
                                            {
                                                Id = market.Id,
                                                Тип = market.Sp14155.Trim(),
                                                Наименование = market.Descr.Trim(),
                                                CampaignId = market.Code.Trim(),
                                                ClientId = market.Sp14053.Trim(),
                                                AuthToken = market.Sp14054.Trim(),
                                                AuthSecret = market.Sp14195.Trim(),
                                                Authorization = market.Sp14077.Trim(),
                                                FeedId = market.Sp14154.Trim(),
                                                HexEncoding = market.Sp14153 == 1,
                                                Multiplayer = market.Sp14165
                                            })
                                            .Distinct()
                                            .ToListAsync();
                List<string> uploadIds = new List<string>();
                var uploadData = Enumerable.Repeat(new
                {
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
                                where !marketUsing.Ismark &&
                                    (marketUsing.Sp14158 == 1) && //Есть в каталоге
                                    ((marketUsing.Sp14150 == 1) || (marketUsing.Sp14174 < limitDate)) &&
                                    (marketUsing.Sp14147 == marketplace.Id)
                                    //&& ((vzTovar == null) || (vzTovar.Rozn <= 0))
                                select new
                                {
                                    Id = marketUsing.Id,
                                    NomCode = nom.Code,
                                    //NomArt = nom.Sp85.Trim(),
                                    ProductId = marketUsing.Sp14190.Trim(),
                                    Квант = nom.Sp14188,
                                    ЦенаРозн = vzTovar != null ? vzTovar.Rozn ?? 0 : 0,
                                    ЦенаСп = vzTovar != null ? vzTovar.RoznSp ?? 0 : 0,
                                    ЦенаЗакуп = vzTovar != null ? vzTovar.Zakup ?? 0 : 0,
                                    ЦенаФикс = marketUsing.Sp14148,
                                    Коэф = marketUsing.Sp14149,
                                })
                                .OrderBy(x => x.NomCode)
                                .Take(maxPerRequest);
                                //.ToList();
                    //if (data.Count > 0)
                    //    System.IO.File.WriteAllText(@"f:\\tmp\15\errors.txt", string.Join(Environment.NewLine, data.Select(x => x.NomArt)));
                    foreach (var d in data)
                    {
                        var Код = marketplace.HexEncoding ? d.NomCode.EncodeHexString() : d.NomCode;
                        var Цена = d.ЦенаСп > 0 ? Math.Min(d.ЦенаСп, d.ЦенаРозн) : d.ЦенаРозн;
                        if (d.ЦенаФикс > 0)
                        {
                            if (d.ЦенаФикс >= Цена)
                                Цена = d.ЦенаФикс;
                            else
                            {
                                var Порог = d.ЦенаЗакуп * (d.Коэф > 0 ? d.Коэф : (marketplace.Multiplayer > 0 ? marketplace.Multiplayer : checkCoeff));
                                if (Порог > d.ЦенаФикс)
                                {
                                    //удалить ЦенаФикс из markUsing ???
                                    //entry.Sp14148 = 0;
                                }
                                else
                                {
                                    Цена = d.ЦенаФикс;
                                }
                            }
                        }
                        if (long.TryParse(marketplace.FeedId, out long feedId))
                            feedId = 0;
                        if (Цена > 0)
                            uploadData.Add(new
                            {
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
                foreach (var priceData in uploadData.GroupBy(x => new { x.Тип, x.CampaignId, x.ClientId, x.AuthToken, x.AuthSecret, x.Authorization, x.FeedId }))
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
                            string.Format(yandexUrl, priceData.Key.CampaignId),
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
                        var result = await OzonClasses.OzonOperators.UpdatePrice(_httpService, priceData.Key.ClientId, priceData.Key.AuthToken,
                            priceData.Select(x =>
                            {
                                var oldPrice = x.ЦенаДоСкидки;
                                if ((oldPrice > 400) && (oldPrice <= 10000))
                                {
                                    if ((oldPrice - x.Цена) <= (oldPrice / 20))
                                        oldPrice = x.Цена;
                                }
                                else if (oldPrice > 10000)
                                {
                                    if ((oldPrice - x.Цена) <= 500)
                                        oldPrice = x.Цена;
                                }
                                return new OzonClasses.PriceRequest
                                {
                                    Offer_id = x.Код,
                                    Old_price = (x.Квант * oldPrice).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                                    Price = (x.Квант * x.Цена).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
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
                            _logger.LogError(result.Item2);
                            uploadIds.Clear();
                        }
                    }
                    else if (priceData.Key.Тип == "ALIEXPRESS")
                    {
                        //global API
                        var result = await AliExpressClasses.Functions.UpdatePriceGlobal(_httpService,
                            priceData.Key.ClientId, priceData.Key.AuthSecret, priceData.Key.Authorization,
                            priceData.Select(x =>
                            {
                                if (!long.TryParse(x.ProductId, out long productId))
                                    productId = 0;
                                return new AliExpressClasses.PriceProductGlobal
                                {
                                    Product_id = productId,
                                    Multiple_sku_update_list = new List<AliExpressClasses.PriceSku>
                                    {
                                        new AliExpressClasses.PriceSku
                                        {
                                            Sku_code = x.Код,
                                            Discount_price = (x.Квант * x.Цена).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                                            Price = x.ЦенаДоСкидки > 0 ? (x.Квант * x.ЦенаДоСкидки).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) : (x.Квант * x.Цена).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                                        }
                                    }
                                };
                            }).ToList(),
                            stoppingToken);
                        if (result.Item1 != null)
                        {
                            var stringData = result.Item1.Select(x => x.ToString()).ToList();
                            uploadIds.AddRange(priceData.Where(x => stringData.Contains(x.ProductId)).Select(x => x.Id));
                            if (!string.IsNullOrEmpty(result.Item2))
                                _logger.LogError(result.Item2);
                        }
                        else
                        {
                            if (_context.Database.CurrentTransaction != null)
                                _context.Database.CurrentTransaction.Rollback();
                            _logger.LogError(result.Item2);
                            uploadIds.Clear();
                        }
                        //local API
                        //var result = await AliExpressClasses.Functions.UpdatePrice(_httpService, priceData.Key.AuthToken,
                        //    priceData.Select(x => new AliExpressClasses.PriceProduct
                        //    {

                        //        Product_id = x.ProductId,
                        //        Skus = new List<AliExpressClasses.PriceSku> 
                        //        { 
                        //            new AliExpressClasses.PriceSku
                        //            {
                        //                Sku_code = x.Код,
                        //                Price = (x.Квант * x.Цена).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                        //            } 
                        //        }
                        //    }).ToList(),
                        //    stoppingToken);
                        //if (result.Item1 != null)
                        //{
                        //    uploadIds.AddRange(priceData.Where(x => result.Item1.Contains(x.Код)).Select(x => x.Id));
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
                    }
                    else if (priceData.Key.Тип == "WILDBERRIES")
                    {
                        var result = await WbClasses.Functions.UpdatePrice(_httpService, priceData.Key.AuthToken,
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
                                var entities = await _context.Sc14152s.Where(x => uploadIds.Contains(x.Id)).ToListAsync();
                                foreach (var entity in entities)
                                {
                                    entity.Sp14150 = 0;
                                    entity.Sp14174 = DateTime.Today;
                                    _context.Update(entity);
                                    _context.РегистрацияИзмененийРаспределеннойИБ(14152, entity.Id);
                                }
                                await _context.SaveChangesAsync(stoppingToken);

                                if (_context.Database.CurrentTransaction != null)
                                    tran.Commit();
                                break;
                            }
                            catch (DbUpdateException db_ex)
                            {
                                if (_context.Database.CurrentTransaction != null)
                                    _context.Database.CurrentTransaction.Rollback();
                                _logger.LogError(db_ex.InnerException.ToString());
                                if (--tryCount == 0)
                                {
                                    break;
                                }
                                await Task.Delay(sleepPeriod);
                            }
                            catch (Exception ex)
                            {
                                if (_context.Database.CurrentTransaction != null)
                                    _context.Database.CurrentTransaction.Rollback();
                                _logger.LogError(ex.Message);
                                if (--tryCount == 0)
                                {
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
        private async Task UpdateQuantum(string campaignId, string clientId, string authToken, string marketplaceId, 
            bool hexEncoding, 
            IList<YandexClasses.OfferMappingEntry> offerEntries,
            CancellationToken cancellationToken)
        {
            var codes = offerEntries.Select(x => hexEncoding ? x.Offer.ShopSku.DecodeHexString() : x.Offer.ShopSku);
            var localData = await (from markUse in _context.Sc14152s
                                   join nom in _context.Sc84s on markUse.Parentext equals nom.Id
                                   where codes.Contains(nom.Code) &&
                                     markUse.Sp14147 == marketplaceId
                                   select new
                                   {
                                       NomCode = hexEncoding ? nom.Code.EncodeHexString() : nom.Code,
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
                    var result = await YandexClasses.YandexOperators.UpdateOfferEntries(_httpService, campaignId, clientId, authToken, updateData, cancellationToken);
                    if (!string.IsNullOrEmpty(result.Item2))
                        _logger.LogError(result.Item2);
                }
            }
        }
        private async Task UpdateCatalogInfo(object data, string marketplaceId, bool hexEncoding, CancellationToken stoppingToken)
        {
            var skus = new List<string>();
            var productIdToSku = new Dictionary<string, string>();
            if (data is IList<YandexClasses.OfferMappingEntry>)
            {
                foreach (var entry in (data as IList<YandexClasses.OfferMappingEntry>))
                    if ((entry.Offer != null) && (entry.Offer.ProcessingState != null))
                    {
                        var nomCode = entry.Offer.ShopSku;
                        if (hexEncoding)
                            try
                            {
                                nomCode = nomCode.DecodeHexString();
                            }
                            catch
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
                        var nomCode = item.Offer_id;
                        if (hexEncoding)
                            try
                            {
                                nomCode = nomCode.DecodeHexString();
                            }
                            catch
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
                            var nomCode = x.Code;
                            if (hexEncoding)
                                try
                                {
                                    nomCode = nomCode.DecodeHexString();
                                }
                                catch
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
            else if (data is IList<WbClasses.CatalogInfo>)
            {
                var nmIds = (data as IList<WbClasses.CatalogInfo>).Select(d => d.NmId.ToString()).ToList();
                productIdToSku = await (from markUse in _context.Sc14152s
                                        join nom in _context.Sc84s on markUse.Parentext equals nom.Id
                                        where nmIds.Contains(markUse.Sp14190) && markUse.Sp14147 == marketplaceId
                                        select new
                                        {
                                            Sku = hexEncoding ? nom.Code.DecodeHexString() : nom.Code,
                                            ProductId = markUse.Sp14190.Trim()
                                        })
                    .ToDictionaryAsync(k => k.ProductId, v => v.Sku, stoppingToken);
                skus = productIdToSku.Select(x => x.Value).Distinct().ToList();
            }
            using var tran = await _context.Database.BeginTransactionAsync();
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
                                    Sp14150 = 1, //Флаг - пусть цены обновятся
                                    Sp14158 = 1, //Есть в каталоге
                                    Sp14174 = Common.min1cDate, //UpdatedAt = Min1C !!!
                                    Sp14178 = Common.min1cDate, //StockUpdatedAt
                                    Sp14179 = 1, //StockUpdated - пусть stock обновится
                                    Sp14187 = 0, //Quantum = 0
                                    Sp14190 = productId,
                                    Sp14198 = 0
                                };
                                await _context.Sc14152s.AddAsync(entity);
                                _context.РегистрацияИзмененийРаспределеннойИБ(14152, entity.Id);
                            }
                        }
                        else
                            foreach (var entity in entities.Where(x => (x.Sp14158 != 1) || (x.Sp14190.Trim() != productId))) 
                            {
                                entity.Sp14158 = 1;
                                if (productIdToSku.Count > 0)
                                    entity.Sp14190 = productId;
                                _context.Update(entity);
                                _context.РегистрацияИзмененийРаспределеннойИБ(14152, entity.Id);
                            }
                        await _context.SaveChangesAsync();
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
        private async Task ParseNextPageCatalogRequest(string url, string campaignId, string clientId, string authToken, int limit, string nextPageToken, string marketplaceId, string marketplaceModel, bool hexEncoding, CancellationToken stoppingToken)
        {
            var result = await YandexClasses.YandexOperators.Exchange<YandexClasses.OfferMappingEntriesResponse>(_httpService,
                string.Format(url, campaignId, limit) + (string.IsNullOrEmpty(nextPageToken) ? "" : "&page_token=" + nextPageToken),
                HttpMethod.Get,
                clientId,
                authToken,
                null,
                stoppingToken);

            //var result = await YandexClasses.YandexOperators.YandexExchange(null, string.Format(url, campaignId, limit) + (string.IsNullOrEmpty(nextPageToken) ? "" : "&page_token=" + nextPageToken), HttpMethod.Get, clientId, authToken, null);
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
                            await UpdateCatalogInfo(catalogResponse.Result.OfferMappingEntries, marketplaceId, hexEncoding, stoppingToken);
                            if ((catalogResponse.Result.Paging != null) && (!string.IsNullOrEmpty(catalogResponse.Result.Paging.NextPageToken)))
                            {
                                await ParseNextPageCatalogRequest(url, campaignId, clientId, authToken, limit, catalogResponse.Result.Paging.NextPageToken, marketplaceId, marketplaceModel, hexEncoding, stoppingToken);
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
        private async Task ParseNextPageCatalogOzon(string clientId, string authToken, int limit, string nextPageToken, string marketplaceId, bool hexEncoding, CancellationToken stoppingToken)
        {
            var result = await OzonClasses.OzonOperators.ParseCatalog(_httpService, clientId, authToken, nextPageToken, limit, stoppingToken);
            if (result.Item2 != null && !string.IsNullOrEmpty(result.Item2))
            {
                _logger.LogError(result.Item2);
            }
            if (result.Item1 != null)
                try
                {
                    await UpdateCatalogInfo(result.Item1.Items, marketplaceId, hexEncoding, stoppingToken);
                    if (!string.IsNullOrEmpty(result.Item1.Last_id))
                    {
                        await ParseNextPageCatalogOzon(clientId, authToken, limit, result.Item1.Last_id, marketplaceId, hexEncoding, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Internal (ParseNextPageCatalogOzon) : " + ex.Message);
                }
        }
        public async Task ParseMextPageCatalogAliExpressGlobal(string appKey, string appSecret, string authToken, int currentPage, int limit, string marketplaceId, bool hexEncoding, CancellationToken stoppingToken)
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
                            marketplaceId, hexEncoding, stoppingToken);
                    }
                    var totalPages = result.Item1.Total_page.HasValue ? result.Item1.Total_page.Value : 1;
                    if (totalPages > currentPage)
                    {
                        await ParseMextPageCatalogAliExpressGlobal(appKey, appSecret, authToken, currentPage+1, limit, marketplaceId, hexEncoding, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("ParseMextPageCatalogAliExpress internal : " + ex.Message);
                }
        }
        public async Task ParseMextPageCatalogAliExpress(string authToken, string lastProductId, int limit, string marketplaceId, bool hexEncoding, CancellationToken stoppingToken)
        {
            var result = await AliExpressClasses.Functions.GetCatalogInfo(_httpService, authToken, lastProductId, limit, stoppingToken);
            if (result.Item2 != null && !string.IsNullOrEmpty(result.Item2))
            {
                _logger.LogError(result.Item2);
            }
            if ((result.Item1 != null) && (result.Item1.Count > 0))
                try
                {
                    await UpdateCatalogInfo(result.Item1, marketplaceId, hexEncoding, stoppingToken);
                    lastProductId = result.Item1.LastOrDefault()?.Id;
                    if (!string.IsNullOrEmpty(lastProductId))
                    {
                        await ParseMextPageCatalogAliExpress(authToken, lastProductId, limit, marketplaceId, hexEncoding, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("ParseMextPageCatalogAliExpress internal : " + ex.Message);
                }
        }
        private async Task ParseWildberriesCatalog(string authToken, string marketplaceId, bool hexEncoding, CancellationToken stoppingToken)
        {
            var result = await WbClasses.Functions.GetCatalogInfo(_httpService, authToken, stoppingToken);
            if (result.Item2 != null && !string.IsNullOrEmpty(result.Item2))
            {
                _logger.LogError(result.Item2);
            }
            if ((result.Item1 != null) && (result.Item1.Count > 0))
                try
                {
                    await UpdateCatalogInfo(result.Item1, marketplaceId, hexEncoding, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError("ParseWildberriesCatalog internal : " + ex.Message);
                }
        }
        public async Task CheckCatalog(CancellationToken stoppingToken)
        {
            var yandexUrl = _configuration["Catalog:yandexUrl"];
            if (int.TryParse(_configuration["Catalog:maxEntriesResponse"], out int maxResponseEntries))
                maxResponseEntries = Math.Max(maxResponseEntries, 1);
            else
                maxResponseEntries = 100;
            try
            {
                var marketplaceIds = await (from market in _context.Sc14042s 
                                            where !market.Ismark
                                                //&& (market.Sp14155.Trim() == "Яндекс")
                                                //&& (market.Code.Trim() == "23503334320000")
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
                                                HexEncoding = market.Sp14153 == 1
                                            })
                                            .ToListAsync();
                foreach (var marketplace in marketplaceIds)
                {
                    if (marketplace.Тип == "ЯНДЕКС")
                        await ParseNextPageCatalogRequest(
                            yandexUrl, 
                            marketplace.CampaignId, 
                            marketplace.ClientId, 
                            marketplace.AuthToken, 
                            maxResponseEntries, 
                            null,
                            marketplace.Id, 
                            marketplace.Модель,
                            marketplace.HexEncoding, 
                            stoppingToken);
                    else if (marketplace.Тип == "OZON")
                    {
                        await ParseNextPageCatalogOzon(
                            marketplace.ClientId, marketplace.AuthToken, maxResponseEntries, null,
                            marketplace.Id, marketplace.HexEncoding, stoppingToken);
                    }
                    else if (marketplace.Тип == "ALIEXPRESS")
                    {
                        //await ParseMextPageCatalogAliExpressGlobal(marketplace.ClientId, marketplace.Authorization, marketplace.AuthToken,
                        //    1, 50, marketplace.Id, marketplace.HexEncoding, stoppingToken);
                        await ParseMextPageCatalogAliExpress(
                            marketplace.AuthToken, "0", 50, marketplace.Id, marketplace.HexEncoding, stoppingToken);
                    }
                    else if (marketplace.Тип == "WILDBERRIES")
                    {
                        await ParseWildberriesCatalog(marketplace.AuthToken, marketplace.Id, marketplace.HexEncoding, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
        public async Task UpdateStock(bool regular, CancellationToken stoppingToken)
        {
            var defFirmaId = _configuration["Stocker:" + _configuration["Stocker:Firma"] + ":FirmaId"];
            if (int.TryParse(_configuration["Stocker:maxPerRequestAliexpress"], out int maxPerRequestAli))
                maxPerRequestAli = Math.Max(maxPerRequestAli, 1);
            else
                maxPerRequestAli = 20;
            if (int.TryParse(_configuration["Stocker:maxPerRequestOzon"], out int maxPerRequestOzon))
                maxPerRequestOzon = Math.Max(maxPerRequestOzon, 1);
            else
                maxPerRequestOzon = 100;
            try
            {
                var marketplaceIds = await (from market in _context.Sc14042s
                                            where !market.Ismark
                                                && (market.Sp14177 == 1)
                                                && (string.IsNullOrEmpty(defFirmaId) ? true : market.Parentext == defFirmaId)
                                            select new
                                            {
                                                Id = market.Id,
                                                Тип = market.Sp14155.Trim(),
                                                //Наименование = market.Descr.Trim(),
                                                FirmaId = market.Parentext,
                                                ClientId = market.Sp14053.Trim(),
                                                AuthToken = market.Sp14054.Trim(),
                                                AuthSecret = market.Sp14195.Trim(),
                                                Authorization = market.Sp14077.Trim(),
                                                Code = market.Code.Trim(),
                                                HexEncoding = market.Sp14153 == 1
                                            })
                                            .ToListAsync(stoppingToken);
                foreach (var marketplace in marketplaceIds)
                {
                    if (marketplace.Тип.ToUpper() == "OZON")
                    {
                        await UpdateOzonStock(
                            marketplace.ClientId, marketplace.AuthToken, regular, maxPerRequestOzon,
                            marketplace.Id, marketplace.FirmaId, marketplace.HexEncoding, marketplace.Code, stoppingToken);
                    }
                    else if (marketplace.Тип.ToUpper() == "ALIEXPRESS")
                    {
                        await UpdateAliExpressStock(marketplace.ClientId, 
                            marketplace.AuthSecret, marketplace.Authorization, regular, maxPerRequestAli,
                            marketplace.Id, marketplace.FirmaId, marketplace.HexEncoding, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
        public async Task UpdateOzonStock(string clientId, string authToken, bool regular, int limit,
            string marketplaceId, string firmaId, bool hexEncoding, string skladCode, CancellationToken stoppingToken)
        {
            var data = await ((from markUse in _context.Sc14152s
                              join nom in _context.Sc84s on markUse.Parentext equals nom.Id
                              where (markUse.Sp14147 == marketplaceId) &&
                                (markUse.Sp14158 == 1) && //Есть в каталоге 
                                //(markUse.Sp14179 == -3) && (markUse.Sp14178 < DateTime.Now.AddSeconds(-100))
                                (((regular ? (markUse.Sp14179 == 1) : (markUse.Sp14179 == -2)) && 
                                  (markUse.Sp14178 < DateTime.Now.AddMinutes(-2))) || 
                                 (markUse.Sp14178.Date != DateTime.Today))
                              select new
                              {
                                  Id = markUse.Id,
                                  Locked = markUse.Ismark,
                                  NomId = markUse.Parentext,
                                  OfferId = hexEncoding ? nom.Code.EncodeHexString() : nom.Code,
                                  Квант = nom.Sp14188,
                                  WarehouseId = string.IsNullOrWhiteSpace(markUse.Sp14190) ? skladCode : markUse.Sp14190,
                                  UpdatedAt = markUse.Sp14178,
                                  UpdatedFlag = markUse.Sp14179 == 1
                              })
                .OrderByDescending(x => x.UpdatedFlag)
                .ThenBy(x => x.UpdatedAt)
                .Take(limit))
                .ToListAsync(stoppingToken);
                              
            if ((data != null) && (data.Count > 0))
            {
                var checkResult = await OzonClasses.OzonOperators.ProductNotReady(_httpService, clientId, authToken,
                    data.Select(x => x.OfferId).ToList(),
                    stoppingToken);
                if (checkResult.Item2 != null && !string.IsNullOrEmpty(checkResult.Item2))
                {
                    _logger.LogError(checkResult.Item2);
                }
                var notReadyIds = new List<string>();
                if ((checkResult.Item1 != null) && (checkResult.Item1.Count > 0))
                {
                    notReadyIds = data.Where(x => checkResult.Item1.Contains(x.OfferId)).Select(x => x.Id).ToList();
                }

                var listIds = data.Where(x => !notReadyIds.Contains(x.Id)).Select(x => x.Id).ToList();
                int tryCount = 5;
                TimeSpan sleepPeriod = TimeSpan.FromSeconds(1);
                bool success = false;
                while (true)
                {
                    using var tran = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        _context.Sc14152s
                            .Where(x => listIds.Contains(x.Id))
                            .ToList()
                            .ForEach(x => { x.Sp14179 = -1; });
                        _context.Sc14152s
                            .Where(x => notReadyIds.Contains(x.Id))
                            .ToList()
                            .ForEach(x => { x.Sp14178 = DateTime.Now; });
                        await _context.SaveChangesAsync(stoppingToken);
                        if (_context.Database.CurrentTransaction != null)
                            tran.Commit();
                        success = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (_context.Database.CurrentTransaction != null)
                            _context.Database.CurrentTransaction.Rollback();
                        _logger.LogError(ex.Message);
                        if (--tryCount == 0)
                        {
                            break;
                        }
                        await Task.Delay(sleepPeriod);
                    }
                }
                if (success && (listIds.Count > 0))
                {
                    var разрешенныеФирмы = await _фирма.ПолучитьСписокРазрешенныхФирмAsync(firmaId);
                    
                    List<string> списокСкладов = await _склад.ПолучитьСкладIdОстатковMarketplace();

                    var списокНоменклатуры = await _номенклатура.ПолучитьСвободныеОстатки(разрешенныеФирмы, списокСкладов, data.Where(x => !notReadyIds.Contains(x.Id)).Select(x => x.NomId).ToList(), false);
                    var stockData = new List<OzonClasses.StockRequest>();
                    foreach (var item in data.Where(x => !notReadyIds.Contains(x.Id)))
                    {
                        var номенклатура = списокНоменклатуры.Where(x => x.Id == item.NomId).FirstOrDefault();
                        if (номенклатура != null)
                        {
                            long остаток = 0;
                            if (item.Квант > 1)
                            {
                                остаток = (int)((номенклатура.Остатки
                                    .Where(x => x.СкладId == Common.SkladEkran)
                                    .Sum(x => x.СвободныйОстаток) / номенклатура.Единица.Коэффициент) / item.Квант);
                            }
                            else
                                остаток = (long)(номенклатура.Остатки.Sum(x => x.СвободныйОстаток) / номенклатура.Единица.Коэффициент);
                            if (!long.TryParse(item.WarehouseId, out long WarehouseId))
                                WarehouseId = 0;
                            stockData.Add(new OzonClasses.StockRequest
                            {
                                Offer_id = hexEncoding ? номенклатура.Code.EncodeHexString() : номенклатура.Code,
                                Stock = item.Locked ? 0 : остаток,
                                Warehouse_id = WarehouseId
                            });
                        }
                    }
                    var result = await OzonClasses.OzonOperators.UpdateStock(_httpService, clientId, authToken,
                        stockData,
                        stoppingToken);
                    if (result.errorMessage != null && !string.IsNullOrEmpty(result.errorMessage))
                    {
                        _logger.LogError(result.errorMessage);
                    }
                    List<string> uploadIds = new List<string>();
                    List<string> errorIds = new List<string>();
                    if (result.updatedOfferIds?.Count > 0)
                    {
                        var nomIds = списокНоменклатуры
                            .Where(x => result.updatedOfferIds.Contains(hexEncoding ? x.Code.EncodeHexString() : x.Code))
                            .Select(x => x.Id);
                        uploadIds = data.Where(x => nomIds.Contains(x.NomId)).Select(x => x.Id).ToList();
                    }
                    if (result.errorOfferIds?.Count > 0)
                    {
                        var nomIds = списокНоменклатуры
                            .Where(x => result.errorOfferIds.Contains(hexEncoding ? x.Code.EncodeHexString() : x.Code))
                            .Select(x => x.Id);
                        errorIds = data.Where(x => nomIds.Contains(x.NomId)).Select(x => x.Id).ToList();
                    }
                    if ((uploadIds.Count > 0) || (errorIds.Count > 0))
                    {
                        tryCount = 5;
                        while (true)
                        {
                            using var tran = await _context.Database.BeginTransactionAsync();
                            try
                            {
                                if (uploadIds.Count > 0)
                                {
                                    var notYetUpdated = await _context.Sc14152s
                                        .Where(x => uploadIds.Contains(x.Id) && (x.Sp14179 == -1))
                                        .ToListAsync(stoppingToken);
                                    foreach (var entity in notYetUpdated)
                                    {
                                        entity.Sp14179 = 0;
                                        entity.Sp14178 = DateTime.Now;  //DateTime.Today;
                                        _context.Update(entity);
                                        _context.РегистрацияИзмененийРаспределеннойИБ(14152, entity.Id);
                                    }
                                }
                                if (errorIds.Count > 0)
                                {
                                    var notYetUpdatedError = await _context.Sc14152s
                                        .Where(x => errorIds.Contains(x.Id) && (x.Sp14179 == -1))
                                        .ToListAsync(stoppingToken);
                                    foreach (var entity in notYetUpdatedError)
                                    {
                                        entity.Sp14179 = -2;
                                        entity.Sp14178 = DateTime.Now;
                                        _context.Update(entity);
                                        _context.РегистрацияИзмененийРаспределеннойИБ(14152, entity.Id);
                                    }
                                }
                                await _context.SaveChangesAsync(stoppingToken);
                                if (_context.Database.CurrentTransaction != null)
                                    tran.Commit();
                                break;
                            }
                            catch (Exception ex)
                            {
                                if (_context.Database.CurrentTransaction != null)
                                    _context.Database.CurrentTransaction.Rollback();
                                _logger.LogError(ex.Message);
                                if (--tryCount == 0)
                                {
                                    break;
                                }
                                await Task.Delay(sleepPeriod);
                            }
                        }
                    }
                }
            }
        }
        public async Task UpdateAliExpressStock(string appKey, string appSecret, string authorization, bool regular, int limit,
            string marketplaceId, string firmaId, bool hexEncoding, CancellationToken stoppingToken)
        {
            var data = await ((from markUse in _context.Sc14152s
                               join nom in _context.Sc84s on markUse.Parentext equals nom.Id
                               where (markUse.Sp14147 == marketplaceId) &&
                                 (markUse.Sp14158 == 1) && //Есть в каталоге 
                                 ((regular ? (markUse.Sp14179 == 1) : (markUse.Sp14179 == -2)) || 
                                 (markUse.Sp14178 != DateTime.Today))
                               select new
                               {
                                   Id = markUse.Id,
                                   Locked = markUse.Ismark,
                                   NomId = markUse.Parentext,
                                   ProductId = markUse.Sp14190.Trim(),
                                   Sku = hexEncoding ? nom.Code.EncodeHexString() : nom.Code,
                                   Квант = nom.Sp14188,
                                   UpdatedAt = markUse.Sp14178,
                                   UpdatedFlag = markUse.Sp14179 == 1
                               })
                .OrderByDescending(x => x.UpdatedFlag)
                .ThenBy(x => x.UpdatedAt)
                .Take(limit))
                .ToListAsync(stoppingToken);

            if ((data != null) && (data.Count > 0))
            {
                //var checkResult = await OzonClasses.OzonOperators.ProductNotReady(_httpService, clientId, authToken,
                //    data.Select(x => x.OfferId).ToList(),
                //    stoppingToken);
                //if (checkResult.Item2 != null && !string.IsNullOrEmpty(checkResult.Item2))
                //{
                //    _logger.LogError(checkResult.Item2);
                //}
                //var notReadyIds = new List<string>();
                //if ((checkResult.Item1 != null) && (checkResult.Item1.Count > 0))
                //{
                //    notReadyIds = data.Where(x => checkResult.Item1.Contains(x.OfferId)).Select(x => x.Id).ToList();
                //}

                //var listIds = data.Where(x => !notReadyIds.Contains(x.Id)).Select(x => x.Id).ToList();
                int tryCount = 5;
                TimeSpan sleepPeriod = TimeSpan.FromSeconds(1);
                bool success = false;
                while (true)
                {
                    using var tran = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        _context.Sc14152s
                            .Where(x => data.Select(d => d.Id).Contains(x.Id))
                            .ToList()
                            .ForEach(x => { x.Sp14179 = -1; });
                        //_context.Sc14152s
                        //    .Where(x => notReadyIds.Contains(x.Id))
                        //    .ToList()
                        //    .ForEach(x => { x.Sp14178 = DateTime.Today; });
                        await _context.SaveChangesAsync(stoppingToken);
                        if (_context.Database.CurrentTransaction != null)
                            tran.Commit();
                        success = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (_context.Database.CurrentTransaction != null)
                            _context.Database.CurrentTransaction.Rollback();
                        _logger.LogError(ex.Message);
                        if (--tryCount == 0)
                        {
                            break;
                        }
                        await Task.Delay(sleepPeriod);
                    }
                }
                if (success)
                {
                    var разрешенныеФирмы = await _фирма.ПолучитьСписокРазрешенныхФирмAsync(firmaId);

                    List<string> списокСкладов = await _склад.ПолучитьСкладIdОстатковMarketplace();

                    var списокНоменклатуры = await _номенклатура.ПолучитьСвободныеОстатки(разрешенныеФирмы, списокСкладов, data.Select(x => x.NomId).ToList(), false);
                    var stockData = new List<AliExpressClasses.StockProductGlobal>();
                    //var stockData = new List<AliExpressClasses.Product>();
                    foreach (var item in data)
                    {
                        var номенклатура = списокНоменклатуры.Where(x => x.Id == item.NomId).FirstOrDefault();
                        if (номенклатура != null)
                        {
                            long остаток = 0;
                            if (item.Квант > 1)
                            {
                                остаток = (int)((номенклатура.Остатки
                                    .Where(x => x.СкладId == Common.SkladEkran)
                                    .Sum(x => x.СвободныйОстаток) / номенклатура.Единица.Коэффициент) / item.Квант);
                            }
                            else
                                остаток = (long)(номенклатура.Остатки.Sum(x => x.СвободныйОстаток) / номенклатура.Единица.Коэффициент);
                            //global API
                            if (!long.TryParse(item.ProductId, out long productId))
                                productId = 0;
                            stockData.Add(new AliExpressClasses.StockProductGlobal
                            {
                                Product_id = productId,
                                Multiple_sku_update_list = new List<AliExpressClasses.StockSku>
                                {
                                    new AliExpressClasses.StockSku
                                    {
                                        Sku_code = hexEncoding ? номенклатура.Code.EncodeHexString() : номенклатура.Code,
                                        Inventory = (item.Locked ? 0 : остаток).ToString()
                                    }
                                }
                            });
                            //local API
                            //stockData.Add(new AliExpressClasses.Product
                            //{
                            //    Product_id = item.ProductId,
                            //    Skus = new List<AliExpressClasses.StockSku>
                            //    {
                            //        new AliExpressClasses.StockSku 
                            //        { 
                            //            Sku_code = hexEncoding ? номенклатура.Code.EncodeHexString() : номенклатура.Code,
                            //            Inventory = (item.Locked ? 0 : остаток).ToString()
                            //        }
                            //    }
                            //});
                        }
                    }
                    var result = await AliExpressClasses.Functions.UpdateStockGlobal(_httpService,
                        appKey, appSecret, authorization, stockData, stoppingToken);
                    //var result = await AliExpressClasses.Functions.UpdateStock(_httpService, authToken,
                    //    stockData,
                    //    stoppingToken);
                    if (result.errorMessage != null && !string.IsNullOrEmpty(result.errorMessage))
                    {
                        _logger.LogError(result.errorMessage);
                    }
                    List<string> uploadIds = new List<string>();
                    List<string> errorIds = new List<string>();
                    if (result.updatedIds != null && result.updatedIds.Count > 0)
                    {
                        uploadIds.AddRange(data
                            .Where(x => result.updatedIds.Select(y => y.ToString()).Contains(x.ProductId))
                            .Select(x => x.Id));
                        //var nomIds = result.Item1.Select(x =>
                        //    списокНоменклатуры
                        //                .Where(y => (hexEncoding ? y.Code.EncodeHexString() : y.Code) == x)
                        //                .Select(z => z.Id).FirstOrDefault());
                        //uploadIds.AddRange(data.Where(x => nomIds.Contains(x.NomId)).Select(x => x.Id));
                    }
                    if (result.errorIds?.Count > 0)
                    {
                        errorIds = data
                            .Where(x => result.errorIds.Select(y => y.ToString()).Contains(x.ProductId))
                            .Select(x => x.Id)
                            .ToList();
                    }
                    if ((uploadIds.Count > 0) || (errorIds.Count > 0))
                    {
                        tryCount = 5;
                        while (true)
                        {
                            using var tran = await _context.Database.BeginTransactionAsync();
                            try
                            {
                                if (uploadIds.Count > 0)
                                {
                                    var notYetUpdated = await _context.Sc14152s
                                        .Where(x => uploadIds.Contains(x.Id) && (x.Sp14179 == -1))
                                        .ToListAsync();
                                    foreach (var entity in notYetUpdated)
                                    {
                                        entity.Sp14179 = 0;
                                        entity.Sp14178 = DateTime.Today;
                                        _context.Update(entity);
                                        _context.РегистрацияИзмененийРаспределеннойИБ(14152, entity.Id);
                                    }
                                }
                                if (errorIds.Count > 0)
                                {
                                    var notYetUpdatedError = await _context.Sc14152s
                                        .Where(x => errorIds.Contains(x.Id) && (x.Sp14179 == -1))
                                        .ToListAsync();
                                    foreach (var entity in notYetUpdatedError)
                                    {
                                        entity.Sp14179 = -2;
                                        entity.Sp14178 = DateTime.Today;
                                        _context.Update(entity);
                                        _context.РегистрацияИзмененийРаспределеннойИБ(14152, entity.Id);
                                    }
                                }
                                await _context.SaveChangesAsync(stoppingToken);
                                if (_context.Database.CurrentTransaction != null)
                                    tran.Commit();
                                break;
                            }
                            catch (Exception ex)
                            {
                                if (_context.Database.CurrentTransaction != null)
                                    _context.Database.CurrentTransaction.Rollback();
                                _logger.LogError(ex.Message);
                                if (--tryCount == 0)
                                {
                                    break;
                                }
                                await Task.Delay(sleepPeriod);
                            }
                        }
                    }
                }
            }
        }
        public async Task RefreshOrders(CancellationToken stoppingToken)
        {
            try
            {
                var marketplaceIds = await (from market in _context.Sc14042s
                                            where !market.Ismark
                                                //&& (market.Sp14177 == 1)
                                                //&& (market.Sp14155.Trim() == "AliExpress")
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
                                                HexEncoding = market.Sp14153 == 1
                                            })
                                            .ToListAsync();
                foreach (var marketplace in marketplaceIds)
                {
                    if (marketplace.Тип == "OZON")
                    {
                        await GetOzonNewOrders(marketplace.ClientId, marketplace.AuthToken, 
                            marketplace.Id, marketplace.FirmaId, marketplace.CustomerId, marketplace.DogovorId, 
                            marketplace.HexEncoding, stoppingToken);
                        await GetOzonCancelOrders(marketplace.ClientId, marketplace.AuthToken,
                            marketplace.Id, marketplace.FirmaId, marketplace.CustomerId, marketplace.DogovorId,
                            marketplace.HexEncoding, stoppingToken);
                        //moved to Slow
                        //await GetOzonDeliveringOrders(marketplace.Id, marketplace.ClientId, marketplace.AuthToken,
                        //    marketplace.Id, marketplace.FirmaId, marketplace.CustomerId, marketplace.DogovorId,
                        //    marketplace.HexEncoding, stoppingToken);
                    }
                    else if (marketplace.Тип == "ЯНДЕКС")
                    {
                        await GetYandexCancelOrders(marketplace.Code, marketplace.ClientId, marketplace.AuthToken, marketplace.Id, stoppingToken);
                    }
                    else if (marketplace.Тип == "ALIEXPRESS")
                    {
                        await GetAliExpressOrders(marketplace.ClientId, marketplace.AuthSecret, marketplace.Authorization,
                            marketplace.Id,
                            marketplace.FirmaId, marketplace.CustomerId, marketplace.DogovorId, marketplace.HexEncoding,
                            stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("RefreshOrders : " + ex.Message);
            }
        }
        public async Task RefreshSlowOrders(CancellationToken stoppingToken)
        {
            try
            {
                var marketplaceIds = await (from market in _context.Sc14042s
                                            where !market.Ismark
                                            //&& (market.Sp14177 == 1)
                                            //&& (market.Sp14155.Trim() == "AliExpress")
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
                                                HexEncoding = market.Sp14153 == 1
                                            })
                                            .ToListAsync();
                foreach (var marketplace in marketplaceIds)
                {
                    if (marketplace.Тип == "OZON")
                    {
                        await GetOzonDeliveringOrders(marketplace.Id, marketplace.ClientId, marketplace.AuthToken,
                            marketplace.Id, marketplace.FirmaId, marketplace.CustomerId, marketplace.DogovorId,
                            marketplace.HexEncoding, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("RefreshSlowOrders : " + ex.Message);
            }
        }
        private async Task GetAliExpressOrders(string appKey, string appSecret, string authorization,
            string marketplaceId,
            string firmaId, string customerId, string dogovorId, bool hexEncoding,
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
                                        var номенклатураCodes = orderDto.Product_list.Order_product_dto.Select(x => hexEncoding ? x.Sku_code.DecodeHexString() : x.Sku_code).ToList();
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
                                                                .Where(b => (hexEncoding ? b.Sku_code.DecodeHexString() : b.Sku_code) == номенклатура.Code)
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
                                                НоменклатураId = hexEncoding ? item.Sku_code.DecodeHexString() : item.Sku_code,
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
                                           hexEncoding,
                                           "0",
                                           "",
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
                                    (orderDto.End_reason == "pay_timeout") &&
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
        private async Task GetOzonNewOrders(string clientId, string authToken, string id, string firmaId, string customerId, string dogovorId, bool hexEncoding, CancellationToken stoppingToken)
        {
            if (int.TryParse(_configuration["Orderer:maxPerRequest"], out int maxPerRequest))
                maxPerRequest = Math.Max(maxPerRequest, 1);
            else
                maxPerRequest = 100;
            var result = await OzonClasses.OzonOperators.UnfulfilledOrders(_httpService, clientId, authToken,
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
                            var номенклатураCodes = posting.Products.Select(x => hexEncoding ? x.Offer_id.DecodeHexString() : x.Offer_id).ToList();
                            var НоменклатураList = await _номенклатура.ПолучитьСвободныеОстатки(
                                разрешенныеФирмы,
                                списокСкладов,
                                номенклатураCodes,
                                true);
                            var nomQuantums = await _номенклатура.ПолучитьКвант(номенклатураCodes, stoppingToken);
                            bool нетВНаличие = false;
                            foreach (var номенклатура in НоменклатураList)
                            {
                                decimal остаток = номенклатура.Остатки
                                            .Sum(z => z.СвободныйОстаток) / номенклатура.Единица.Коэффициент;
                                var quantum = (int)nomQuantums.Where(q => q.Key == номенклатура.Id).Select(q => q.Value).FirstOrDefault();
                                if (quantum == 0)
                                    quantum = 1;
                                var asked = posting.Products
                                                    .Where(b => (hexEncoding ? b.Offer_id.DecodeHexString() : b.Offer_id) == номенклатура.Code)
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
                                var cancelResult = await OzonClasses.OzonOperators.CancelOrder(_httpService, clientId, authToken,
                                    posting.Posting_number, 352, "Product is out of stock", null, stoppingToken);
                                if (!string.IsNullOrEmpty(cancelResult))
                                    _logger.LogError(cancelResult);
                                continue;
                            }
                        }
                        var orderItems = new List<OrderItem>();
                        if (posting.Status == Enum.GetName(OzonClasses.OrderStatus.awaiting_packaging))
                        {
                            orderItems = await OzonMakeOrdersPosting(clientId, authToken, hexEncoding, posting, stoppingToken);
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
                                    НоменклатураId = hexEncoding ? item.Offer_id.DecodeHexString() : item.Offer_id,
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
                                   hexEncoding,
                                   posting.Delivery_method != null ? posting.Delivery_method.Id.ToString() : "0",
                                   posting.Delivery_method != null ? posting.Delivery_method.Name : "",
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
        private async Task<List<OrderItem>> OzonMakeOrdersPosting(string clientId, string authToken, bool hexEncoding, OzonClasses.FbsPosting posting, CancellationToken stoppingToken)
        {
            var orderItems = new List<OrderItem>();
            var postingPackageData = new List<OzonClasses.PostingPackage>();
            foreach (var product in posting.Products)
            {
                var code = hexEncoding ? product.Offer_id.DecodeHexString() : product.Offer_id;
                string nomId = await _номенклатура.GetIdByCode(code);
                //decimal колМест = await _номенклатура.ПолучитьКоличествоМест(nomId);
                //колМест = колМест == 0 ? 1 : колМест;
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
                                        _httpService, clientId, authToken, страна, stoppingToken);
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
                            _httpService, clientId, authToken,
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
                for (int i = 0; i < product.Quantity; i++)
                {
                    postingPackageData.Add(new OzonClasses.PostingPackage
                    {
                        Products = new List<OzonClasses.PostingPackageProduct>
                                            {
                                                new OzonClasses.PostingPackageProduct
                                                {
                                                    Exemplar_info = new List<OzonClasses.PostingExemplarInfo>
                                                    {
                                                        new OzonClasses.PostingExemplarInfo
                                                        {
                                                            Gtd = string.IsNullOrWhiteSpace(gtd) ? null : gtd,
                                                            Is_gtd_absent = string.IsNullOrWhiteSpace(gtd),
                                                        }
                                                    },
                                                    Product_id = product.Sku,
                                                    Quantity = 1
                                                }
                                            }
                    });
                }
            }
            var result = await OzonClasses.OzonOperators.SetOrderPosting(_httpService, clientId, authToken,
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
                            НоменклатураId = hexEncoding ? item.Offer_id.DecodeHexString() : item.Offer_id,
                            Количество = item.Quantity,
                            Цена = Convert.ToDecimal(item.Price, new System.Globalization.NumberFormatInfo { NumberDecimalSeparator = "." }),
                        });
                    }
                }
            }
            return orderItems;
        }
        private async Task GetYandexCancelOrders(string campaignId, string clientId, string authToken, string marketplaceId, CancellationToken cancellationToken)
        {
            int pageNumber = 0;
            bool nextPage = true;
            while (nextPage)
            {
                pageNumber++;
                nextPage = false;
                var result = await YandexClasses.YandexOperators.OrderCancelList(_httpService, campaignId, clientId, authToken, pageNumber, cancellationToken);
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
        private async Task GetOzonCancelOrders(string clientId, string authToken, string id, string firmaId, string customerId, string dogovorId, bool hexEncoding, CancellationToken stoppingToken)
        {
            int limit = 1000;
            int numberCount = 0;
            bool nextPage = true;
            while (nextPage)
            {
                var result = await OzonClasses.OzonOperators.DetailOrders(_httpService, clientId, authToken,
                    OzonClasses.OrderStatus.cancelled,
                    30,
                    limit,
                    limit * numberCount,
                    stoppingToken);
                if (result.Item3 != null)
                    _logger.LogError(result.Item3);
                if (result.Item1 != null)
                {
                    foreach (var posting in result.Item1)
                    {
                        var order = await _order.ПолучитьOrderByMarketplaceId(id, posting.Posting_number);
                        if ((order != null) && (order.InternalStatus != 5) && (order.InternalStatus != 6))
                        {
                            await _docService.OrderCancelled(order);
                        }
                    }
                }
                numberCount++;
                nextPage = result.Item2.HasValue ? result.Item2.Value : false;
            }
        }
        private async Task GetOzonDeliveringOrders(string marketplaceId, string clientId, string authToken, string id, string firmaId, string customerId, string dogovorId, bool hexEncoding, CancellationToken stoppingToken)
        {
            var activeOrders = await ActiveOrders(marketplaceId, stoppingToken);
            if ((activeOrders != null) && (activeOrders.Count > 0))
            {
                foreach (var postingNumber in activeOrders)
                {
                    var result = await OzonClasses.OzonOperators.OrderDetails(_httpService, clientId, authToken,
                        postingNumber,
                        stoppingToken);
                    if (result.Item2 != null)
                        _logger.LogError(result.Item2);
                    if (result.Item1 != null)
                    {
                        if ((result.Item1 == OzonClasses.OrderStatus.driver_pickup) ||
                            (result.Item1 == OzonClasses.OrderStatus.delivering) ||
                            (result.Item1 == OzonClasses.OrderStatus.delivered))
                        {
                            var order = await _order.ПолучитьOrderByMarketplaceId(id, postingNumber);
                            if (order != null)
                                await _docService.OrderDeliveried(order);
                        }
                        else if ((result.Item1 == OzonClasses.OrderStatus.arbitration) ||
                            (result.Item1 == OzonClasses.OrderStatus.client_arbitration))
                        {
                            var order = await _order.ПолучитьOrderByMarketplaceId(id, postingNumber);
                            if (order != null)
                                await _order.ОбновитьOrderStatus(order.Id, (int)StinOrderStatus.ARBITRATION);
                        }
                    }    
                }
            }
        }
        private async Task<List<string>> ActiveOrders(string marketplaceId, CancellationToken cancellationToken)
        {
            DateTime dateRegTA = _context.GetRegTA();
            return await 
                        (from r in _context.Rg11973s //НаборНаСкладе
                         join doc in _context.Dh11948s on r.Sp11970 equals doc.Iddoc
                         join o in _context.Sc13994s on doc.Sp14003 equals o.Id
                         where (r.Period == dateRegTA) &&
                              (doc.Sp11938 == 1) &&
                              (o.Sp14038 == marketplaceId)
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
                                                //&& market.Code.Trim() == "22162396" //tmp
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
                                                HexEncoding = market.Sp14153 == 1
                                            })
                                            .ToListAsync();
                foreach (var marketplace in marketplaceIds)
                {
                    if (marketplace.Тип.ToUpper() == "ЯНДЕКС")
                    {
                        await UpdateTariffsYandex(marketplace.Id, marketplace.CampaignId, marketplace.ClientId, marketplace.AuthToken, marketplace.HexEncoding, marketplace.Модель, cancellationToken);
                    }
                    else if (marketplace.Тип.ToUpper() == "OZON")
                    {
                        await UpdateTariffsOzon(marketplace.Id, marketplace.CampaignId, marketplace.ClientId, marketplace.AuthToken, marketplace.HexEncoding, marketplace.Модель, cancellationToken);
                    }
                    else if (marketplace.Тип.ToUpper() == "ALIEXPRESS")
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
        private async Task UpdateTariffsYandex(string marketplaceId,
            string campaignId,
            string clientId,
            string authToken,
            bool hexEncoding,
            string model,
            CancellationToken cancellationToken)
        {
            string detailUrl = "https://api.partner.market.yandex.ru/v2/campaigns/{0}/stats/skus.json";
            int requestLimit = 500;

            double tariffPerOrder = model == "FBS" ? 45 : 0;

            double weightLimit = 25;
            double dimensionsLimit = 150;

            double shipmentFeeLightPercent = ((model == "FBY") || (model == "FBS")) ? 5 : 0;
            double shipmentFeeLightMin = model == "FBY" ? 13 : (model == "FBS" ? 60 : 0);
            double shipmentFeeLightMax = model == "FBY" ? 250 : (model == "FBS" ? 350 : 0);

            double shipmentFeeLightPercent_OutBorder = 1; //up to 5 for FBS
            double shipmentFeeLightMin_OutBorder = model == "FBY" ? 10 : (model == "FBS" ? 20 : 0);
            double shipmentFeeLightMax_OutBorder = model == "FBY" ? 100 : (model == "FBS" ? 500 : 0);

            double shipmentFeeHard = 400;
            double shipmentFeeHardPercent_OutBorder = 1; //up to 5 for FBS
            double shipmentFeeHardMin_OutBorder = model == "FBY" ? 50 : (model == "FBS" ? 25 : 0);
            double shipmentFeeHardMax_OutBorder = model == "FBY" ? 500 : (model == "FBS" ? 1500 : 0);

            var query = from markUse in _context.Sc14152s
                        join nom in _context.Sc84s on markUse.Parentext equals nom.Id
                        where !markUse.Ismark && (markUse.Sp14147 == marketplaceId) &&
                          (markUse.Sp14158 == 1) //Есть в каталоге 
                          //&& nom.Code == "D00045010"
                        select new
                        {
                            Id = markUse.Id,
                            Sku = nom.Code  
                        };
            for (int i = 0; i < query.Count(); i = i + requestLimit)
            {
                var data = await query
                    .OrderBy(x => x.Sku)
                    .Skip(i)
                    .Take(requestLimit)
                    .ToListAsync(cancellationToken);
                var request = new YandexClasses.SkuDetailsRequest { ShopSkus = data.Select(x => hexEncoding ? x.Sku.EncodeHexString() : x.Sku).ToList() };
                var result = await YandexClasses.YandexOperators.Exchange<YandexClasses.SkuDetailsResponse>(_httpService,
                    string.Format(detailUrl, campaignId),
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
                        string markUseId = data.Where(x => (hexEncoding ? x.Sku.EncodeHexString() : x.Sku) == item.ShopSku).Select(x => x.Id).FirstOrDefault();
                        Sc14152 entity = null;
                        if (!string.IsNullOrEmpty(markUseId))
                            entity = await _context.Sc14152s.FirstOrDefaultAsync(x => x.Id == markUseId, cancellationToken);
                        if (entity != null)
                        {
                            double dimensions = (item.WeightDimensions?.Width ?? 0)
                                + (item.WeightDimensions?.Length ?? 0)
                                + (item.WeightDimensions?.Height ?? 0);
                            double weight = item.WeightDimensions?.Weight ?? 0;

                            var tariff = item.Tariffs?.Sum(x => x.Amount) ?? 0;
                            tariff += tariffPerOrder;
                            if ((weight > weightLimit) || (dimensions > dimensionsLimit))
                            {
                                tariff += shipmentFeeHard; //Доставка покупателю
                                tariff += (item.Price * shipmentFeeHardPercent_OutBorder / 100)
                                    .WithLimits(shipmentFeeHardMin_OutBorder, shipmentFeeHardMax_OutBorder); //доставка в другой округ
                            }
                            else
                            {
                                tariff += (item.Price * shipmentFeeLightPercent / 100)
                                    .WithLimits(shipmentFeeLightMin, shipmentFeeLightMax); //Доставка покупателю
                                tariff += (item.Price * shipmentFeeLightPercent_OutBorder / 100)
                                    .WithLimits(shipmentFeeLightMin_OutBorder, shipmentFeeLightMax_OutBorder); //доставка в другой округ
                            }
                            if ((decimal)tariff != entity.Sp14198)
                            {
                                entity.Sp14198 = (decimal)tariff;
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
        }
        private async Task UpdateTariffsOzon(string marketplaceId,
            string campaignId,
            string clientId,
            string authToken,
            bool hexEncoding,
            string model,
            CancellationToken cancellationToken)
        {

        }
    }
}
