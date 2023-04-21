using Market.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YandexClasses;
using StinClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StinClasses.Справочники.Functions;
using StinClasses.Справочники;
using Microsoft.Extensions.Logging;

namespace Market.Controllers
{
    [ApiController]
    [Route("yandexDBS")]
    public class YandexDbsController : ControllerBase
    {
        private IServiceScopeFactory _serviceScopeFactory;
        private IConfiguration _configuration;
        IMarketplaceFunctions _marketplaceFunctions;
        IFirmaFunctions _firmaFunctions;
        IStockFunctions _stockFunctions;
        ILogger<YandexDbsController> _logger;
        private string _defFirma;
        private string _defFirmaId;
        public YandexDbsController(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory, 
            IMarketplaceFunctions marketplaceFunctions, 
            IFirmaFunctions firmaFunctions,
            IStockFunctions stockFunctions,
            ILogger<YandexDbsController> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;
            _marketplaceFunctions = marketplaceFunctions;
            _firmaFunctions = firmaFunctions;
            _stockFunctions = stockFunctions;
            _logger = logger;
            _defFirma = _configuration["Settings:Firma"];
            _defFirmaId = _configuration["Settings:" + _defFirma + ":FirmaId"];
        }
        [HttpPost("cart")]
        public async Task<ActionResult<ResponseCartDBS>> Cart([FromHeader] HeadersParameters headers, [FromBody] RequestedCart requestedCart, CancellationToken cancellationToken)
        {
            var responseCart = new ResponseCartDBS();
            responseCart.Cart = new CartResponseDBSEntry();

            var marketplace = await _marketplaceFunctions.GetMarketplaceByFirmaAsync(_defFirmaId, headers.Authorization, cancellationToken);
            var nomCodes = requestedCart.Cart.Items.Select(x => x.OfferId.Decode(marketplace.Encoding))
                .Where(x => !string.IsNullOrEmpty(x)).ToList();
            var marketUseData = await _marketplaceFunctions.GetMarketUseInfoForStockAsync(nomCodes, marketplace, false, 0, cancellationToken);

            var requestedRegion = requestedCart.Cart.Delivery.Region.FindRegionByType(RegionType.CITY);
            if (requestedRegion == null)
                requestedRegion = requestedCart.Cart.Delivery.Region.FindRegionByType(RegionType.SUBJECT_FEDERATION_DISTRICT);
            if (requestedRegion == null)
                requestedRegion = requestedCart.Cart.Delivery.Region.FindRegionByType(RegionType.SUBJECT_FEDERATION);
            if (requestedRegion == null)
                requestedRegion = requestedCart.Cart.Delivery.Region.FindRegionByType(RegionType.COUNTRY_DISTRICT);
            if (requestedRegion == null)
                requestedRegion = requestedCart.Cart.Delivery.Region.FindRegionByType(RegionType.COUNTRY);

            var stockData = await _marketplaceFunctions.GetStockData(marketUseData, marketplace, requestedRegion?.Name, cancellationToken);

            var фирма = await _firmaFunctions.GetEntityByIdAsync(_defFirmaId, cancellationToken);

            var currentTime = DateTime.Now.TimeOfDay; 
            decimal суммаЗаказа = 0;
            Dictionary<string, int> pickupDays = new();
            bool deliveryAvailable = true;
            foreach (var requestedItem in requestedCart.Cart.Items)
            {
                var stockInfo = stockData.FirstOrDefault(x => x.offerId == requestedItem.OfferId);
                int result = Math.Min(requestedItem.Count, stockInfo.stock); 
                responseCart.Cart.Items.Add(new ResponseItemDBS
                {
                    FeedId = requestedItem.FeedId,
                    OfferId = requestedItem.OfferId,
                    Count = result,
                    Delivery = result > 0,
                    SellerInn = фирма.ЮрЛицо.ИНН
                });
                if (result > 0)
                {
                    if (deliveryAvailable)
                        deliveryAvailable = !stockInfo.pickupOnly;
                    суммаЗаказа += stockInfo.price * result;
                    foreach (var info in stockInfo.pickupsInfo)
                    {
                        if (info.Value >= result)
                        {
                            var daysCount = await _stockFunctions.NextBusinessDay(info.Key.СкладId, DateTime.Today, (currentTime > info.Key.МаксВремяЗаказа ? 1 : 0), cancellationToken);
                            if (pickupDays.ContainsKey(info.Key.PickupId))
                                pickupDays[info.Key.PickupId] = Math.Max(pickupDays[info.Key.PickupId], daysCount);
                            else
                                pickupDays.Add(info.Key.PickupId, daysCount);
                        }
                    }
                }
            }
            responseCart.Cart.DeliveryCurrency = CurrencyType.RUR;
            if (суммаЗаказа > 0)
            {
                //доставка
                if (deliveryAvailable && (суммаЗаказа > 500))
                {
                    var mainDeliveryFromDate = await _stockFunctions.NextBusinessDay(Common.SkladEkran, DateTime.Today, 1, cancellationToken);
                    var mainDeliveryToDate = await _stockFunctions.NextBusinessDay(Common.SkladEkran, DateTime.Today, mainDeliveryFromDate + 1, cancellationToken);
                    if (!TimeSpan.TryParse("09:00", out TimeSpan fromTime))
                        fromTime = TimeSpan.Zero;
                    if (!TimeSpan.TryParse("18:00", out TimeSpan toTime))
                        toTime = TimeSpan.MaxValue;
                    responseCart.Cart.DeliveryOptions.Add(new DeliveryOption
                    {
                        Id = "99",
                        Price = 1,
                        ServiceName = "Собственная служба",
                        Type = DeliveryType.DELIVERY,
                        Dates = new Date
                        {
                            FromDate = DateTime.Today.AddDays(mainDeliveryFromDate),
                            ToDate = DateTime.Today.AddDays(mainDeliveryToDate),
                            Intervals = new List<Interval>
                            {
                                new Interval { Date = DateTime.Today.AddDays(mainDeliveryFromDate), FromTime = fromTime, ToTime = toTime },
                                new Interval { Date = DateTime.Today.AddDays(mainDeliveryToDate), FromTime = fromTime, ToTime = toTime },
                            }
                        }
                    });
                }
                //самовывоз:
                var rezervTime = 3; //время хранения резерва 3 дня?
                foreach (var pickupGroup in pickupDays.GroupBy(x => x.Value))
                {
                    var calcSklad = Common.SkladEkran;
                    if (pickupGroup.Count() == 1)
                        calcSklad = pickupGroup.Select(x => x.Key).First();
                    var deliveryOption = new DeliveryOption
                    {
                        Price = 0,
                        ServiceName = "PickPoint",
                        Type = DeliveryType.PICKUP,
                        Dates = new Date
                        {
                            FromDate = DateTime.Today.AddDays(pickupGroup.Key),
                            ToDate = DateTime.Today.AddDays(await _stockFunctions.NextBusinessDay(calcSklad, DateTime.Today, pickupGroup.Key + rezervTime, cancellationToken))
                        },
                    };
                    foreach (var pickup in pickupGroup)
                        deliveryOption.Outlets.Add(new Outlet { Code = pickup.Key });
                    responseCart.Cart.DeliveryOptions.Add(deliveryOption);
                }
                responseCart.Cart.PaymentMethods = new List<PaymentMethod>
                {
                    PaymentMethod.YANDEX,
                    PaymentMethod.APPLE_PAY,
                    PaymentMethod.GOOGLE_PAY,
                    PaymentMethod.CARD_ON_DELIVERY,
                    PaymentMethod.CASH_ON_DELIVERY
                };
            }

            //using IBridge1C bridge = _serviceScopeFactory.CreateScope()
            //.ServiceProvider.GetService<IBridge1C>();

            //var market = await bridge.ПолучитьМаркет(headers.Authorization, _defFirmaId);
            //var requestedRegion = requestedCart.Cart.Delivery.Region.FindRegionByType(RegionType.CITY);
            //if (requestedRegion == null)
            //    requestedRegion = requestedCart.Cart.Delivery.Region.FindRegionByType(RegionType.SUBJECT_FEDERATION_DISTRICT);
            //if (requestedRegion == null)
            //    requestedRegion = requestedCart.Cart.Delivery.Region.FindRegionByType(RegionType.SUBJECT_FEDERATION);
            //if (requestedRegion == null)
            //    requestedRegion = requestedCart.Cart.Delivery.Region.FindRegionByType(RegionType.COUNTRY_DISTRICT);
            //if (requestedRegion == null)
            //    requestedRegion = requestedCart.Cart.Delivery.Region.FindRegionByType(RegionType.COUNTRY);
            ////requestedRegion.Name == CITY (MarketplacePickups.Регион)
            //var фирма = await bridge.ПолучитьФирму(_defFirmaId);
            //var currentTime = DateTime.Now.TimeOfDay;
            ////получить DataList = new List(pickupId == MarketplacePickups.Code, складId, regionName, МаксВремяЗаказа, КолвоДнейВыполнения = 0) 
            ////исходя из фирмы и из Marketplace.AuthorizationYandexApi == configuration["Settings:YandexDBS"]
            ////и из requestedCart.regionName
            //var точкиСамовывоза = await bridge.ПолучитьТочкиСамовывоза(фирма.Id, headers.Authorization, requestedRegion.Name);

            //List<string> списокСкладовНаличияТовара = null;
            //if (!string.IsNullOrEmpty(market.СкладId))
            //    списокСкладовНаличияТовара = new List<string> { market.СкладId };
            //else
            //    списокСкладовНаличияТовара = await bridge.ПолучитьСкладIdОстатковMarketplace();
            ////получить Свободные остатки исходя из полученных складId
            //var номенклатураCodes = requestedCart.Cart.Items.Select(x => x.OfferId.Decode(market.Encoding)).Where(x => !string.IsNullOrEmpty(x)).ToList();
            ////market.HexEncoding ? x.OfferId.TryDecodeHexString() : x.OfferId).Where(x => !string.IsNullOrEmpty(x)).ToList();
            //var НоменклатураList = await bridge.ПолучитьСвободныеОстатки(номенклатураCodes, списокСкладовНаличияТовара);
            //var lockedNomIds = await bridge.ПолучитьLockedНоменклатураIds(headers.Authorization, номенклатураCodes);
            //var nomDeltaStock = await bridge.ПолучитьDeltaStock(market.Id, номенклатураCodes, cancellationToken);

            //bool естьТовар = false;
            //decimal суммаЗаказа = 0;
            //foreach (var requestedItem in requestedCart.Cart.Items)
            //{
            //    var номенклатура = НоменклатураList.Where(x => x.Code == requestedItem.OfferId.Decode(market.Encoding)).FirstOrDefault();
            //    //(market.HexEncoding ? requestedItem.OfferId.TryDecodeHexString() : requestedItem.OfferId)).FirstOrDefault();
            //    if (номенклатура != null)
            //    {
            //        int ОтпуститьВсего = 0;
            //        var deltaStock = (int)nomDeltaStock.Where(x => x.Key == номенклатура.Id).Select(x => x.Value).FirstOrDefault();
            //        if (!lockedNomIds.Any(x => x == номенклатура.Id))
            //            ОтпуститьВсего = Math.Min(requestedItem.Count, Math.Max((int)((номенклатура.Остатки.Sum(x => x.СвободныйОстаток) / номенклатура.Единица.Коэффициент) - deltaStock),0));
            //        responseCart.Cart.Items.Add(new ResponseItemDBS
            //        {
            //            FeedId = requestedItem.FeedId,
            //            OfferId = requestedItem.OfferId,
            //            Count = ОтпуститьВсего,
            //            Delivery = ОтпуститьВсего > 0,
            //            SellerInn = фирма.ЮрЛицо.ИНН
            //        });
            //        if (ОтпуститьВсего > 0)
            //        {
            //            естьТовар = true;
            //            суммаЗаказа += (номенклатура.Цена != null ? (номенклатура.Цена.РозСП > 0 ? номенклатура.Цена.РозСП : номенклатура.Цена.Розничная) : 0m) * ОтпуститьВсего;
            //            foreach (var pickup in точкиСамовывоза.Where(x => x.КолВоДнейВыполнения == 0))
            //            {
            //                var МожноОтпустить = Math.Min(ОтпуститьВсего, (int)(номенклатура.Остатки.Where(x => x.СкладId == pickup.СкладId).Sum(x => x.СвободныйОстаток) / номенклатура.Единица.Коэффициент));
            //                if (МожноОтпустить < ОтпуститьВсего)
            //                {
            //                    //нужно перемещать с другого склада
            //                    pickup.КолВоДнейВыполнения = -1;
            //                }
            //                else
            //                    pickup.КолВоДнейВыполнения = await bridge.РассчитатьКолвоДнейВыполнения(pickup.СкладId, (currentTime > pickup.МаксВремяЗаказа ? 1 : 0));
            //            }
            //        }
            //    }
            //    else
            //        responseCart.Cart.Items.Add(new ResponseItemDBS
            //        {
            //            FeedId = requestedItem.FeedId,
            //            OfferId = requestedItem.OfferId,
            //            Count = 0,
            //            Delivery = false,
            //            SellerInn = фирма.ЮрЛицо.ИНН
            //        });
            //}
            ////заполнить responseCart.Cart.DeliveryOptions
            //responseCart.Cart.DeliveryCurrency = CurrencyType.RUR;
            //if (естьТовар)
            //{
            //    //доставка
            //    bool deliveryAvailable = !НоменклатураList.Where(x =>
            //        responseCart.Cart.Items
            //            .Where(y => y.Delivery)
            //            .Any(z => z.OfferId.Decode(market.Encoding) == x.Code))
            //            //(market.HexEncoding ? z.OfferId.DecodeHexString() : z.OfferId) == x.Code))
            //        .Any(x => x.PickupOnly);
            //    if (deliveryAvailable && (суммаЗаказа > 500))
            //    {
            //        string складОтгрузкиId = "";
            //        foreach (var складId in списокСкладовНаличияТовара)
            //        {
            //            bool нетНаСкладе = НоменклатураList
            //                .Where(x => responseCart.Cart.Items
            //                        .Where(y => y.Delivery)
            //                        .Any(z => z.OfferId.Decode(market.Encoding) == x.Code))
            //                        //(market.HexEncoding ? z.OfferId.DecodeHexString() : z.OfferId) == x.Code))
            //                .Any(x => x.Остатки
            //                        .Where(y => y.СкладId == складId)
            //                        .Sum(z => z.СвободныйОстаток) / x.Единица.Коэффициент <
            //                            responseCart.Cart.Items
            //                                .Where(b => b.OfferId.Decode(market.Encoding) == x.Code)
            //                                //(market.HexEncoding ? b.OfferId.DecodeHexString() : b.OfferId) == x.Code)
            //                                .Select(c => c.Count)
            //                                .FirstOrDefault());
            //            if (!нетНаСкладе)
            //            {
            //                складОтгрузкиId = складId;
            //                break;
            //            }
            //        }

            //        var mainDeliveryFromDate = await bridge.РассчитатьКолвоДнейВыполнения(Common.SkladEkran, 0);
            //        if (mainDeliveryFromDate == 0)
            //        {
            //            //заказ пришел в рабочий день
            //            mainDeliveryFromDate = await bridge.РассчитатьКолвоДнейВыполнения(Common.SkladEkran, mainDeliveryFromDate + 1);
            //            if (!TimeSpan.TryParse("17:00", out TimeSpan limitDeliveryTime))
            //                limitDeliveryTime = TimeSpan.Zero;
            //            if (currentTime > limitDeliveryTime)
            //                mainDeliveryFromDate = await bridge.РассчитатьКолвоДнейВыполнения(Common.SkladEkran, mainDeliveryFromDate + 1);
            //        }
            //        else
            //            mainDeliveryFromDate = await bridge.РассчитатьКолвоДнейВыполнения(Common.SkladEkran, mainDeliveryFromDate + 1);

            //        var mainDeliveryToDate = await bridge.РассчитатьКолвоДнейВыполнения(Common.SkladEkran, mainDeliveryFromDate + 1);
            //        if (!TimeSpan.TryParse("09:00", out TimeSpan fromTime))
            //        {
            //            fromTime = TimeSpan.Zero;
            //        }
            //        if (!TimeSpan.TryParse("18:00", out TimeSpan toTime))
            //        {
            //            toTime = TimeSpan.MaxValue;
            //        }
            //        responseCart.Cart.DeliveryOptions.Add(new DeliveryOption
            //        {
            //            Id = "99",
            //            Price = 1,
            //            ServiceName = "Собственная служба",
            //            Type = DeliveryType.DELIVERY,
            //            Dates = new Date
            //            {
            //                FromDate = DateTime.Today.AddDays(mainDeliveryFromDate),
            //                ToDate = DateTime.Today.AddDays(mainDeliveryToDate),
            //                Intervals = new List<Interval>
            //            {
            //                new Interval { Date = DateTime.Today.AddDays(mainDeliveryFromDate), FromTime = fromTime, ToTime = toTime },
            //                new Interval { Date = DateTime.Today.AddDays(mainDeliveryToDate), FromTime = fromTime, ToTime = toTime },
            //            }
            //            }
            //        });
            //    }
            //    //самовывоз:
            //    //DataList сгруппировать по КолвоДнейВыполнения (если Время больше DataList.МаксВремяЗаказа, то добавить еще один день. Как учесть выходные?
            //    //заполнить самовывоз исходя из полученных данных
            //    var rezervTime = 3; //время хранения резерва 3 дня?
            //    foreach (var pickupGroup in точкиСамовывоза.Where(x => x.КолВоДнейВыполнения >= 0).GroupBy(x => x.КолВоДнейВыполнения))
            //    {
            //        var deliveryOption = new DeliveryOption
            //        {
            //            Price = 0,
            //            ServiceName = "PickPoint",
            //            Type = DeliveryType.PICKUP,
            //            Dates = new Date
            //            {
            //                FromDate = DateTime.Today.AddDays(pickupGroup.Key),
            //                ToDate = DateTime.Today.AddDays(pickupGroup.Key + rezervTime)
            //            },
            //        };
            //        foreach (var pickup in pickupGroup)
            //            deliveryOption.Outlets.Add(new Outlet { Code = pickup.PickupId });

            //        responseCart.Cart.DeliveryOptions.Add(deliveryOption);

            //    }
            //    responseCart.Cart.PaymentMethods = new List<PaymentMethod>
            //    {
            //        PaymentMethod.YANDEX,
            //        PaymentMethod.APPLE_PAY,
            //        PaymentMethod.GOOGLE_PAY,
            //        PaymentMethod.CARD_ON_DELIVERY,
            //        PaymentMethod.CASH_ON_DELIVERY
            //    };
            //}
            return Ok(responseCart);
        }
        [HttpPost("stocks")]
        public async Task<ActionResult<ResponseStocks>> Stocks([FromHeader] HeadersParameters headers, [FromBody] RequestStocks requestedStock, CancellationToken cancellationToken)
        {
            var responseStock = new ResponseStocks();
            var marketplace = await _marketplaceFunctions.GetMarketplaceByFirmaAsync(_defFirmaId, headers.Authorization, cancellationToken);
            var nomCodes = requestedStock.Skus.Select(x => x.Decode(marketplace.Encoding))
                .Where(x => !string.IsNullOrEmpty(x)).ToList();
            var marketUseData = await _marketplaceFunctions.GetMarketUseInfoForStockAsync(nomCodes, marketplace, false, 0, cancellationToken);
            var stockData = await _marketplaceFunctions.GetStockData(marketUseData, marketplace, cancellationToken);
            foreach (var requestedSku in requestedStock.Skus)
            {
                var result = stockData.FirstOrDefault(x => x.offerId == requestedSku).stock;
                result = Math.Max(result, 0);
                responseStock.Skus.Add(new SkuEntry
                {
                    Sku = requestedSku,
                    WarehouseId = requestedStock.WarehouseId,
                    Items = new List<SkuItem>
                    {
                        new SkuItem
                        {
                            Type = ItemType.FIT,
                            Count = result.ToString(),
                            UpdatedAt = DateTime.Now
                        }
                    }
                });
            }

            //using IBridge1C bridge = _serviceScopeFactory.CreateScope()
            //    .ServiceProvider.GetService<IBridge1C>();
           
            //var market = await bridge.ПолучитьМаркет(headers.Authorization, _defFirmaId);
            //List<string> списокСкладов = null;
            //if (!string.IsNullOrEmpty(market.СкладId))
            //    списокСкладов = new List<string> { market.СкладId };
            //else
            //    списокСкладов = await bridge.ПолучитьСкладIdОстатковMarketplace();

            //var номенклатураCodes = requestedStock.Skus.Select(x => x.Decode(market.Encoding)).Where(x => !string.IsNullOrEmpty(x)).ToList();
            //var НоменклатураList = await bridge.ПолучитьСвободныеОстатки(номенклатураCodes, списокСкладов);
            //var резервыМаркета = await bridge.ПолучитьРезервМаркета(market.Id, НоменклатураList.Select(x => x.Id));

            //var lockedNomIds = await bridge.ПолучитьLockedНоменклатураIds(headers.Authorization, номенклатураCodes);
            //var nomDeltaStock = await bridge.ПолучитьDeltaStock(market.Id, номенклатураCodes, cancellationToken);

            //foreach (var requestedSku in requestedStock.Skus)
            //{
            //    int count = 0;
            //    var номенклатура = НоменклатураList.Where(x => x.Code == requestedSku.Decode(market.Encoding)).FirstOrDefault();
            //    if ((номенклатура != null) && !lockedNomIds.Any(x => x == номенклатура.Id))
            //    {
            //        резервыМаркета.TryGetValue(номенклатура.Id, out decimal резервМаркета);
            //        var остатокРегистр = номенклатура.Остатки.Sum(x => x.СвободныйОстаток);
            //        остатокРегистр += резервМаркета;
            //        var deltaStock = (int)nomDeltaStock.Where(x => x.Key == номенклатура.Id).Select(x => x.Value).FirstOrDefault();
            //        count = Math.Max((int)(остатокРегистр / номенклатура.Единица.Коэффициент) - deltaStock, 0);
            //    }
            //    responseStock.Skus.Add(new SkuEntry
            //    {
            //        Sku = requestedSku,
            //        WarehouseId = requestedStock.WarehouseId,
            //        Items = new List<SkuItem>
            //        {
            //            new SkuItem
            //            {
            //                Type = ItemType.FIT,
            //                Count = count.ToString(),
            //                UpdatedAt = DateTime.Now
            //            }
            //        }
            //    });
            //}
            return Ok(responseStock);
        }
        [HttpPost("order/accept")]
        public async Task<ActionResult<ResponseOrder>> OrderAccept([FromHeader] HeadersParameters headers, [FromBody] RequestedOrder requestedOrder, CancellationToken cancellationToken)
        {
            var responseOrder = new ResponseOrder();
            responseOrder.Order = new OrderResponseEntry();

            int tryCount = 5;
            TimeSpan sleepPeriod = TimeSpan.FromSeconds(1);
            while (true)
            {
                using IBridge1C bridge = _serviceScopeFactory.CreateScope()
                    .ServiceProvider.GetService<IBridge1C>();
                var orderResult = await bridge.NewOrder(false, headers.Authorization, requestedOrder.Order);
                responseOrder.Order.Accepted = !string.IsNullOrEmpty(orderResult.Item1);
                if (responseOrder.Order.Accepted)
                {
                    responseOrder.Order.Id = orderResult.Item1;
                    responseOrder.Order.ShipmentDate = orderResult.Item2;
                    break;
                }
                else
                {
                    if (--tryCount == 0)
                        break;
                    await Task.Delay(sleepPeriod);
                }
            }
            if (!responseOrder.Order.Accepted)
            {
                string dirPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_offers");
                string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_offers", RouteData.Values["controller"] + ".txt");
                if (!System.IO.Directory.Exists(dirPath))
                    System.IO.Directory.CreateDirectory(dirPath);
                if (!System.IO.File.Exists(fullPath))
                {
                    using (System.IO.StreamWriter sw = System.IO.File.CreateText(fullPath))
                    {
                        sw.WriteLine(DateTime.Now.ToString("dd:MM:yyyy HH:mm:ss") + " " + requestedOrder.Order.Id.ToString());
                    }
                    return StatusCode((int)System.Net.Sockets.SocketError.ConnectionRefused);
                }
                else
                {
                    var errorData = System.IO.File.ReadAllLines(fullPath);
                    var last10records = errorData.Reverse().Take(10).Where(x => x.EndsWith(requestedOrder.Order.Id.ToString(), StringComparison.InvariantCulture));
                    if (last10records.Count() >= 5)
                        responseOrder.Order.Reason = FaultReason.OUT_OF_DATE;
                    else
                    {
                        using (System.IO.StreamWriter sw = System.IO.File.AppendText(fullPath))
                        {
                            sw.WriteLine(DateTime.Now.ToString("dd:MM:yyyy HH:mm:ss") + " " + requestedOrder.Order.Id.ToString());
                        }
                        return StatusCode((int)System.Net.Sockets.SocketError.ConnectionRefused);
                    }
                }
            }
            return Ok(responseOrder);
        }
        [HttpPost("order/status")]
        public async Task<ActionResult> OrderChangeStatus([FromHeader] HeadersParameters headers, [FromBody] RequestedOrder requestedOrder, CancellationToken cancellationToken)
        {
            using IBridge1C bridge = _serviceScopeFactory.CreateScope()
                .ServiceProvider.GetService<IBridge1C>();
            if ((requestedOrder.Order.Buyer != null) || ((requestedOrder.Order.Delivery != null) && (requestedOrder.Order.Delivery.Address != null)))
            {
                string BuyerLastName = "";
                string BuyerFirstName = "";
                string BuyerMiddleName = "";
                string Recipient = "";
                string Phone = "";
                string Postcode = ""; string Country = ""; string City = ""; string Subway = ""; string Street = "";
                string House = ""; string Block = ""; string Entrance = ""; string Entryphone = ""; string Floor = ""; string Apartment = "";
                if (requestedOrder.Order.Buyer != null)
                {
                    BuyerLastName = requestedOrder.Order.Buyer.LastName;
                    BuyerFirstName = requestedOrder.Order.Buyer.FirstName;
                    BuyerMiddleName = requestedOrder.Order.Buyer.MiddleName;
                    if (!string.IsNullOrEmpty(requestedOrder.Order.Buyer.Phone))
                        Phone = requestedOrder.Order.Buyer.Phone;
                }
                if ((requestedOrder.Order.Delivery != null) && (requestedOrder.Order.Delivery.Address != null))
                {
                    Recipient = requestedOrder.Order.Delivery.Address.Recipient;
                    if (!string.IsNullOrEmpty(requestedOrder.Order.Delivery.Address.Phone))
                        Phone = requestedOrder.Order.Delivery.Address.Phone;
                    Postcode = requestedOrder.Order.Delivery.Address.Postcode;
                    Country = requestedOrder.Order.Delivery.Address.Country;
                    City = requestedOrder.Order.Delivery.Address.City;
                    Subway = requestedOrder.Order.Delivery.Address.Subway;
                    Street = requestedOrder.Order.Delivery.Address.Street;
                    House = requestedOrder.Order.Delivery.Address.House;
                    Block = requestedOrder.Order.Delivery.Address.Block;
                    Entrance = requestedOrder.Order.Delivery.Address.Entrance;
                    Entryphone = requestedOrder.Order.Delivery.Address.Entryphone;
                    Floor = requestedOrder.Order.Delivery.Address.Floor;
                    Apartment = requestedOrder.Order.Delivery.Address.Apartment;
                }
                await bridge.SetRecipientAndAddress(headers.Authorization, requestedOrder.Order.Id, 
                    BuyerLastName, BuyerFirstName, BuyerMiddleName, Recipient, Phone,
                    Postcode, Country, City, Subway, Street, House, Block, Entrance, Entryphone, Floor, Apartment,
                    requestedOrder.Order.Notes);
            }
            await bridge.ChangeStatus(null, headers.Authorization, requestedOrder.Order.Id, requestedOrder.Order.Status, requestedOrder.Order.SubStatus);
            return Ok();
        }
        [HttpPost("order/cancellation/notify")]
        public async Task<ActionResult> OrderCancelNotify([FromHeader] HeadersParameters headers, [FromBody] RequestedOrder requestedOrder, CancellationToken cancellationToken)
        {
            using IBridge1C bridge = _serviceScopeFactory.CreateScope()
                .ServiceProvider.GetService<IBridge1C>();
            await bridge.SetCancelNotify(headers.Authorization, requestedOrder.Order.Id);
            return Ok();
        }
        [HttpPost("order/int_items")]
        public async Task<ActionResult<string>> OrderIntReduceItems([FromBody] RequestedDocId requestedDocId, CancellationToken cancellationToken)
        {
            using IBridge1C bridge = _serviceScopeFactory.CreateScope()
                .ServiceProvider.GetService<IBridge1C>();
            var result = await bridge.ReduceCancelItems(requestedDocId.DocId, cancellationToken);
            return Ok(result);
        }
    }
}
