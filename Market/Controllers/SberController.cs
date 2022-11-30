using Market.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Threading;
using SberClasses;
using System;
using System.Linq;
using StinClasses.Справочники;
using System.Collections.Generic;
using YandexClasses;
using StinClasses;
using System.Xml.Linq;
using System.Text;
using System.IO;

namespace Market.Controllers
{
    [ApiController]
    [Route("sber")]
    public class SberController : ControllerBase
    {
        private IServiceScopeFactory _serviceScopeFactory;
        private IConfiguration _configuration;
        private string _defFirma;
        private string _defFirmaId;
        public SberController(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;
            _defFirma = _configuration["Settings:Firma"];
            _defFirmaId = _configuration["Settings:" + _defFirma + ":FirmaId"];
        }
        [HttpPost("order/new")]
        public async Task<ActionResult<SberResponse>> OrderAccept([FromHeader] HeadersParameters headers, [FromBody] SberRequest requestedOrder, CancellationToken cancellationToken)
        {
            var response = new SberResponse();
            string authorization = System.Text.ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(headers.Authorization.Substring(6)));
            string marketplaceName = "Sber FBS";
            var singleShipment = requestedOrder.Data.Shipments.FirstOrDefault();
            string orderId = singleShipment?.ShipmentId ?? string.Empty;
            string orderDeliveryShipmentId = singleShipment?.Label?.DeliveryId ?? string.Empty;
            DateTime orderShipmentDate = singleShipment?.Label?.ShippingDate ?? DateTime.MinValue;
            bool delivery = singleShipment?.Label?.DeliveryType?.Contains("самовывоз", StringComparison.InvariantCultureIgnoreCase) ?? false;
            string regionName = singleShipment?.Label?.City ?? string.Empty;
            if (string.IsNullOrEmpty(regionName))
                regionName = singleShipment?.Label?.Region ?? string.Empty;
            string outletId = string.Empty;
            List<OrderItem> orderItems = new List<OrderItem>();
            foreach (var item in singleShipment?.Items)
            {
                orderItems.Add(new OrderItem
                {
                    Id = item.ItemIndex,
                    Sku = item.OfferId,
                    Количество = item.Quantity ?? 0,
                    Цена = item.Price ?? 0,
                    ЦенаСоСкидкой = item.FinalPrice ?? 0,
                    //Вознаграждение = (decimal)x.Subsidy,
                    Доставка = delivery,
                    //ДопПараметры = x.Params,
                    ИдентификаторПоставщика = singleShipment?.Label?.MerchantName,
                    ИдентификаторСклада = singleShipment?.Label?.MerchantId.ToString(),
                    ИдентификаторСкладаПартнера = singleShipment?.Shipping?.ShippingPoint.ToString()
                });
            }
            double deliveryPrice = 0;
            double deliverySubsidy = 0;
            OrderRecipientAddress address = new OrderRecipientAddress
            {
                Postcode = "",
                Country = "",
                City = singleShipment?.Label?.City ?? "",
                Subway = "",
                Street = "",
                House = "",
                Block = "",
                Entrance = "",
                Entryphone = "",
                Floor = "",
                Apartment = ""
            };

            int tryCount = 5;
            TimeSpan sleepPeriod = TimeSpan.FromSeconds(1);
            while (true)
            {
                using IBridge1C bridge = _serviceScopeFactory.CreateScope()
                    .ServiceProvider.GetService<IBridge1C>();
                var orderResult = await bridge.NewOrder(
                    authorization,
                    marketplaceName,
                    orderId,
                    orderItems,
                    regionName,
                    outletId,
                    orderDeliveryShipmentId,
                    orderShipmentDate,
                    deliveryPrice,
                    deliverySubsidy,
                    address,
                    StinClasses.StinPaymentType.NotFound,
                    StinClasses.StinPaymentMethod.NotFound,
                    StinClasses.StinDeliveryPartnerType.SBER_MEGA_MARKET,
                    StinClasses.StinDeliveryType.DELIVERY,
                    singleShipment?.Label?.MerchantId.ToString(),
                    singleShipment?.Label?.MerchantName,
                    "0",
                    regionName,
                    "",
                    cancellationToken);
                response.Success = string.IsNullOrEmpty(orderResult.orderNo) ? 0 : 1;
                if (response.Success == 1)
                    break;
                else
                {
                    if (--tryCount == 0)
                        break;
                    await Task.Delay(sleepPeriod);
                }
            }
            if (response.Success == 0)
            {
                string dirPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_offers");
                string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_offers", RouteData.Values["controller"] + ".txt");
                if (!System.IO.Directory.Exists(dirPath))
                    System.IO.Directory.CreateDirectory(dirPath);
                if (!System.IO.File.Exists(fullPath))
                {
                    using (System.IO.StreamWriter sw = System.IO.File.CreateText(fullPath))
                    {
                        sw.WriteLine(DateTime.Now.ToString("dd:MM:yyyy HH:mm:ss") + " " + orderId);
                    }
                    return StatusCode((int)System.Net.Sockets.SocketError.ConnectionRefused);
                }
                else
                {
                    var errorData = System.IO.File.ReadAllLines(fullPath);
                    var last10records = errorData.Reverse().Take(10).Where(x => x.EndsWith(orderId, StringComparison.InvariantCulture));
                    if (last10records.Count() < 5)
                    {
                        using (System.IO.StreamWriter sw = System.IO.File.AppendText(fullPath))
                        {
                            sw.WriteLine(DateTime.Now.ToString("dd:MM:yyyy HH:mm:ss") + " " + orderId);
                        }
                        return StatusCode((int)System.Net.Sockets.SocketError.ConnectionRefused);
                    }
                }
            }
            return Ok(response);
        }
        [HttpPost("order/cancel")]
        public async Task<ActionResult<SberResponse>> OrderCancel([FromHeader] HeadersParameters headers, [FromBody] SberRequest requestedOrder, CancellationToken cancellationToken)
        {
            var response = new SberResponse();
            string authorization = System.Text.ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(headers.Authorization.Substring(6)));
            var singleShipment = requestedOrder.Data.Shipments.FirstOrDefault();
            string orderId = singleShipment?.ShipmentId ?? string.Empty;
            var cancelItems = new List<OrderItem>();
            foreach (var item in singleShipment.Items)
            {
                cancelItems.Add(new OrderItem { Sku = item.OfferId, Количество = 1 });
            }
            using IBridge1C bridge = _serviceScopeFactory.CreateScope()
                .ServiceProvider.GetService<IBridge1C>();
            bool isSuccess = await bridge.ReduceCancelItems(orderId, authorization, cancelItems, cancellationToken);
            response.Success = isSuccess ? 1 : 0;
            return Ok(response);
        }
        [HttpGet("products/{filename}")]
        public ActionResult GetProductFeed(string filename, CancellationToken cancellationToken)
        {
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "products", filename);
            if (!System.IO.File.Exists(fullPath))
                return NotFound();
            byte[] fileBytes = System.IO.File.ReadAllBytes(fullPath);
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, filename);
        }
    }
}
