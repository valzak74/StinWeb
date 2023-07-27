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
                        calcSklad = pickupGroup.Select(x => x.Key).First().FormatTo1CId();
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
