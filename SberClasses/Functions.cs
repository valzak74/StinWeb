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
        public static async Task<(bool success, string error)> OrderConfirm(IHttpService httpService, string token,
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
                "https://partner.goodsteam.tech/api/market/v1/orderService/order/confirm",
                HttpMethod.Post,
                new Dictionary<string, string>(),
                request,
                cancellationToken);
            string err = "";
            if (!string.IsNullOrEmpty(result.Item2))
                err = ("OrderConfirmResponse : ").ParseError(shipmentId, result.Item2);
            if (result.Item1 != null)
            {
                if (result.Item1.Error != null) 
                    err = err.ParseError(shipmentId, JsonConvert.SerializeObject(result.Item1.Error));
                return (result.Item1.Success == 1, err);
            }
            return (false, err);
        }
        public static async Task<(byte[]? pdf, string error)> StickerPrint(IHttpService httpService, string clientId, string token,
            string shipmentId,
            string orderCode,
            List<string> itemIndexes,
            CancellationToken cancellationToken)
        {
            string boxCodePrefix = clientId + "*" + orderCode + "*";
            var boxCodes = new List<string>();
            var items = new List<SberItem>();
            int boxIndex = 0;
            foreach (var item in itemIndexes)
            {
                boxIndex++;
                string boxCode = boxCodePrefix + boxIndex.ToString();
                boxCodes.Add(boxCode);
                items.Add(new SberItem 
                {
                    ItemIndex = item,
                    Quantity = 1,
                    Boxes = new List<SberBox>
                    {
                        new SberBox
                        {
                            BoxIndex = boxIndex,
                            BoxCode = boxCode
                        }
                    }
                });
            }

            var request = new SberRequest(token);
            request.Data.PrintAsPdf = true;
            request.Data.Shipments = new List<SberShipment> { new SberShipment { ShipmentId = shipmentId, BoxCodes = boxCodes, Items = items } };
            var result = await httpService.Exchange<byte[], string>(
                "https://partner.goodsteam.tech/api/market/v1/orderService/sticker/print",
                HttpMethod.Post,
                new Dictionary<string, string>(),
                request,
                cancellationToken);
            string err = "";
            if (!string.IsNullOrEmpty(result.Item2))
                err = ("StickerPrintResponse : ").ParseError(shipmentId, result.Item2);
            if (result.Item1 != null)
                return (result.Item1, err);
            return (null, err);
        }
        public static async Task<(bool success, string error)> OrderPicking(IHttpService httpService, string clientId, string token,
            string shipmentId,
            string orderCode,
            List<string> itemIndexes,
            CancellationToken cancellationToken)
        {
            var request = new SberRequest(token);
            request.Data.Shipments = new List<SberShipment> { new SberShipment { ShipmentId = shipmentId, OrderCode = orderCode, Items = new List<SberItem>() } };
            string boxCodePrefix = clientId + "*" + orderCode + "*";
            int boxIndex = 0;
            foreach (var item in itemIndexes)
            {
                boxIndex++;
                request.Data?.Shipments?[0].Items?.Add(new SberItem
                {
                    ItemIndex = item,
                    Boxes = new List<SberBox>
                    {
                        new SberBox { BoxIndex = boxIndex, BoxCode = boxCodePrefix + boxIndex.ToString() }
                    }
                });
            }
            var result = await httpService.Exchange<SberResponse, string>(
                "https://partner.goodsteam.tech/api/market/v1/orderService/order/packing",
                HttpMethod.Post,
                new Dictionary<string, string>(),
                request,
                cancellationToken);
            string err = "";
            if (!string.IsNullOrEmpty(result.Item2))
                err = ("OrderPickingResponse : ").ParseError(shipmentId, result.Item2);
            if (result.Item1 != null)
            {
                if (result.Item1.Error != null)
                    err = err.ParseError(shipmentId, JsonConvert.SerializeObject(result.Item1.Error));
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
                "https://partner.goodsteam.tech/api/market/v1/orderService/order/shipping",
                HttpMethod.Post,
                new Dictionary<string, string>(),
                request,
                cancellationToken);
            string err = "";
            if (!string.IsNullOrEmpty(result.Item2))
                err = ("OrderShippingResponse : ").ParseError(shipmentId, result.Item2);
            if (result.Item1 != null)
            {
                if (result.Item1.Error != null)
                    err = err.ParseError(shipmentId, JsonConvert.SerializeObject(result.Item1.Error));
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
                "https://partner.goodsteam.tech/api/market/v1/orderService/order/reject",
                HttpMethod.Post,
                new Dictionary<string, string>(),
                request,
                cancellationToken);
            string err = "";
            if (!string.IsNullOrEmpty(result.Item2))
                err = ("OrderRejectResponse : ").ParseError(shipmentId, result.Item2);
            if (result.Item1 != null)
            {
                if (result.Item1.Error != null)
                    err = err.ParseError(shipmentId, JsonConvert.SerializeObject(result.Item1.Error));
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
                "https://partner.goodsteam.tech/api/merchantIntegration/v1/offerService/stock/update",
                HttpMethod.Post,
                new Dictionary<string, string>(),
                request,
                cancellationToken);
            string err = "";
            if (!string.IsNullOrEmpty(result.Item2))
                err = ("SberStockResponse : ").ParseError("", result.Item2);
            if (result.Item1 != null)
            {
                if (result.Item1.Error != null)
                    err = err.ParseError("", JsonConvert.SerializeObject(result.Item1.Error));
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
                "https://partner.goodsteam.tech/api/merchantIntegration/v1/offerService/manualPrice/save",
                HttpMethod.Post,
                new Dictionary<string, string>(),
                request,
                cancellationToken);
            string err = "";
            if (!string.IsNullOrEmpty(result.Item2))
                err = ("SberPriceResponse : ").ParseError("", result.Item2);
            if (result.Item1 != null)
            {
                if (result.Item1.Error != null)
                    err = err.ParseError("", JsonConvert.SerializeObject(result.Item1.Error));
                if (result.Item1.Data?.Warnings != null)
                    err = err.ParseError("", JsonConvert.SerializeObject(result.Item1.Data?.Warnings));
                return ((result.Item1.Success == 1) && (result.Item1.Data?.SavedPrices == 1), err);
            }
            return (false, err);
        }
    }
}
