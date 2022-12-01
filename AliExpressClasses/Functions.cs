using HttpExtensions;
using JsonExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AliExpressClasses
{
    public static class Functions
    {
        public static string ParseErrorGlobal(this string errorText, ErrorGlobal response)
        {
            if ((response != null) && (response.Error_response != null))
            {
                if (!string.IsNullOrEmpty(errorText))
                    errorText += Environment.NewLine;
                return errorText + response.Error_response.Code + ": " + response.Error_response.Msg;
            }
            return errorText;
        }
        public static string ParseError(this string errorText, Error response)
        {
            if (response != null)
            {
                if (!string.IsNullOrEmpty(errorText))
                    errorText += Environment.NewLine;
                return errorText + response.Code + ": " + response.Message;
            }
            return errorText;
        }
        public static Dictionary<string, string> GetGlobalHeaders(string appKey, string appSecret, string autorization, string method, string? key = null, object? json = null)
        {
            var result = new Dictionary<string, string>();
            result.Add("format", "json");
            result.Add("v", "2.0");
            result.Add("sign_method", "hmac"); //"md5");
            result.Add("method", method);
            result.Add("app_key", appKey);
            result.Add("session", appSecret);
            result.Add("timestamp", DateTime.UtcNow.AddHours(8).ToString("yyyy-MM-dd HH:mm:ss"));
            if ((json != null) && (!string.IsNullOrEmpty(key)))
                result.Add(key, json.SerializeObject());
            string sign = result.GetGlobalSign(autorization);
            result.Add("sign", sign);
            return result;
        }
        public static Dictionary<string, string> GetCustomHeaders(string authToken)
        {
            var result = new Dictionary<string, string>();
            result.Add("x-auth-token", authToken);
            return result;
        }
        private static string GetGlobalSign(this IDictionary<string, string> parameters, string secret)
        {
            IDictionary<string, string> sortedParams = new SortedDictionary<string, string>(parameters, StringComparer.Ordinal);
            IEnumerator<KeyValuePair<string, string>> dem = sortedParams.GetEnumerator();
            StringBuilder query = new StringBuilder();
            //query.Append(secret); //only for MD5
            while (dem.MoveNext())
            {
                string key = dem.Current.Key;
                string value = dem.Current.Value;
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                {
                    query.Append(key).Append(value);
                }
            }
            //if (!string.IsNullOrEmpty(body))
            //{
            //    query.Append(body);
            //}
            byte[] bytes;
            //query.Append(secret); //only for MD5
            //MD5 md5 = MD5.Create(); //only for MD5
            //bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(query.ToString())); //only for MD5
            HMACMD5 hmac = new HMACMD5(Encoding.UTF8.GetBytes(secret)); //only for HMAC
            bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(query.ToString())); //only for HMAC

            StringBuilder result = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                result.Append(bytes[i].ToString("X2"));
            }
            return result.ToString();
        }
        public static async Task<Tuple<List<CatalogInfo>?, string?>> GetCatalogInfo(IHttpService httpService, string authToken,
            string lastProductId,
            int limit,
            CancellationToken cancellationToken)
        {
            var request = new CatalogListRequest { Last_product_id = lastProductId, Limit = limit.ToString() };
            var result = await httpService.Exchange<CatalogListResponse, Error>(
                "https://openapi.aliexpress.ru/public/api/v1/scroll-short-product-by-filter",
                HttpMethod.Post,
                GetCustomHeaders(authToken),
                request,
                cancellationToken);
            string err = "";
            if (result.Item2 != null)
                err = ("AliGetCatalogInfoResponse : ").ParseError(result.Item2);
            if (result.Item1 != null)
            {
                if (result.Item1.Error != null)
                    err = err.ParseError(result.Item1.Error);
                return new(result.Item1.Data, err);
            }
            return new(null, err);
        }
        public static async Task<Tuple<ProductListResult?, string>> GetCatalogInfoGlobal(IHttpService httpService, string appKey, string appSecret, string authToken,
            int currentPage, int limit, 
            CancellationToken cancellationToken)
        {
            var method = "aliexpress.solution.product.list.get";
            var request = new CatalogListRequestGlobal { 
                Aeop_a_e_product_list_query = new Aeop_a_e_product_list_query 
                {
                    Product_status_type = AliProductStatusType.onSelling,
                    Page_size = limit,
                    Current_page = currentPage,
                } 
            };
            var result = await httpService.Exchange<CatalogListResponseGlobal, ErrorGlobal>(
                "https://api.taobao.com/router/rest",
                HttpMethod.Get,
                GetGlobalHeaders(appKey, appSecret, authToken, method, "aeop_a_e_product_list_query", request),
                cancellationToken);
            if (result.Item2 != null)
            {
                return new(null, "".ParseErrorGlobal(result.Item2));
            }
            else if ((result.Item1 != null) &&
                (result.Item1.Aliexpress_solution_product_list_get_response != null) &&
                (result.Item1.Aliexpress_solution_product_list_get_response.Result != null))
            {
                return new(result.Item1.Aliexpress_solution_product_list_get_response.Result, "");
            }
            return new(null, "GetCatalogInfoGlobal : empty result");
        }
        public static async Task<Tuple<List<long>?, string>> UpdatePriceGlobal(IHttpService httpService, string appKey, string appSecret, string authorization,
            List<PriceProductGlobal> priceData,
            CancellationToken cancellationToken)
        {
            var method = "aliexpress.solution.batch.product.price.update";
            //var parameters = GetGlobalHeaders(appKey, appSecret, authToken, method, "mutiple_product_update_list", priceData);
            var result = await httpService.Exchange<PriceGlobalResponse, ErrorGlobal>(
                "https://api.taobao.com/router/rest",
                HttpMethod.Post,
                GetGlobalHeaders(appKey, appSecret, authorization, method, "mutiple_product_update_list", priceData),
                cancellationToken);
            if (result.Item2 != null)
            {
                return new(null, "".ParseErrorGlobal(result.Item2));
            }
            else if (result.Item1 != null)
            {
                var tmpList = new List<long>();
                var err = "";
                if (result.Item1.Aliexpress_solution_batch_product_price_update_response != null)
                {
                    if ((result.Item1.Aliexpress_solution_batch_product_price_update_response.Update_successful_list != null) &&
                        (result.Item1.Aliexpress_solution_batch_product_price_update_response.Update_successful_list.Synchronize_product_response_dto != null) &&
                        (result.Item1.Aliexpress_solution_batch_product_price_update_response.Update_successful_list.Synchronize_product_response_dto.Count > 0))
                    {
                        tmpList = result.Item1.Aliexpress_solution_batch_product_price_update_response.Update_successful_list.Synchronize_product_response_dto.Select(x => x.Product_id).ToList();
                    }
                    if ((result.Item1.Aliexpress_solution_batch_product_price_update_response.Update_failed_list != null) &&
                        (result.Item1.Aliexpress_solution_batch_product_price_update_response.Update_failed_list.Synchronize_product_response_dto != null) &&
                        (result.Item1.Aliexpress_solution_batch_product_price_update_response.Update_failed_list.Synchronize_product_response_dto.Count > 0))
                    {
                        if (!string.IsNullOrEmpty(err))
                            err += Environment.NewLine;
                        err += string.Join(Environment.NewLine, result.Item1.Aliexpress_solution_batch_product_price_update_response.Update_failed_list.Synchronize_product_response_dto.Select(x => "ProductId : " + x.Product_id +
                            (string.IsNullOrEmpty(x.Error_code) ? "" : " | Code : " + x.Error_code) +
                            (string.IsNullOrEmpty(x.Error_message) ? "" : " | Message : " + x.Error_message)));
                    }
                }
                return new(tmpList, err);
            }
            return new(null, "");
        }
        public static async Task<(List<long>? updatedIds, List<long>? errorIds, string errorMessage)> UpdateStockGlobal(IHttpService httpService, string appKey, string appSecret, string authorization,
            List<StockProductGlobal> stockData,
            CancellationToken cancellationToken)
        {
            var method = "aliexpress.solution.batch.product.inventory.update";
            //var parameters = GetGlobalHeaders(appKey, appSecret, authToken, method, "mutiple_product_update_list", stockData);
            var result = await httpService.Exchange<StockGlobalResponse, ErrorGlobal>(
                "https://api.taobao.com/router/rest",
                HttpMethod.Post,
                GetGlobalHeaders(appKey, appSecret, authorization, method, "mutiple_product_update_list", stockData),
                cancellationToken);
            if (result.Item2 != null)
            {
                return (updatedIds: null, errorIds: null, errorMessage: "".ParseErrorGlobal(result.Item2));
            }
            else if (result.Item1 != null)
            {
                var tmpList = new List<long>();
                var errList = new List<long>();
                var err = "";
                if (result.Item1.Aliexpress_solution_batch_product_inventory_update_response != null)
                {
                    if ((result.Item1.Aliexpress_solution_batch_product_inventory_update_response.Update_successful_list != null) &&
                        (result.Item1.Aliexpress_solution_batch_product_inventory_update_response.Update_successful_list.Synchronize_product_response_dto != null) &&
                        (result.Item1.Aliexpress_solution_batch_product_inventory_update_response.Update_successful_list.Synchronize_product_response_dto.Count > 0))
                    {
                        tmpList = result.Item1.Aliexpress_solution_batch_product_inventory_update_response.Update_successful_list.Synchronize_product_response_dto.Select(x => x.Product_id).ToList();
                    }
                    if ((result.Item1.Aliexpress_solution_batch_product_inventory_update_response.Update_failed_list != null) &&
                        (result.Item1.Aliexpress_solution_batch_product_inventory_update_response.Update_failed_list.Synchronize_product_response_dto != null) &&
                        (result.Item1.Aliexpress_solution_batch_product_inventory_update_response.Update_failed_list.Synchronize_product_response_dto.Count > 0))
                    {
                        if (!string.IsNullOrEmpty(err))
                            err += Environment.NewLine;
                        err += string.Join(Environment.NewLine, result.Item1.Aliexpress_solution_batch_product_inventory_update_response.Update_failed_list.Synchronize_product_response_dto.Select(x => "ProductId : " + x.Product_id +
                            (string.IsNullOrEmpty(x.Error_code) ? "" : " | Code : " + x.Error_code) +
                            (string.IsNullOrEmpty(x.Error_message) ? "" : " | Message : " + x.Error_message)));
                        errList = result.Item1.Aliexpress_solution_batch_product_inventory_update_response.Update_failed_list.Synchronize_product_response_dto
                            .Select(x => x.Product_id).ToList();
                    }
                }
                return (updatedIds: tmpList, errorIds: errList, errorMessage: err);
            }
            return (null, null, "");
        }
        public static async Task<(List<string>? UpdatedIds, string ErrorMessage)> UpdatePrice(IHttpService httpService, string authToken,
            List<PriceProduct> priceData,
            CancellationToken cancellationToken)
        {
            var request = new PriceRequest();
            request.Products = priceData;
            var result = await httpService.Exchange<AliResponse, Error>(
                "https://openapi.aliexpress.ru/public/api/v1/product/update-sku-price",
                HttpMethod.Post,
                GetCustomHeaders(authToken),
                request,
                cancellationToken);
            if (result.Item2 != null)
            {
                return (UpdatedIds: null, ErrorMessage: "".ParseError(result.Item2));
            }
            else if (result.Item1 != null)
            {
                if ((result.Item1.Results != null) && (result.Item1.Results.Count > 0))
                {
                    var tmpList = new List<string>();
                    var err = "";
                    for (int i = 0; i < result.Item1.Results.Count; i++)
                    {
                        if (priceData.Count > i)
                        {
                            var item = result.Item1.Results[i];
                            //var skuRequested = (priceData[i]?.Skus ?? new List<PriceSku>()).Select(x => x.Sku_code).FirstOrDefault();
                            //if ((item.Errors != null) && (item.Errors.Count > 0))
                            //{
                            //    if (!string.IsNullOrEmpty(err))
                            //        err += Environment.NewLine;
                            //    err += item.Offer_id + " : " + string.Join(';', item.Errors.Select(x => x.Code + ": " + x.Message));
                            //}
                            if (item.Ok)
                            {
                                tmpList.Add(priceData[i]?.Product_id ?? "");
                            }
                        }
                    }
                    return (UpdatedIds: tmpList, ErrorMessage: err);
                }
                else
                    return (UpdatedIds: null, ErrorMessage: "Internal: Can't find Result in Ali price response");
            }
            else
            {
                return (UpdatedIds: null, ErrorMessage: "AliPriceResponse Internal: both sides are null");
            }
        }
        public static async Task<(List<string> UpdatedIds, List<string> ErrorIds, string ErrorMessage)> UpdateStock(IHttpService httpService, string authToken,
            List<Product> stockData,
            CancellationToken cancellationToken)
        {
            var request = new StockRequest();
            request.Products = stockData;
            var result = await httpService.Exchange<AliResponse, Error>(
                "https://openapi.aliexpress.ru/public/api/v1/product/update-sku-stock",
                HttpMethod.Post,
                GetCustomHeaders(authToken),
                request,
                cancellationToken);
            List<string> uploadIds = new List<string>();
            List<string> errorIds = new List<string>();
            string err = "";
            if (result.Item2 != null)
            {
                err = ("AliUpdateStockResponse : ").ParseError(result.Item2);
            }
            if (result.Item1 != null)
            {
                if ((result.Item1.Results != null) && (result.Item1.Results.Count > 0))
                {
                    for (int i = 0; i < result.Item1.Results.Count; i++)
                    {
                        if (stockData.Count > i)
                        {
                            var item = result.Item1.Results[i];
                            var skuRequested = (stockData[i]?.Skus ?? new List<StockSku>()).Select(x => x.Sku_code).FirstOrDefault();
                            if (item.Ok)
                            {
                                //uploadIds.Add(skuRequested ?? "");
                                uploadIds.Add(stockData[i]?.Product_id ?? "");
                            }
                            else if (item.Errors != null)
                            {
                                if (!string.IsNullOrEmpty(err))
                                    err += Environment.NewLine;
                                err += skuRequested ?? "" + " : " + item.Errors.Code + ": " + item.Errors.Message;
                                errorIds.Add(stockData[i]?.Product_id ?? "");
                            }
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(err))
                        err += Environment.NewLine;
                    err += "Internal: Can't find Result in AliExpress stock response";
                }
            }
            return (UpdatedIds: uploadIds, ErrorIds: errorIds, ErrorMessage: err);
        }
        public static async Task<(ResponseData? data, string error)> GetOrders(IHttpService httpService, string authToken,
            int currentPage, int limit,
            CancellationToken cancellationToken)
        {
            var request = new LocalOrdersRequest(currentPage, limit);
            request.Trade_order_info = TradeOrderInfo.LogisticInfo;
            var result = await httpService.Exchange<LocalOrderResponse, string>(
                "https://openapi.aliexpress.ru/seller-api/v1/order/get-order-list",
                HttpMethod.Post,
                GetCustomHeaders(authToken),
                request,
                cancellationToken);
            string err = "";
            if (!string.IsNullOrEmpty(result.Item2))
            {
                err += "AliGetOrders : " + result.Item2;
            }
            if (result.Item1?.Error != null)
                err = err.ParseError(result.Item1.Error);
            if (result.Item1?.Data != null)
                return (data: result.Item1.Data, error: string.IsNullOrEmpty(err) ? string.Empty : "AliGetOrders : " + err);
            return (null, error: string.IsNullOrEmpty(err) ? string.Empty : "AliGetOrders : " + err);
        }
        public static async Task<Tuple<GetOrderResult?, string>> GetOrdersGlobal(IHttpService httpService, string appKey, string appSecret, string authorization,
            int currentPage, int limit,
            CancellationToken cancellationToken)
        {
            var method = "aliexpress.solution.order.get";
            var request = new OrdersRequest
            {
                Page_size = limit,
                Current_page = currentPage,
                Create_date_start = DateTime.Today.AddDays(-30),
                Order_status_list = new List<OrderStatus> {
                    OrderStatus.PLACE_ORDER_SUCCESS,
                    OrderStatus.IN_CANCEL,
                    OrderStatus.WAIT_SELLER_SEND_GOODS,
                    OrderStatus.SELLER_PART_SEND_GOODS,
                    OrderStatus.WAIT_BUYER_ACCEPT_GOODS,
                    OrderStatus.FUND_PROCESSING,
                    OrderStatus.IN_ISSUE,
                    OrderStatus.IN_FROZEN,
                    OrderStatus.WAIT_SELLER_EXAMINE_MONEY,
                    OrderStatus.RISK_CONTROL,
                    OrderStatus.FINISH,
                }
            };
            var result = await httpService.Exchange<OrdersResponse, ErrorGlobal>(
                "https://api.taobao.com/router/rest",
                HttpMethod.Post,
                GetGlobalHeaders(appKey, appSecret, authorization, method, "param0", request),
                cancellationToken);
            if (result.Item2 != null)
            {
                return new(null, "".ParseErrorGlobal(result.Item2));
            }
            else if ((result.Item1 != null) && (result.Item1.Aliexpress_solution_order_get_response != null))
            {
                return new(result.Item1.Aliexpress_solution_order_get_response.Result, "");
            }
            return new(null, "GetOrdersGlobal : empty result");
        }
        public static async Task GetLogisticDetails(IHttpService httpService, string appKey, string appSecret, string authorization,
            CancellationToken cancellationToken)
        {
            var method = "aliexpress.logistics.redefining.getonlinelogisticsinfo";
            var result = await httpService.Exchange<string, ErrorGlobal>(
                "https://api.taobao.com/router/rest",
                HttpMethod.Post,
                GetGlobalHeaders(appKey, appSecret, authorization, method, "logistics_order_id", 7771084),
                cancellationToken);
        }
    }
}
