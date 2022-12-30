using HttpExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SberClasses
{
    public static class Functions
    {
        public static string ParseError(this string errorText, string shipmentId, string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                if (!string.IsNullOrEmpty(errorText))
                    errorText += Environment.NewLine;
                if (!string.IsNullOrEmpty(shipmentId))
                    errorText += "(" + shipmentId + ") ";
                return errorText + message;
            }
            return errorText;
        }
        public static async Task<(bool success, string error)> OrderConfirm(IHttpService httpService, string proxyHost, string token,
            string shipmentId,
            string orderCode,
            List<KeyValuePair<string,string>> items,
            CancellationToken cancellationToken)
        {
            var request = new SberRequest(token);
            request.Data.Shipments = new List<SberShipment> { new SberShipment { ShipmentId = shipmentId, OrderCode = orderCode, Items = new List<SberItem>() } };
            foreach (var item in items)
            {
                request.Data?.Shipments?[0].Items?.Add(new SberItem
                {
                    ItemIndex = item.Key,
                    OfferId = item.Value
                });
            }
            var result = await httpService.Exchange<SberResponse, string>(
                $"https://{proxyHost}partner.sbermegamarket.ru/api/market/v1/orderService/order/confirm",
                HttpMethod.Post,
                new Dictionary<string, string>(),
                request,
                cancellationToken);
            string err = "";
            if (!string.IsNullOrEmpty(result.Item2))
                err = "OrderConfirmResponse : ".ParseError(shipmentId, result.Item2);
            if (result.Item1 != null)
            {
                if (result.Item1.Error != null) 
                    err = err.ParseError(shipmentId, "OrderConfirmResponse : " + JsonConvert.SerializeObject(result.Item1.Error));
                return (result.Item1.Success == 1, err);
            }
            return (false, err);
        }
        public static async Task<(byte[]? pdf, string error)> StickerPrint(IHttpService httpService, string proxyHost, string clientId, string token,
            string shipmentId,
            string orderCode,
            List<KeyValuePair<string, int>> items,
            CancellationToken cancellationToken)
        {
            string boxCodePrefix = clientId + "*" + orderCode + "*";
            var boxCodes = new List<string>();
            var requestItems = new List<SberItem>();
            int boxIndex = 0;
            foreach (var item in items)
            {
                var boxes = new List<SberBox>();
                for (int i = 0; i < item.Value; i++)
                {
                    boxIndex++;
                    string boxCode = boxCodePrefix + boxIndex.ToString();
                    boxCodes.Add(boxCode);
                    boxes.Add(new SberBox
                    {
                        BoxIndex = boxIndex,
                        BoxCode = boxCode
                    });
                }
                requestItems.Add(new SberItem
                {
                    ItemIndex = item.Key,
                    Quantity = 1,
                    Boxes = boxes
                });
            }

            var request = new SberRequest(token);
            request.Data.PrintAsPdf = true;
            request.Data.Shipments = new List<SberShipment> { new SberShipment { ShipmentId = shipmentId, BoxCodes = boxCodes, Items = requestItems } };
            var result = await httpService.Exchange<byte[], string>(
                $"https://{proxyHost}partner.sbermegamarket.ru/api/market/v1/orderService/sticker/print",
                HttpMethod.Post,
                new Dictionary<string, string>(),
                request,
                cancellationToken);
            string err = "";
            if (!string.IsNullOrEmpty(result.Item2))
                err = "StickerPrintResponse : ".ParseError(shipmentId, result.Item2);
            if (result.Item1 != null)
                return (result.Item1, err);
            return (null, err);
        }
        public static async Task<(bool success, string error)> OrderPicking(IHttpService httpService, string proxyHost, string clientId, string token,
            string shipmentId,
            string orderCode,
            List<KeyValuePair<string, int>> items,
            CancellationToken cancellationToken)
        {
            var request = new SberRequest(token);
            request.Data.Shipments = new List<SberShipment> { new SberShipment { ShipmentId = shipmentId, OrderCode = orderCode, Items = new List<SberItem>() } };
            string boxCodePrefix = clientId + "*" + orderCode + "*";
            int boxIndex = 0;
            foreach (var item in items)
            {
                var boxes = new List<SberBox>();
                for (int i = 0; i < item.Value; i++)
                {
                    boxIndex++;
                    boxes.Add(new SberBox
                    {
                        BoxIndex = boxIndex,
                        BoxCode = boxCodePrefix + boxIndex.ToString()
                    });
                }
                request.Data?.Shipments?[0].Items?.Add(new SberItem
                {
                    ItemIndex = item.Key,
                    Boxes = boxes
                });
            }
            var result = await httpService.Exchange<SberResponse, string>(
                $"https://{proxyHost}partner.sbermegamarket.ru/api/market/v1/orderService/order/packing",
                HttpMethod.Post,
                new Dictionary<string, string>(),
                request,
                cancellationToken);
            string err = "";
            if (!string.IsNullOrEmpty(result.Item2))
                err = "OrderPickingResponse : ".ParseError(shipmentId, result.Item2);
            if (result.Item1 != null)
            {
                if (result.Item1.Error != null)
                    err = err.ParseError(shipmentId, "OrderPickingResponse : " + JsonConvert.SerializeObject(result.Item1.Error));
                return ((result.Item1.Success == 1) && (result.Item1.Data?.Result == 1), err);
            }
            return (false, err);
        }
        public static async Task<(bool success, string error)> OrderShipping(IHttpService httpService, string clientId, string token,
            string shipmentId,
            string orderCode,
            DateTime shippingDate,
            int boxsCount,
            CancellationToken cancellationToken)
        {
            string boxCodePrefix = clientId + "*" + orderCode + "*";
            var boxes = new List<SberBox>();
            for (int boxIndex = 1; boxIndex <= boxsCount; boxIndex++)
            {
                boxes.Add(new SberBox 
                {
                    BoxIndex = boxIndex,
                    BoxCode = boxCodePrefix + boxIndex.ToString()
                });
            }

            var request = new SberRequest(token);
            request.Data.Shipments = new List<SberShipment> { new SberShipment { ShipmentId = shipmentId, Boxes = boxes, Shipping = new SberShipping { ShippingDate = shippingDate } } };
            var result = await httpService.Exchange<SberResponse, string>(
                "https://partner.sbermegamarket.ru/api/market/v1/orderService/order/shipping",
                HttpMethod.Post,
                new Dictionary<string, string>(),
                request,
                cancellationToken);
            string err = "";
            if (!string.IsNullOrEmpty(result.Item2))
                err = "OrderShippingResponse : ".ParseError(shipmentId, result.Item2);
            if (result.Item1 != null)
            {
                if (result.Item1.Error != null)
                    err = err.ParseError(shipmentId, "OrderShippingResponse : " + JsonConvert.SerializeObject(result.Item1.Error));
                return ((result.Item1.Success == 1) && (result.Item1.Data?.Result == 1), err);
            }
            return (false, err);
        }
        public static async Task<(bool success, string error)> OrderReject(IHttpService httpService, string token,
            string shipmentId,
            SberReason reason,
            List<KeyValuePair<string, string>> items,
            CancellationToken cancellationToken)
        {
            var sberItems = new List<SberItem>();
            foreach (var item in items)
            {
                sberItems.Add(new SberItem
                {
                    ItemIndex = item.Key,
                    OfferId = item.Value
                });
            }
            var request = new SberRequest(token);
            request.Data.Reason = reason;
            request.Data.Shipments = new List<SberShipment> { new SberShipment { ShipmentId = shipmentId, Items = sberItems } };

            var result = await httpService.Exchange<SberResponse, string>(
                "https://partner.sbermegamarket.ru/api/market/v1/orderService/order/reject",
                HttpMethod.Post,
                new Dictionary<string, string>(),
                request,
                cancellationToken);
            string err = "";
            if (!string.IsNullOrEmpty(result.Item2))
                err = "OrderRejectResponse : ".ParseError(shipmentId, result.Item2);
            if (result.Item1 != null)
            {
                if (result.Item1.Error != null)
                    err = err.ParseError(shipmentId, "OrderRejectResponse : " + JsonConvert.SerializeObject(result.Item1.Error));
                return ((result.Item1.Success == 1) && (result.Item1.Data?.Result == 1), err);
            }
            return (false, err);
        }
        public static async Task<(bool success, string error)> UpdateStock(IHttpService httpService, string token,
            Dictionary<string, int> items,
            CancellationToken cancellationToken)
        {
            var sberStock = new List<SberStock>();
            foreach (var item in items)
            {
                sberStock.Add(new SberStock
                {
                    OfferId = item.Key,
                    Quantity = item.Value
                });
            }
            var request = new SberRequest(token);
            request.Data.Stocks = sberStock;

            var result = await httpService.Exchange<SberResponseSingleError, string>(
                "https://partner.sbermegamarket.ru/api/merchantIntegration/v1/offerService/stock/update",
                HttpMethod.Post,
                new Dictionary<string, string>(),
                request,
                cancellationToken);
            string err = "";
            if (!string.IsNullOrEmpty(result.Item2))
                err = "SberStockResponse : ".ParseError("", result.Item2);
            if (result.Item1 != null)
            {
                if (result.Item1.Error != null)
                    err = err.ParseError("", "SberStockResponse : " + JsonConvert.SerializeObject(result.Item1.Error));
                return (result.Item1.Success == 1, err);
            }
            return (false, err);
        }
        public static async Task<(bool success, string error)> UpdatePrice(IHttpService httpService, string token,
            Dictionary<string, int> items,
            CancellationToken cancellationToken)
        {
            var sberPrice = new List<SberPrice>();
            foreach (var item in items)
            {
                sberPrice.Add(new SberPrice
                {
                    OfferId = item.Key,
                    Price = item.Value,
                    IsDeleted = false
                });
            }
            var request = new SberRequest(token);
            request.Data.Prices = sberPrice;

            var result = await httpService.Exchange<SberResponseSingleError, string>(
                "https://partner.sbermegamarket.ru/api/merchantIntegration/v1/offerService/manualPrice/save",
                HttpMethod.Post,
                new Dictionary<string, string>(),
                request,
                cancellationToken);
            string err = "";
            if (!string.IsNullOrEmpty(result.Item2))
                err = "SberPriceResponse : ".ParseError("", result.Item2);
            if (result.Item1 != null)
            {
                if (result.Item1.Error != null)
                    err = err.ParseError("", "SberPriceResponse : " +JsonConvert.SerializeObject(result.Item1.Error));
                if (result.Item1.Data?.Warnings?.Count > 0)
                    err = err.ParseError("", "SberPriceResponse : " + JsonConvert.SerializeObject(result.Item1.Data?.Warnings));
                return ((result.Item1.Success == 1) && (result.Item1.Data?.SavedPrices == request.Data.Prices.Count), err);
            }
            return (false, err);
        }
        public static async Task<(List<SberDetailOrder>? orders, string error)> GetOrders(IHttpService httpService, string token,
            CancellationToken cancellationToken)
        {
            var request = new OrderListRequest(token);
            request.Data.DateFrom = DateTime.Today.AddDays(-30);
            request.Data.DateTo = DateTime.Today;
            request.Data.Statuses = new List<SberStatus> { SberStatus.CONFIRMED, SberStatus.CUSTOMER_CANCELED };

            var result = await httpService.Exchange<OrderListResponse, string>(
                "https://partner.sbermegamarket.ru/api/market/v1/orderService/order/search",
                HttpMethod.Post,
                new Dictionary<string, string>(),
                request,
                cancellationToken);
            string err = "";
            if (!string.IsNullOrEmpty(result.Item2))
                err = err.ParseError("", result.Item2);
            if (result.Item1 != null)
            {
                if (result.Item1.Error != null)
                    err = err.ParseError("", JsonConvert.SerializeObject(result.Item1.Error));
                if (result.Item1.Data?.Warnings?.Count > 0)
                    err = err.ParseError("", JsonConvert.SerializeObject(result.Item1.Data?.Warnings));
                if (result.Item1.Data?.Shipments?.Count > 0)
                {
                    request.Data.Shipments = result.Item1.Data?.Shipments;
                    var resultDetails = await httpService.Exchange<OrderListDetailResponse, string>(
                        "https://partner.sbermegamarket.ru/api/market/v1/orderService/order/get",
                        HttpMethod.Post,
                        new Dictionary<string, string>(),
                        request,
                        cancellationToken);
                    if (!string.IsNullOrEmpty(resultDetails.Item2))
                        err = err.ParseError("", resultDetails.Item2);
                    if (resultDetails.Item1 != null)
                    {
                        if (resultDetails.Item1.Error != null)
                            err = err.ParseError("", JsonConvert.SerializeObject(resultDetails.Item1.Error));
                        if (resultDetails.Item1.Data?.Warnings?.Count > 0)
                            err = err.ParseError("", JsonConvert.SerializeObject(resultDetails.Item1.Data?.Warnings));
                        return (resultDetails.Item1.Data?.Shipments, string.IsNullOrEmpty(err) ? string.Empty : "SberGetOrders: " + err);
                    }
                }
                return (null, string.IsNullOrEmpty(err) ? string.Empty : "SberGetOrders: " + err);
            }
            return (null, string.IsNullOrEmpty(err) ? string.Empty : "SberGetOrders: " + err);
        }
        public static int NDS(decimal percent)
        {
            return percent switch
            {
                20 => 1,
                10 => 2,
                0 => 5,
                _ => 6
            };
        }
    }
}
