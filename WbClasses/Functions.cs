using HttpExtensions;
using Newtonsoft.Json;
using System.Net;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using static WbClasses.CardListResponse;

namespace WbClasses
{
    public static class Functions
    {
        static readonly string queryKey = "QueryParameter";
        public static string ParseError(this string errorText, List<string> response)
        {
            if ((response != null) && (response.Count > 0))
            {
                foreach (var error in response)
                {
                    if (!string.IsNullOrEmpty(errorText))
                        errorText += Environment.NewLine;
                    errorText += error;
                }
            }
            return errorText;
        }
        public static string LogWbErrors(this Response response, string commonError)
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(commonError))
            {
                sb.Append("common: ");
                sb.Append(commonError);
            }
            if (!string.IsNullOrEmpty(response.Code))
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append("code:");
                sb.Append(response.Code);
            }
            if (!string.IsNullOrEmpty(response.Message))
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append("message:");
                sb.Append(response.Message);
            }
            return sb.ToString();
        }
        public static Dictionary<string, string> GetCustomHeaders(string authToken)
        {
            var result = new Dictionary<string, string>();
            result.Add("Authorization", authToken);
            return result;
        }
        public static async Task<(CardListData? data, string error)> GetCatalogInfo(IHttpService httpService, string proxyHost, string authToken,
            int limit,
            DateTime updatedAt,
            long nmId,
            CancellationToken cancellationToken)
        {
            CardsListRequest request;
            if ((nmId > 0) && (updatedAt != DateTime.MinValue))
                request = new CardsListRequest(limit, nmId, updatedAt);
            else
                request = new CardsListRequest(limit);
            var result = await httpService.Exchange<CardListResponse, string>(
                $"https://{proxyHost}suppliers-api.wildberries.ru/content/v1/cards/cursor/list",
                HttpMethod.Post,
                GetCustomHeaders(authToken),
                request,
                cancellationToken);
            string err = "";
            if (result.Item1 != null)
                err = result.Item1.LogWbErrors(result.Item2);
            else if (!string.IsNullOrEmpty(result.Item2))
                err = result.Item2;
            return (data: result.Item1?.Data, error: string.IsNullOrEmpty(err) ? "" : "WbGetCatalogInfo: " + err);
        }
        public static async Task<(bool, string?)> UpdatePrice(IHttpService httpService, string proxyHost, string authToken,
            List<PriceRequest> priceData,
            CancellationToken cancellationToken)
        {
            var result = await httpService.Exchange<bool, List<string>>(
                $"https://{proxyHost}suppliers-api.wildberries.ru/public/api/v1/prices",
                HttpMethod.Post,
                GetCustomHeaders(authToken),
                priceData,
                cancellationToken);
            if (result.Item2 != null)
            {
                return (false, "".ParseError(result.Item2));
            }
            return (result.Item1, null);
        }
        public static async Task<(bool success, Dictionary<string, string>? errors)> UpdateStock(IHttpService httpService, string proxyHost, string authToken,
            int warehouseId,
            Dictionary<string, int> data,
            CancellationToken cancellationToken)
        {
            var request = new StocksRequestV3(data);
            var result = await httpService.ExchangeErrorList<bool, StockError>(
                $"https://{proxyHost}suppliers-api.wildberries.ru/api/v3/stocks/{warehouseId}",
                HttpMethod.Put,
                GetCustomHeaders(authToken),
                request,
                cancellationToken);
            var errors = result.Errors?
                .SelectMany(stockError =>
                {
                    if (stockError.Data != null)
                        return stockError.Data
                                  .Where(x => !string.IsNullOrEmpty(x.Sku))
                                  .Select(x => new { ErrorCode = stockError.Code ?? "", Sku = x.Sku ?? "" });
                    else
                        return Enumerable.Repeat(new { ErrorCode = stockError.Code ?? "", Sku = "common" }, 1);
                })
                .GroupBy(x => x.Sku)
                .Select(gr => new { gr.Key, Value = string.Join(", ", gr.Select(x => x.ErrorCode ?? "")) })
                .ToDictionary(k => k.Key, v => v.Value);
            return (success: result.SuccessData, errors);
        }
        public static async Task<(OrderList data, string error)> GetNewOrders(IHttpService httpService, string proxyHost, string authToken,
            CancellationToken cancellationToken)
        {
            var result = await httpService.Exchange<OrderList, string>(
                $"https://{proxyHost}suppliers-api.wildberries.ru/api/v3/orders/new",
                HttpMethod.Get,
                GetCustomHeaders(authToken),
                null,
                cancellationToken);
            string err = "";
            if (!string.IsNullOrEmpty(result.Item2))
                err = result.Item2;
            return (data: result.Item1, error: string.IsNullOrEmpty(err) ? "" : "WbGetOrders: " + err);
        }
        public static async Task<(Dictionary<long, WbStatus>? orders, string error)> GetOrderStatuses(IHttpService httpService, string proxyHost, string authToken,
            List<long> orderIds,
            CancellationToken cancellationToken)
        {
            var request = new StickerRequest(orderIds);
            var result = await httpService.Exchange<OrderStatusResponse, WbErrorResponse>(
                $"https://{proxyHost}suppliers-api.wildberries.ru/api/v3/orders/status",
                HttpMethod.Post,
                GetCustomHeaders(authToken),
                request,
                cancellationToken);
            string err = "";
            if (result.Item2 != null)
                err = result.Item2.LogWbErrors("");
            return (orders: result.Item1?.Orders?.ToDictionary(k => k.Id, v => v.WbStatus), error: string.IsNullOrEmpty(err) ? "" : "WbGetOrderStatuses: " + err);
        }
        public static async Task<(bool success, string error)> CancelOrder(IHttpService httpService, string proxyHost, string authToken,
            string orderId,
            CancellationToken cancellationToken)
        {
            var result = await httpService.Exchange<bool, WbErrorResponse>(
                $"https://{proxyHost}suppliers-api.wildberries.ru/api/v3/orders/{orderId}/cancel",
                HttpMethod.Patch,
                GetCustomHeaders(authToken),
                null,
                cancellationToken);
            string err = "";
            if (result.Item2 != null)
                err = result.Item2.LogWbErrors("");
            return (success: result.Item1, error: string.IsNullOrEmpty(err) ? "" : "WbCancelOrder: " + err);

        }
        public static async Task<(byte[]? png, string? barcode, int? partA, int? partB, string error)> GetLabel(IHttpService httpService, string proxyHost, string authToken, List<long> orders, CancellationToken cancellationToken)
        {
            var headers = GetCustomHeaders(authToken);
            headers.Add(queryKey + "type", Enum.GetName(WbSupplyBarcodeType.png) ?? "");
            headers.Add(queryKey + "width", "58");
            headers.Add(queryKey + "height", "40");
            var result = await httpService.Exchange<StickerResponse, WbErrorResponse>(
                $"https://{proxyHost}suppliers-api.wildberries.ru/api/v3/orders/stickers",
                HttpMethod.Post,
                headers,
                new StickerRequest(orders),
                cancellationToken);
            string err = "";
            if (result.Item2 != null)
                err = result.Item2.LogWbErrors("");
            if (result.Item1 != null)
                err += result.Item1.LogWbErrors("");
            var responseData = result.Item1?.Stickers?.Select(x => new { x.Data, x.Barcode, x.PartA, x.PartB }).FirstOrDefault();
            return (png: responseData?.Data, barcode: responseData?.Barcode, partA: responseData?.PartA, partB: responseData?.PartB, error: string.IsNullOrEmpty(err) ? "" : "WbGetLabel: " + err);
        }
        public static async Task<(byte[]? png, string error)> GetSupplyBarcode(IHttpService httpService, string authToken, string supplyId, CancellationToken cancellationToken)
        {
            var headers = GetCustomHeaders(authToken);
            headers.Add(queryKey + "type", Enum.GetName(WbSupplyBarcodeType.png) ?? "");
            headers.Add(queryKey + "width", "58");
            headers.Add(queryKey + "height", "40");

            var result = await httpService.Exchange<WbBarcode, WbErrorResponse>(
                $"https://suppliers-api.wildberries.ru/api/v3/supplies/{supplyId}/barcode",
                HttpMethod.Get,
                headers,
                null,
                cancellationToken);
            string err = "";
            if (result.Item2 != null)
                err = result.Item2.LogWbErrors("");
            return (png: result.Item1?.Data, error: string.IsNullOrEmpty(err) ? "" : "WbGetSupplyBarcode: " + err);
        }
        public static async Task<(List<string> supplyIds, string error)> GetSuppliesList(IHttpService httpService, string proxyHost, string authToken, CancellationToken cancellationToken)
        {
            List<string> data = new List<string>();
            int limit = 1000;
            long next = 0;

            var headers = GetCustomHeaders(authToken);
            headers.Add(queryKey + "limit", limit.ToString());
            headers.Add(queryKey + "next", next.ToString());

            string err = "";
            bool nextPage = true;
            while (nextPage && (data.Count == 0))
            {
                nextPage = false;
                var result = await httpService.Exchange<SuppliesList, WbErrorResponse>(
                    $"https://{proxyHost}suppliers-api.wildberries.ru/api/v3/supplies",
                    HttpMethod.Get,
                    headers,
                    null,
                    cancellationToken);
                if (result.Item2 != null)
                    err += result.Item2.LogWbErrors("");
                if (result.Item1?.Next > next)
                {
                    nextPage = true;
                    next = result.Item1.Next;
                    headers[queryKey + "next"] = next.ToString();
                }
                if (result.Item1?.Supplies?.Count > 0)
                    foreach (var supply in result.Item1.Supplies.Where(x => !x.ClosedAt.HasValue || x.ClosedAt.Value == DateTime.MinValue))
                        data.Add(supply.Id ?? "");
            }
            return (supplyIds: data, error: string.IsNullOrEmpty(err) ? "" : "WbGetSuppliesList: " + err);
        }
        public static async Task<(List<Order>? orders, string error)> GetSupplyOrders(IHttpService httpService, string proxyHost, string authToken, string supplyId, CancellationToken cancellationToken)
        {
            var result = await httpService.Exchange<OrderList, WbErrorResponse>(
                $"https://{proxyHost}suppliers-api.wildberries.ru/api/v3/supplies/{supplyId}/orders",
                HttpMethod.Get,
                GetCustomHeaders(authToken),
                null,
                cancellationToken);
            string err = "";
            if (result.Item2 != null)
                err = result.Item2.LogWbErrors("");
            return (orders: result.Item1?.Orders, error: string.IsNullOrEmpty(err) ? "" : "WbGetSupplyOrders: " + err);
        }
        public static async Task<(string? supplyId, string error)> CreateSupply(IHttpService httpService, string proxyHost, string authToken, CancellationToken cancellationToken)
        {
            var result = await httpService.Exchange<Supply, WbErrorResponse>(
                $"https://{proxyHost}suppliers-api.wildberries.ru/api/v3/supplies",
                HttpMethod.Post,
                GetCustomHeaders(authToken),
                new SupplyName { Name = DateTime.Today.ToString("ddMMyyyymmss") },
                cancellationToken);
            string err = "";
            if (result.Item2 != null)
                err = result.Item2.LogWbErrors("");
            return (supplyId: result.Item1?.Id, error: string.IsNullOrEmpty(err) ? "" : "WbCreateSupply: " + err);
        }
        public static async Task<(bool success, string error)> AddToSupply(IHttpService httpService, string proxyHost, string authToken, 
            string supplyId, string orderId,
            CancellationToken cancellationToken)
        {
            var result = await httpService.Exchange<bool, WbErrorResponse>(
                $"https://{proxyHost}suppliers-api.wildberries.ru/api/v3/supplies/{supplyId}/orders/{orderId}",
                HttpMethod.Patch,
                GetCustomHeaders(authToken),
                null,
                cancellationToken);
            string err = "";
            if (result.Item2 != null)
                err = result.Item2.LogWbErrors("");
            return (success: result.Item1, error: string.IsNullOrEmpty(err) ? "" : "WbGetSuppliesList: " + err);
        }
        public static async Task<(Supply? supply, string error)> GetSupplyInfo(IHttpService httpService, string authToken, string supplyId, CancellationToken cancellationToken)
        {
            var result = await httpService.Exchange<Supply, WbErrorResponse>(
                $"https://suppliers-api.wildberries.ru/api/v3/supplies/{supplyId}",
                HttpMethod.Get,
                GetCustomHeaders(authToken),
                null,
                cancellationToken);
            string err = "";
            if (result.Item2 != null)
                err = result.Item2.LogWbErrors("");
            return (supply: result.Item1, error: string.IsNullOrEmpty(err) ? "" : "WbGetSupplyInfo: " + err);
        }
        public static async Task<(bool success, string error)> CloseSupply(IHttpService httpService, string authToken,
            string supplyId,
            CancellationToken cancellationToken)
        {
            var result = await httpService.Exchange<bool, WbErrorResponse>(
                $"https://suppliers-api.wildberries.ru/api/v3/supplies/{supplyId}/deliver",
                HttpMethod.Patch,
                GetCustomHeaders(authToken),
                null,
                cancellationToken);
            string err = "";
            if (result.Item2 != null)
                err = result.Item2.LogWbErrors("");
            return (success: result.Item1, error: string.IsNullOrEmpty(err) ? "" : "WbCloseSupply: " + err);
        }
    }
}