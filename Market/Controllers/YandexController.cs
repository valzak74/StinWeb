using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Market.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using StinClasses;
using YandexClasses;
using StinClasses.Справочники;
using OzonClasses;

namespace Market.Controllers
{
    [ApiController]
    //[Route("[controller]")]
    [Route("yandex")]
    public class YandexController : ControllerBase
    {
        private IServiceScopeFactory _serviceScopeFactory;
        private IConfiguration _configuration;
        private string _defFirma;
        private string _defFirmaId;
        public YandexController(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;
            _defFirma = _configuration["Settings:Firma"];
            _defFirmaId = _configuration["Settings:" + _defFirma + ":FirmaId"];
        }
        [HttpPost("cart")]
        public async Task<ActionResult<ResponseCartFBS>> Cart([FromHeader] HeadersParameters headers, [FromBody] RequestedCart requestedCart, CancellationToken cancellationToken)
        {
            var responseCart = new ResponseCartFBS();
            responseCart.Cart = new CartResponseFBSEntry();

            using IBridge1C bridge = _serviceScopeFactory.CreateScope()
                .ServiceProvider.GetService<IBridge1C>();

            var market = await bridge.ПолучитьМаркет(headers.Authorization, _defFirmaId);

            List<string> списокСкладов = null;
            if (!string.IsNullOrEmpty(market.СкладId))
                списокСкладов = new List<string> { market.СкладId };
            else
                списокСкладов = await bridge.ПолучитьСкладIdОстатковMarketplace();

            var номенклатураCodes = requestedCart.Cart.Items.Select(x => x.OfferId.Decode(market.Encoding))
                .Where(x => !string.IsNullOrEmpty(x)).ToList();
            var НоменклатураList = await bridge.ПолучитьСвободныеОстатки(номенклатураCodes, списокСкладов);
            
            var lockedNomIds = await bridge.ПолучитьLockedНоменклатураIds(headers.Authorization, номенклатураCodes);
            var nomQuantums = await bridge.ПолучитьКвант(номенклатураCodes, cancellationToken);
            var nomDeltaStock = await bridge.ПолучитьDeltaStock(market.Id, номенклатураCodes, cancellationToken);

            foreach (var requestedItem in requestedCart.Cart.Items)
            {
                var номенклатура = НоменклатураList.Where(x => x.Code == requestedItem.OfferId.Decode(market.Encoding)).FirstOrDefault();
                if (номенклатура != null)
                {
                    int МожноОтпустить = 0;
                    if (!lockedNomIds.Any(x => x == номенклатура.Id))
                    {
                        var quantum = (int)nomQuantums.Where(x => x.Key == номенклатура.Id).Select(x => x.Value).FirstOrDefault();
                        var deltaStock = (int)nomDeltaStock.Where(x => x.Key == номенклатура.Id).Select(x => x.Value).FirstOrDefault();
                        if (quantum > 1)
                        {
                            var запрошеноКвантов = (int)(requestedItem.Count / quantum);
                            if (запрошеноКвантов > 0)
                            {
                                var остатокКвантов = (int)(((номенклатура.Остатки
                                    .Where(x => x.СкладId == Common.SkladEkran)
                                    .Sum(x => x.СвободныйОстаток) / номенклатура.Единица.Коэффициент) - deltaStock) / quantum);
                                остатокКвантов = Math.Max(остатокКвантов, 0);
                                МожноОтпустить = Math.Min(запрошеноКвантов * quantum, остатокКвантов * quantum);
                            }
                        }
                        else
                        {
                            var остаток = (int)((номенклатура.Остатки.Sum(x => x.СвободныйОстаток) / номенклатура.Единица.Коэффициент) - deltaStock);
                            остаток = Math.Max(остаток, 0);
                            МожноОтпустить = Math.Min(requestedItem.Count, остаток);
                        }
                    }
                    responseCart.Cart.Items.Add(new ResponseItemFBS
                    {
                        FeedId = requestedItem.FeedId,
                        OfferId = requestedItem.OfferId,
                        Count = МожноОтпустить,
                        Delivery = МожноОтпустить > 0
                    });
                }
                else
                    responseCart.Cart.Items.Add(new ResponseItemFBS
                    {
                        FeedId = requestedItem.FeedId,
                        OfferId = requestedItem.OfferId,
                        Count = 0,
                        Delivery = false
                    });
            }
            return Ok(responseCart);
        }
        [HttpPost("stocks")]
        public async Task<ActionResult<ResponseStocks>> Stocks([FromHeader] HeadersParameters headers, [FromBody] RequestStocks requestedStock, CancellationToken cancellationToken)
        {
            var responseStock = new ResponseStocks();

            using IBridge1C bridge = _serviceScopeFactory.CreateScope()
                .ServiceProvider.GetService<IBridge1C>();

            var market = await bridge.ПолучитьМаркет(headers.Authorization, _defFirmaId);
            List<string> списокСкладов = null;
            if (!string.IsNullOrEmpty(market.СкладId))
                списокСкладов = new List<string> { market.СкладId };
            else
                списокСкладов = await bridge.ПолучитьСкладIdОстатковMarketplace();

            var номенклатураCodes = requestedStock.Skus.Select(x => x.Decode(market.Encoding))
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();
            var НоменклатураList = await bridge.ПолучитьСвободныеОстатки(номенклатураCodes, списокСкладов);
            var резервыМаркета = await bridge.ПолучитьРезервМаркета(market.Id, НоменклатураList.Select(x => x.Id));

            var lockedNomIds = await bridge.ПолучитьLockedНоменклатураIds(headers.Authorization, номенклатураCodes);
            var nomQuantums = await bridge.ПолучитьКвант(номенклатураCodes, cancellationToken);
            var nomDeltaStock = await bridge.ПолучитьDeltaStock(market.Id, номенклатураCodes, cancellationToken);

            foreach (var requestedSku in requestedStock.Skus)
            {
                int count = 0;
                var номенклатура = НоменклатураList.Where(x => x.Code == requestedSku.Decode(market.Encoding))
                    .FirstOrDefault();
                if ((номенклатура != null) && !lockedNomIds.Any(x => x == номенклатура.Id))
                {
                    резервыМаркета.TryGetValue(номенклатура.Id, out decimal резервМаркета);
                    var quantum = (int)nomQuantums.Where(x => x.Key == номенклатура.Id).Select(x => x.Value).FirstOrDefault();
                    var deltaStock = (int)nomDeltaStock.Where(x => x.Key == номенклатура.Id).Select(x => x.Value).FirstOrDefault();
                    if (quantum > 1)
                    {
                        var остатокРегистр = номенклатура.Остатки
                            .Where(x => x.СкладId == Common.SkladEkran)
                            .Sum(x => x.СвободныйОстаток);
                        остатокРегистр += резервМаркета;
                        var остатокКвантов = (int)(((остатокРегистр / номенклатура.Единица.Коэффициент) - deltaStock) / quantum);
                        остатокКвантов = Math.Max(остатокКвантов, 0);
                        count = остатокКвантов * quantum;
                    }
                    else
                    {
                        var остатокРегистр = номенклатура.Остатки.Sum(x => x.СвободныйОстаток);
                        остатокРегистр += резервМаркета;
                        count = (int)(остатокРегистр / номенклатура.Единица.Коэффициент) - deltaStock;
                        count = Math.Max(count, 0);
                    }
                }
                responseStock.Skus.Add(new SkuEntry 
                {
                    Sku = requestedSku,
                    WarehouseId = requestedStock.WarehouseId,
                    Items = new List<SkuItem> 
                    {
                        new SkuItem
                        {
                            Type = ItemType.FIT,
                            Count = count.ToString(),
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
                var orderResult = await bridge.NewOrder(true, headers.Authorization, requestedOrder.Order);
                responseOrder.Order.Accepted = !string.IsNullOrEmpty(orderResult.Item1);
                if (responseOrder.Order.Accepted)
                {
                    responseOrder.Order.Id = orderResult.Item1;
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
            await bridge.ChangeStatus(null, headers.Authorization, requestedOrder.Order.Id, requestedOrder.Order.Status, requestedOrder.Order.SubStatus);
            if (!string.IsNullOrEmpty(requestedOrder.Order.ElectronicAcceptanceCertificateCode))
                await bridge.SetElectronicAcceptanceCertificateCode(headers.Authorization, requestedOrder.Order.Id, requestedOrder.Order.ElectronicAcceptanceCertificateCode, cancellationToken);
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
        [HttpPost("order/int_status_shipped")]
        public async Task<ActionResult<string>> OrderStatusShipped([FromBody] RequestedOrderId requestedOrderId, CancellationToken cancellationToken)
        {
            using IBridge1C bridge = _serviceScopeFactory.CreateScope()
                .ServiceProvider.GetService<IBridge1C>();
            var result = await bridge.SetStatusShipped(requestedOrderId.Id, requestedOrderId.UserId, requestedOrderId.PaymentType, requestedOrderId.ReceiverEmail, requestedOrderId.ReceiverPhone, cancellationToken);
            return Ok(result);
        }
        [HttpPost("order/int_status_cancelled_user_changed_mind")]
        public async Task<ActionResult<string>> OrderStatusCancelledUserChangeMind([FromBody] RequestedOrderId requestedOrderId, CancellationToken cancellationToken)
        {
            using IBridge1C bridge = _serviceScopeFactory.CreateScope()
                .ServiceProvider.GetService<IBridge1C>();
            var result = await bridge.SetStatusCancelledUserChangeMind(requestedOrderId.Id, cancellationToken);
            return Ok(result);
        }
    }
}
