using HttpExtensions;
using JsonExtensions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace YandexClasses
{
    public static class YandexOperators
    {
        public static readonly string urlChangeItems = "https://api.partner.market.yandex.ru/v2/campaigns/{0}/orders/{1}/items.json";
        public static string ParseErrorResponse(ErrorResponse response)
        {
            if (response != null && response.Errors != null && response.Errors.Count > 0)
            {
                string infoError = "";
                foreach (var responseError in response.Errors)
                    infoError += (infoError.Length > 0 ? ", " : "") + responseError.Code.Trim() + " : " + responseError.Message.Trim();
                return infoError;
            }
            return "";
        }
        public static Dictionary<string, string> GetCustomHeaders(string clientId, string authToken)
        {
            var result = new Dictionary<string, string>();
            result.Add("Authorization", $"OAuth oauth_token=\"{authToken}\", oauth_client_id=\"{clientId}\"");
            return result;
        }
        public static async Task<(ResponseStatus, T, string)> Exchange<T>(IHttpService httpService, string url, HttpMethod method, string clientId, string authToken, object content, CancellationToken cancellationToken)
        {
            var result = await httpService.Exchange<T, ErrorResponse>(
                url,
                method,
                GetCustomHeaders(clientId, authToken),
                content,
                cancellationToken);
            if (result.Item2 != null)
            {
                return new(ResponseStatus.ERROR, default, ParseErrorResponse(result.Item2));
            }
            if (result.Item1 != null)
            {
                return new(ResponseStatus.OK, result.Item1, default);
            }
            return new(ResponseStatus.ERROR, default, default);
        }
        public static Region FindRegion(this Region currentNode, RegionType searchType, string searchValue)
        {
            if ((searchType == currentNode.Type) && (currentNode.Name.Equals(searchValue, StringComparison.OrdinalIgnoreCase)))
            {
                return currentNode;
            }
            else
            {
                if (currentNode.Parent != null)
                {
                    return currentNode.Parent.FindRegion(searchType, searchValue);
                }
            }
            return null;
        }
        public static Region FindRegionByType(this Region currentNode, RegionType searchType)
        {
            if (searchType == currentNode.Type)
            {
                return currentNode;
            }
            else
            {
                if (currentNode.Parent != null)
                {
                    return currentNode.Parent.FindRegionByType(searchType);
                }
            }
            return null;
        }
        public static async Task<Buyer> BuyerDetails(IHttpService httpService, string proxyHost, string campaignId, string clientId, string authToken, string orderNo, CancellationToken cancellationToken)
        {
            var result = await Exchange<BuyerDetailsResponse>(httpService,
                $"https://{proxyHost}api.partner.market.yandex.ru/v2/campaigns/{campaignId}/orders/{orderNo}/buyer.json",
                HttpMethod.Get,
                clientId,
                authToken,
                null,
                cancellationToken);
            if ((result.Item2 != null) && (result.Item2.Result != null))
                return result.Item2.Result;
            return null;
        }
        public static async Task<Tuple<StatusYandex, SubStatusYandex>> OrderDetails(IHttpService httpService, string proxyHost, string campaignId, string clientId, string authToken, string orderNo, CancellationToken cancellationToken)
        {
            var result = await Exchange<OrderDetailsResponse>(httpService,
                $"https://{proxyHost}api.partner.market.yandex.ru/v2/campaigns/{campaignId}/orders/{orderNo}.json",
                HttpMethod.Get,
                clientId,
                authToken,
                null,
                cancellationToken);
            //var result = await YandexExchange(null, string.Format(urlDetails, campaignId, orderNo), HttpMethod.Get, clientId, authToken, null);
            if (result.Item2 != null)
            {
                try
                {
                    //var response = result.Item2.DeserializeObject<OrderDetailsResponse>();
                    if (result.Item2.Order != null)
                        return new(result.Item2.Order.Status, result.Item2.Order.SubStatus);
                }
                catch { }
            }
            return new(StatusYandex.NotFound, SubStatusYandex.NotFound);
        }
        public static async Task<OrderDetailsResponse> OrderDetailsFull(IHttpService httpService, string proxyHost, 
            string campaignId, string clientId, string authToken, string orderNo, CancellationToken cancellationToken)
        {
            var result = await Exchange<OrderDetailsResponse>(httpService,
                $"https://{proxyHost}api.partner.market.yandex.ru/v2/campaigns/{campaignId}/orders/{orderNo}.json",
                HttpMethod.Get,
                clientId,
                authToken,
                null,
                cancellationToken);
            return result.Item2;
        }
        public static async Task<List<Tuple<string, StatusYandex, SubStatusYandex, bool>>> OrderCancelList(IHttpService httpService, string proxyHost, string campaignId, string clientId, string authToken, int pageNumber, CancellationToken cancellationToken)
        {
            var url = $"https://{proxyHost}api.partner.market.yandex.ru/v2/campaigns/{campaignId}/orders.json?status=CANCELLED&page={pageNumber}";
            var result = await Exchange<OrderDetailListResponse>(httpService,
                url,
                HttpMethod.Get,
                clientId,
                authToken,
                null,
                cancellationToken);
            if (result.Item2 != null)
            {
                try
                {
                    var data = new List<Tuple<string, StatusYandex, SubStatusYandex, bool>>();
                    var response = result.Item2;
                    bool needNextPage = (response.Pager != null) && (response.Pager.CurrentPage < response.Pager.PagesCount);
                    if ((response.Orders != null) && (response.Orders.Count > 0))
                    {
                        foreach (var item in response.Orders)
                        {
                            data.Add(new(item.Id.ToString(), item.Status, item.SubStatus, needNextPage));
                        }
                    }
                    return data;
                }
                catch { }
            }
            return null;
        }
        public static async Task<(List<DetailOrder> Orders, bool NextPage)> OrdersList(IHttpService httpService, 
            string proxyHost,
            string campaignId, 
            string clientId, 
            string authToken, 
            string status,
            DateTime fromDate,
            int pageNumber, 
            CancellationToken cancellationToken)
        {
            var url = $"https://{proxyHost}api.partner.market.yandex.ru/v2/campaigns/{campaignId}/orders.json?status={status}&fromDate={fromDate.ToString("dd-MM-yyyy")}&page={pageNumber}";
            var result = await Exchange<OrderDetailListResponse>(httpService,
                url,
                HttpMethod.Get,
                clientId,
                authToken,
                null,
                cancellationToken);
            return (Orders: result.Item2?.Orders, NextPage: result.Item2?.Pager?.CurrentPage < result.Item2?.Pager?.PagesCount);
        }
        public static async Task<Tuple<bool, string>> UpdateOfferEntries(IHttpService httpService, string proxyHost, string campaignId, string clientId, string authToken, List<OfferMappingEntry> data, CancellationToken cancellationToken)
        {
            var request = new OfferMappingUpdateRequest { OfferMappingEntries = data };
            var result = await Exchange<OfferMappingUpdateResponse>(httpService,
                $"https://{proxyHost}api.partner.market.yandex.ru/v2/campaigns/{campaignId}/offer-mapping-entries/updates.json",
                HttpMethod.Post,
                clientId,
                authToken,
                request,
                cancellationToken);
            //var result = await YandexExchange(null, string.Format(urlOfferUpdate, campaignId), HttpMethod.Post, clientId, authToken, request.SerializeObject());
            string err = "";
            if (!string.IsNullOrEmpty(result.Item3))
                err = result.Item3;
            if (result.Item2 != null)
                try
                {
                    var response = result.Item2;
                    if ((response.Errors != null) && (response.Errors.Count > 0))
                        foreach (var responseError in response.Errors)
                            err += (err.Length > 0 ? ", " : "") + responseError.Code.Trim() + " : " + responseError.Message.Trim();
                    return new(response.Status == ResponseStatus.OK, err);
                }
                catch { }
            return new(false, err);
        }
        public static async Task<Tuple<bool,string>> ChangeStatus(IHttpService httpService, string proxyHost,
            string campaignId, string marketplaceId, 
            string clientId, string authToken,
            StatusYandex status, SubStatusYandex subStatus,
            DeliveryType deliveryType,
            CancellationToken cancellationToken)
        {
            var request = new StatusRequest { Order = new OrderStatus { Status = status, Substatus = subStatus } };
            //if ((deliveryType == DeliveryType.PICKUP) && (status == StatusYandex.PICKUP))
            //    request.Order.Delivery = new StatusDelivery { Dates = new StatusDates { RealDeliveryDate = DateTime.Now } };
            var result = await Exchange<StatusResponse>(httpService,
                $"https://{proxyHost}api.partner.market.yandex.ru/v2/campaigns/{campaignId}/orders/{marketplaceId}/status.json",
                HttpMethod.Put,
                clientId,
                authToken,
                request,
                cancellationToken);
            string err = "";
            if (result.Item1 == ResponseStatus.ERROR)
            {
                if (!string.IsNullOrEmpty(result.Item3) && !result.Item3.Contains("INTERNAL_SERVER_ERROR"))
                    err = result.Item3;
                else
                    err = "ChangeStatus error with empty description";
            }
            if ((result.Item2 != null) && 
                (result.Item2.Order != null) &&
                ((status == StatusYandex.CANCELLED ? result.Item2.Order.CancelRequested : true) ||
                 ((result.Item2.Order.Status == status) &&
                 (subStatus != SubStatusYandex.NotFound ? result.Item2.Order.Substatus == subStatus : true))))
            {
                return new(true, err);
            }
            return new(false, err);
        }
        public static async Task<(bool success, string error)> UpdateStock(IHttpService httpService, string proxyHost,
            string campaignId,
            string clientId,
            string authToken,
            string warehouseId,
            Dictionary<string, string> data,
            CancellationToken cancellationToken)
        {
            var request = new ResponseStocks();
            foreach (var item in data)
            {
                request.Skus.Add(new SkuEntry
                {
                    Sku = item.Key,
                    WarehouseId = warehouseId,
                    Items = new List<SkuItem> 
                    {  
                        new SkuItem 
                        { 
                            Type = ItemType.FIT,
                            Count = item.Value,
                            UpdatedAt = DateTime.Now,
                        } 
                    }
                });
            }
            var result = await Exchange<ErrorResponse>(httpService,
                $"https://{proxyHost}api.partner.market.yandex.ru/v2/campaigns/{campaignId}/offers/stocks.json",
                HttpMethod.Put,
                clientId,
                authToken,
                request,
                cancellationToken);
            string err = "";
            if (result.Item1 == ResponseStatus.ERROR)
            {
                if (!string.IsNullOrEmpty(result.Item3) && !result.Item3.Contains("INTERNAL_SERVER_ERROR"))
                    err = "Yandex update stock " + result.Item3;
                else
                    err = "Yandex update stock error with empty description";
            }
            return (success: result.Item1 == ResponseStatus.OK, error: err);
        }
    }
}
