using HttpExtensions;
using Newtonsoft.Json;
using System.Net;
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
                sb.Append("common: " + commonError);
            if (!string.IsNullOrEmpty(response.ErrorText))
                sb.Append("errorText:" + response.ErrorText);
            if (response.AdditionalErrors != null)
                sb.Append("additionalError: " + JsonConvert.SerializeObject(response.AdditionalErrors));
            return sb.ToString();
        }
        public static Dictionary<string, string> GetCustomHeaders(string authToken)
        {
            var result = new Dictionary<string, string>();
            result.Add("Authorization", authToken);
            return result;
        }
        public static async Task<(CardListData? data, string error)> GetCatalogInfo(IHttpService httpService, string authToken,
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
                "https://suppliers-api.wildberries.ru/content/v1/cards/cursor/list",
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
        public static async Task<(bool, string?)> UpdatePrice(IHttpService httpService, string authToken,
            List<PriceRequest> priceData,
            CancellationToken cancellationToken)
        {
            var result = await httpService.Exchange<bool, List<string>>(
                "https://suppliers-api.wildberries.ru/public/api/v1/prices",
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
        public static async Task<(bool success, Dictionary<string,string>? errors)> UpdateStock(IHttpService httpService, string authToken,
            int warehouseId,
            Dictionary<string, int> data,
            CancellationToken cancellationToken)
        {
            var request = new List<StocksRequest>();
            foreach (var item in data)
            {
                request.Add(new StocksRequest(item.Key, item.Value, warehouseId));
            }
            var result = await httpService.Exchange<StocksResponse, string>(
                "https://suppliers-api.wildberries.ru/api/v2/stocks",
                HttpMethod.Post,
                GetCustomHeaders(authToken),
                request,
                cancellationToken);
            bool success = !result.Item1?.Error ?? false;
            Dictionary<string, string> errors = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(result.Item2))
                errors.Add("common", result.Item2);
            if (!string.IsNullOrEmpty(result.Item1?.ErrorText))
                errors.Add("errorText", result.Item1.ErrorText);
            if (result.Item1?.AdditionalErrors != null)
                errors.Add("additionalError", JsonConvert.SerializeObject(result.Item1.AdditionalErrors));
            if (result.Item1?.Data?.Error?.Count > 0)
                foreach (var err in result.Item1.Data.Error)
                {
                    if (!string.IsNullOrEmpty(err.Barcode) && !string.IsNullOrEmpty(err.Err))
                        errors.Add(err.Barcode, err.Err);
                }
            return (success, errors.Count > 0 ? errors : null);
        }
        public static async Task<(OrderList data, string error)> GetOrders(IHttpService httpService, string authToken,
            DateTime dateStart, DateTime dateEnd, int status, int skip, int take, int id,
            CancellationToken cancellationToken)
        {
            var headers = GetCustomHeaders(authToken);
            headers.Add(queryKey + "date_start", dateStart.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            headers.Add(queryKey + "skip", skip.ToString());
            headers.Add(queryKey + "take", take.ToString());
            if (dateEnd != DateTime.MinValue)
                headers.Add(queryKey + "date_end", dateEnd.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            if (status >= 0)
                headers.Add(queryKey + "status", status.ToString());
            if (id >= 0)
                headers.Add(queryKey + "id", id.ToString());

            var result = await httpService.Exchange<OrderList, string>(
                "https://suppliers-api.wildberries.ru/api/v2/orders",
                HttpMethod.Get,
                headers,
                null,
                cancellationToken);
            string err = "";
            if (!string.IsNullOrEmpty(result.Item2))
                err = result.Item2;
            return (data: result.Item1, error: string.IsNullOrEmpty(err) ? "" : "WbGetOrders: " + err);
        }
        public static async Task<(bool success, string error)> ChangeOrderStatus(IHttpService httpService, string authToken,
            string orderId,
            WbStatus newStatus,
            CancellationToken cancellationToken)
        {
            var request = new ChangeOrderStatusRequest();
            request.OrderId = orderId;
            request.Status = newStatus;
            var result = await httpService.Exchange<bool, ChangeOrderStatusErrorResponse>(
                "https://suppliers-api.wildberries.ru/api/v2/orders",
                HttpMethod.Put,
                GetCustomHeaders(authToken),
                request,
                cancellationToken);
            string err = "";
            if (result.Item2 != null)
                err = result.Item2.LogWbErrors("");
            return (success: result.Item1, error: string.IsNullOrEmpty(err) ? "" : "WbChangeOrderStatus: " + err);

        }
        public static async Task<(byte[]? pdf, string error)> GetLabel(IHttpService httpService, string authToken, List<long> orders, CancellationToken cancellationToken)
        {
            var result = await httpService.Exchange<StickerResponse, WbErrorResponse>(
                "https://suppliers-api.wildberries.ru/api/v2/orders/stickers/pdf",
                HttpMethod.Post,
                GetCustomHeaders(authToken),
                new StickerRequest(orders),
                cancellationToken);
            string err = "";
            if (result.Item2 != null)
                err = result.Item2.LogWbErrors("");
            if (result.Item1 != null)
                err += result.Item1.LogWbErrors("");
            return (pdf: result.Item1?.Data?.Barcode, error: string.IsNullOrEmpty(err) ? "" : "WbGetLabel: " + err);
        }
        public static async Task<(byte[]? pdf, string error)> GetSupplyBarcode(IHttpService httpService, string authToken, string id, CancellationToken cancellationToken)
        {
            var headers = GetCustomHeaders(authToken);
            headers.Add(queryKey + "type", Enum.GetName(WbSupplyBarcodeType.pdf) ?? "");

            var result = await httpService.Exchange<WbBarcode, WbErrorResponse>(
                $"https://suppliers-api.wildberries.ru/api/v2/supplies/{id}/barcode",
                HttpMethod.Get,
                headers,
                null,
                cancellationToken);
            string err = "";
            if (result.Item2 != null)
                err = result.Item2.LogWbErrors("");
            return (pdf: result.Item1?.Barcode, error: string.IsNullOrEmpty(err) ? "" : "WbGetSupplyBarcode: " + err);
        }
        public static async Task<(List<string>? supplyIds, string error)> GetSuppliesList(IHttpService httpService, string authToken, CancellationToken cancellationToken)
        {
            var headers = GetCustomHeaders(authToken);
            headers.Add(queryKey + "status", Enum.GetName(WbSupplyStatus.ACTIVE) ?? "");

            var result = await httpService.Exchange<SuppliesList, WbErrorResponse>(
                "https://suppliers-api.wildberries.ru/api/v2/supplies",
                HttpMethod.Get,
                headers,
                null,
                cancellationToken);
            string err = "";
            if (result.Item2 != null)
                err = result.Item2.LogWbErrors("");
            return (supplyIds: result.Item1?.Supplies?.Select(x => x.SupplyId ?? "").ToList(), error: string.IsNullOrEmpty(err) ? "" : "WbGetSuppliesList: " + err);
        }
        public static async Task<(List<Order>? orders, string error)> GetSupplyOrders(IHttpService httpService, string authToken, string id, CancellationToken cancellationToken)
        {
            var result = await httpService.Exchange<OrderList, WbErrorResponse>(
                $"https://suppliers-api.wildberries.ru/api/v2/supplies/{id}/orders",
                HttpMethod.Get,
                GetCustomHeaders(authToken),
                null,
                cancellationToken);
            string err = "";
            if (result.Item2 != null)
                err = result.Item2.LogWbErrors("");
            return (orders: result.Item1?.Orders, error: string.IsNullOrEmpty(err) ? "" : "WbGetSupplyOrders: " + err);
        }
        public static async Task<(string? supplyId, string error)> CreateSupply(IHttpService httpService, string authToken, CancellationToken cancellationToken)
        {
            var result = await httpService.Exchange<Supply, WbErrorResponse>(
                "https://suppliers-api.wildberries.ru/api/v2/supplies",
                HttpMethod.Post,
                GetCustomHeaders(authToken),
                null,
                cancellationToken);
            string err = "";
            if (result.Item2 != null)
                err = result.Item2.LogWbErrors("");
            return (supplyId: result.Item1?.SupplyId, error: string.IsNullOrEmpty(err) ? "" : "WbCreateSupply: " + err);
        }
        public static async Task<(bool success, string error)> AddToSupply(IHttpService httpService, string authToken, 
            string id,
            List<string> orders,
            CancellationToken cancellationToken)
        {
            var result = await httpService.Exchange<bool, WbErrorResponse>(
                $"https://suppliers-api.wildberries.ru/api/v2/supplies/{id}",
                HttpMethod.Put,
                GetCustomHeaders(authToken),
                new AddToSupply(orders),
                cancellationToken);
            string err = "";
            if (result.Item2 != null)
                err = result.Item2.LogWbErrors("");
            return (success: result.Item1, error: string.IsNullOrEmpty(err) ? "" : "WbGetSuppliesList: " + err);
        }
        public static async Task<(bool success, string error)> CloseSupply(IHttpService httpService, string authToken,
            string id,
            CancellationToken cancellationToken)
        {
            var result = await httpService.Exchange<bool, WbErrorResponse>(
                $"https://suppliers-api.wildberries.ru/api/v2/supplies/{id}/close",
                HttpMethod.Post,
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