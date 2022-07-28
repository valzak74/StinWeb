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
        private string _authApi;
        public YandexController(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;
            _defFirma = _configuration["Settings:Firma"];
            _defFirmaId = _configuration["Settings:" + _defFirma + ":FirmaId"];
            _authApi = _configuration["Settings:" + _defFirma + ":YandexFBS"];
        }
        [HttpPost("cart")]
        public async Task<ActionResult<ResponseCartFBS>> Cart([FromBody] RequestedCart requestedCart, CancellationToken cancellationToken)
        {
            var responseCart = new ResponseCartFBS();
            responseCart.Cart = new CartResponseFBSEntry();

            using IBridge1C bridge = _serviceScopeFactory.CreateScope()
                .ServiceProvider.GetService<IBridge1C>();
            List<string> списокСкладов = await bridge.ПолучитьСкладIdОстатковMarketplace(); 
            
            var market = await bridge.ПолучитьМаркет(_authApi, _defFirmaId);

            var номенклатураCodes = requestedCart.Cart.Items.Select(x => market.HexEncoding ? x.OfferId.DecodeHexString() : x.OfferId).ToList();
            var НоменклатураList = await bridge.ПолучитьСвободныеОстатки(номенклатураCodes, списокСкладов);
            
            var lockedNomIds = await bridge.ПолучитьLockedНоменклатураIds(_authApi, номенклатураCodes);
            var nomQuantums = await bridge.ПолучитьКвант(номенклатураCodes, cancellationToken);

            foreach (var requestedItem in requestedCart.Cart.Items)
            {
                var номенклатура = НоменклатураList.Where(x => x.Code == (market.HexEncoding ? requestedItem.OfferId.DecodeHexString() : requestedItem.OfferId)).FirstOrDefault();
                if (номенклатура != null)
                {
                    int МожноОтпустить = 0;
                    if (!lockedNomIds.Any(x => x == номенклатура.Id))
                    {
                        var quantum = (int)nomQuantums.Where(x => x.Key == номенклатура.Id).Select(x => x.Value).FirstOrDefault();
                        if (quantum > 1)
                        {
                            var запрошеноКвантов = (int)(requestedItem.Count / quantum);
                            if (запрошеноКвантов > 0)
                            {
                                var остатокКвантов = (int)((номенклатура.Остатки
                                    .Where(x => x.СкладId == Common.SkladEkran)
                                    .Sum(x => x.СвободныйОстаток) / номенклатура.Единица.Коэффициент) / quantum);
                                МожноОтпустить = Math.Min(запрошеноКвантов * quantum, остатокКвантов * quantum);
                            }
                        }
                        else
                            МожноОтпустить = Math.Min(requestedItem.Count, (int)(номенклатура.Остатки.Sum(x => x.СвободныйОстаток) / номенклатура.Единица.Коэффициент));
                    }
                    responseCart.Cart.Items.Add(new ResponseItemFBS
                    {
                        FeedId = requestedItem.FeedId,
                        OfferId = requestedItem.OfferId,
                        Count = МожноОтпустить,
                        Delivery = МожноОтпустить > 0
                    });
                }
            }
            return Ok(responseCart);
        }
        [HttpPost("stocks")]
        public async Task<ActionResult<ResponseStocks>> Stocks([FromBody] RequestStocks requestedStock, CancellationToken cancellationToken)
        {
            var responseStock = new ResponseStocks();

            using IBridge1C bridge = _serviceScopeFactory.CreateScope()
                .ServiceProvider.GetService<IBridge1C>();

            List<string> списокСкладов = await bridge.ПолучитьСкладIdОстатковMarketplace();
            
            var market = await bridge.ПолучитьМаркет(_authApi, _defFirmaId);
            var номенклатураCodes = market.HexEncoding ? requestedStock.Skus.Select(x => x.DecodeHexString()).ToList() : requestedStock.Skus;
            var НоменклатураList = await bridge.ПолучитьСвободныеОстатки(номенклатураCodes, списокСкладов);

            var lockedNomIds = await bridge.ПолучитьLockedНоменклатураIds(_authApi, номенклатураCodes);
            var nomQuantums = await bridge.ПолучитьКвант(номенклатураCodes, cancellationToken);

            foreach (var requestedSku in requestedStock.Skus)
            {
                int count = 0;
                var номенклатура = НоменклатураList.Where(x => x.Code == (market.HexEncoding ? requestedSku.DecodeHexString() : requestedSku)).FirstOrDefault();
                if ((номенклатура != null) && !lockedNomIds.Any(x => x == номенклатура.Id))
                {
                    var quantum = (int)nomQuantums.Where(x => x.Key == номенклатура.Id).Select(x => x.Value).FirstOrDefault();
                    if (quantum > 1)
                    {
                        var остатокКвантов = (int)((номенклатура.Остатки
                            .Where(x => x.СкладId == Common.SkladEkran)
                            .Sum(x => x.СвободныйОстаток) / номенклатура.Единица.Коэффициент) / quantum);
                        count = остатокКвантов * quantum;
                    }
                    else
                        count = (int)(номенклатура.Остатки.Sum(x => x.СвободныйОстаток) / номенклатура.Единица.Коэффициент);
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
        public async Task<ActionResult<ResponseOrder>> OrderAccept([FromBody] RequestedOrder requestedOrder, CancellationToken cancellationToken)
        {
            var responseOrder = new ResponseOrder();
            responseOrder.Order = new OrderResponseEntry();

            int tryCount = 5;
            TimeSpan sleepPeriod = TimeSpan.FromSeconds(1);
            while (true)
            {
                using IBridge1C bridge = _serviceScopeFactory.CreateScope()
                    .ServiceProvider.GetService<IBridge1C>();
                var orderResult = await bridge.NewOrder(true, _authApi, requestedOrder.Order);
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
                responseOrder.Order.Reason = FaultReason.OUT_OF_DATE;
            return Ok(responseOrder);
        }
        [HttpPost("order/status")]
        public async Task<ActionResult> OrderChangeStatus([FromBody] RequestedOrder requestedOrder, CancellationToken cancellationToken)
        {
            using IBridge1C bridge = _serviceScopeFactory.CreateScope()
                .ServiceProvider.GetService<IBridge1C>();
            await bridge.ChangeStatus(null, _authApi, requestedOrder.Order.Id, requestedOrder.Order.Status, requestedOrder.Order.SubStatus);
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
