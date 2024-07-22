using HttpExtensions;
using JsonExtensions;

namespace OzonClasses
{
    public static class OzonOperators
    {
        public static string ParseOzonError(ErrorResponse response)
        {
            if (response != null)
            {
                string infoError = "Ozon " + response.Code + ": " + response.Message;
                foreach (var responseError in response.Details ?? Enumerable.Empty<Detail>())
                    infoError += ", " + responseError.TypeUrl ?? "" + " : " + responseError.Value ?? "";
                return infoError;
            }
            return "";
        }
        public static Dictionary<string,string> GetOzonHeaders(string clientId, string authToken)
        {
            var result = new Dictionary<string, string>();
            result.Add("Client-Id", clientId);
            result.Add("Api-Key", authToken);
            return result;
        }
        public static async Task<Tuple<List<FbsPosting>?,string?>> UnfulfilledOrders(IHttpService httpService, string proxyHost, string clientId, string authToken,
            long limit,
            CancellationToken cancellationToken)
        {
            var request = new OzonUnfulfilledOrderRequest();
            request.Dir = SortOrder.ASC;
            request.Filter = new UnfulfilledFilter
            {
                //Delivering_date_from = DateTime.Today.AddDays(-30),
                //Delivering_date_to = DateTime.Today.AddDays(30),

                Cutoff_from = DateTime.Today.AddDays(-60),
                Cutoff_to = DateTime.Today.AddDays(60),
                Status = OrderStatus.awaiting_packaging //awaiting_deliver //
            };
            request.Limit = limit;
            request.Offset = 0;
            var headers = GetOzonHeaders(clientId, authToken);
            var result = await httpService.Exchange<OzonUnfulfilledOrderResponse, ErrorResponse>(
                $"https://{proxyHost}api-seller.ozon.ru/v3/posting/fbs/unfulfilled/list",
                HttpMethod.Post,
                headers,
                request,
                cancellationToken);
            string err = "";
            if (result.Item2 != null)
            {
                err = "OzonUnfulfilledResponse : " + result.Item2;
                //return new(null, "OzonUnfulfilledResponse : " + result.Item2);
            }
            List<FbsPosting> data = new List<FbsPosting>();
            if ((result.Item1 != null) && (result.Item1.Result != null) && (result.Item1.Result.Count > 0) &&
                (result.Item1.Result.Postings != null) && (result.Item1.Result.Postings.Count > 0))
            {
                data.AddRange(result.Item1.Result.Postings);
                //return new(result.Item1.Result.Postings, null);
            }
            //second check
            request.Filter.Status = OrderStatus.awaiting_deliver;
            result = await httpService.Exchange<OzonUnfulfilledOrderResponse, ErrorResponse>(
                $"https://{proxyHost}api-seller.ozon.ru/v3/posting/fbs/unfulfilled/list",
                HttpMethod.Post,
                headers,
                request,
                cancellationToken);
            if (result.Item2 != null)
            {
                if (!string.IsNullOrEmpty(err))
                    err += Environment.NewLine + result.Item2;
                else
                    err = "OzonUnfulfilledResponse : " + result.Item2;
                //return new(null, "OzonUnfulfilledResponse : " + result.Item2);
            }
            if ((result.Item1 != null) && (result.Item1.Result != null) && (result.Item1.Result.Count > 0) &&
                (result.Item1.Result.Postings != null) && (result.Item1.Result.Postings.Count > 0))
            {
                data.AddRange(result.Item1.Result.Postings);
                //return new(result.Item1.Result.Postings, null);
            }
            return new(data.Count > 0 ? data : null, string.IsNullOrEmpty(err) ? null : err);
        }
        public static async Task<Tuple<List<FbsPosting>?,bool?, string?>> DetailOrders(IHttpService httpService, string proxyHost, string clientId, string authToken,
            OrderStatus? status,
            int checkDays,
            long limit,
            long offset,
            CancellationToken cancellationToken)
        {
            var request = new DetailOrderRequest();
            request.Dir = SortOrder.ASC;
            request.Filter = new DetailFilter
            {
                Since = DateTime.Today.AddDays(-checkDays),
                To = DateTime.Today.AddDays(checkDays),
                Status = status,
                //Order_id = 23849303191
            };
            request.Limit = limit;
            request.Offset = offset;
            var result = await httpService.Exchange<DetailOrderResponse, ErrorResponse>(
                $"https://{proxyHost}api-seller.ozon.ru/v3/posting/fbs/list",
                HttpMethod.Post,
                GetOzonHeaders(clientId, authToken),
                request,
                cancellationToken);
            if (result.Item2 != null)
            {
                return new(null,null, "DetailOrderResponse : " + result.Item2);
            }
            if ((result.Item1 != null) && (result.Item1.Result != null) && 
                (result.Item1.Result.Postings != null) && (result.Item1.Result.Postings.Count > 0))
            {
                return new(result.Item1.Result.Postings,result.Item1.Result.Has_next, null);
            }
            return new(null,null, null);
        }
        public static async Task<Tuple<OrderStatus?,string?, PostingBarcodes?>> OrderDetails(IHttpService httpService, string proxyHost, string clientId, string authToken,
            string postingNumber,
            CancellationToken cancellationToken)
        {
            var request = new GetOrderByPostingNumberRequest();
            request.Posting_number = postingNumber;
            request.With = new RequestWithParams { Analytics_data = false, Barcodes = true, Financial_data = false, Translit = false };
            var result = await httpService.Exchange<GetOrderByPostingNumberResponse, ErrorResponse>(
                $"https://{proxyHost}api-seller.ozon.ru/v3/posting/fbs/get",
                HttpMethod.Post,
                GetOzonHeaders(clientId, authToken),
                request,
                cancellationToken);
            if (result.Item2 != null)
            {
                return new(null, "GetOrderByPostingNumberResponse : " + result.Item2, null);
            }
            if ((result.Item1 != null) && (result.Item1.Result != null))
            {
                return new(result.Item1.Result.Status, null, result.Item1.Result.Barcodes);
            }
            return new(null, null, null);
        }
        public static async Task<string> CancelOrder(IHttpService httpService, string proxyHost, string clientId, string authToken,
            string postingNumber,
            long reasonId,
            string reasonMessage,
            List<CancelItem> cancelItems,
            CancellationToken cancellationToken)
        {
            var request = new CancelOrderRequest();
            request.Cancel_reason_id = reasonId; 
            request.Cancel_reason_message = reasonMessage;
            request.Posting_number = postingNumber;
            request.Items = cancelItems;
            var response = await httpService.Exchange<CancelOrderResponse, ErrorResponse>(
                $"https://{proxyHost}api-seller.ozon.ru/v2/posting/fbs/cancel",
                HttpMethod.Post,
                GetOzonHeaders(clientId, authToken),
                request,
                cancellationToken);
            if (response.Item2 != null)
            {
                return ParseOzonError(response.Item2);
            }
            else if (response.Item1 != null)
            {
                if (!response.Item1.Result)
                    return "Internal: Result = false in Ozon cancel response";
                else
                    return "";
            }
            else
            {
                return "Internal: empty cancel response";
            }
        }
        public static async Task<Tuple<bool?,string?>> AddToDelivery(IHttpService httpService, string clientId, string authToken,
            string postingNumber,
            CancellationToken cancellationToken)
        {
            var request = new AddToDeliveryRequest { Posting_number = new List<string> { postingNumber } };
            var result = await httpService.Exchange<AddToDeliveryResponse, ErrorResponse>(
                "https://api-seller.ozon.ru/v2/posting/fbs/awaiting-delivery",
                HttpMethod.Post,
                GetOzonHeaders(clientId, authToken),
                request,
                cancellationToken);
            if (result.Item2 != null)
            {
                return new(null, "AddToDeliveryResponse : " + ParseOzonError(result.Item2));
            }
            if (result.Item1 != null)
                return new(result.Item1.Result, null);
            return new(null,null);
        }
        public static async Task<Tuple<string?,string?>> GetCountryCode(IHttpService httpService, string proxyHost, string clientId, string authToken,
            string countryName,
            CancellationToken cancellationToken)
        {
            var request = new CountryCodeRequest { Name_search = countryName };
            var result = await httpService.Exchange<CountryCodeResponse, ErrorResponse>(
                $"https://{proxyHost}api-seller.ozon.ru/v2/posting/fbs/product/country/list",
                HttpMethod.Post,
                GetOzonHeaders(clientId, authToken),
                request,
                cancellationToken);
            string err = "";
            if (result.Item2 != null)
            {
                err += "CountryCodeResponse : " + ParseOzonError(result.Item2);
            }
            if ((result.Item1 != null) && (result.Item1.Result != null) && (result.Item1.Result.Count > 0))
            {
                return new(result.Item1.Result.Select(x => x.Country_iso_code).FirstOrDefault(),err);
            }
            return new(null,err);
        }
        public static async Task<Tuple<bool?,string>> SetCountryCode(IHttpService httpService, string proxyHost, string clientId, string authToken,
            string postingNumber,
            long productId,
            string countryCode,
            CancellationToken cancellationToken)
        {
            var request = new SetCountryRequest();
            request.Posting_number = postingNumber;
            request.Product_id = productId;
            request.Country_iso_code = countryCode;
            var result = await httpService.Exchange<SetCountryResponse, ErrorResponse>(
                $"https://{proxyHost}api-seller.ozon.ru/v2/posting/fbs/product/country/set",
                HttpMethod.Post,
                GetOzonHeaders(clientId, authToken),
                request,
                cancellationToken);
            string err = "";
            if (result.Item2 != null)
            {
                err += "SetCountryResponse : " + ParseOzonError(result.Item2);
            }
            if ((result.Item1 != null) && (result.Item1.Product_id == productId))
            {
                return new(result.Item1.Is_gtd_needed,err);
            }
            return new(null,err);
        }
        public static async Task<(Dictionary<long, ProductExemplarCreateOrGetItem>? data, string error)> CreateOrGetExemplar(IHttpService httpService, string proxyHost, string clientId, string authToken,
            string postingNumber,
            CancellationToken cancellationToken
        )
        {
            var request = new ExemplarCreateOrGetRequest();
            request.Posting_number = postingNumber;

            var result = await httpService.Exchange<ExemplarCreateOrGetResponse, ErrorResponse>(
                $"https://{proxyHost}api-seller.ozon.ru/v5/fbs/posting/product/exemplar/create-or-get",
                HttpMethod.Post,
                GetOzonHeaders(clientId, authToken),
                request,
                cancellationToken);

            string err = "";
            if (result.Item2 != null)
            {
                if (!string.IsNullOrEmpty(err))
                    err += Environment.NewLine;
                err += "ExemplarCreateOrGetResponse : (" + postingNumber + ") " + ParseOzonError(result.Item2);
            }
            if (result.Item1 != null)
            {
                return (data: result.Item1.Products.ToDictionary(k => k.Product_id), error: err);
            }
            else
            {
                if (!string.IsNullOrEmpty(err))
                    err += Environment.NewLine;
                err += "ExemplarCreateOrGetResponse : Empty response";
            }
            return (data: null, error: err);
        }
        public static async Task<(Dictionary<long, List<ExemplarStatusItem>>? data, string error)> GetExemplar(IHttpService httpService, string proxyHost, string clientId, string authToken,
            string postingNumber,
            CancellationToken cancellationToken
        )
        {
            var request = new ExemplarStatusRequest();
            request.Posting_number = postingNumber;

            var result = await httpService.Exchange<ExemplarStatusResponse, ErrorResponse>(
                $"https://{proxyHost}api-seller.ozon.ru/v4/fbs/posting/product/exemplar/status",
                HttpMethod.Post,
                GetOzonHeaders(clientId, authToken),
                request,
                cancellationToken);

            string err = "";
            if (result.Item2 != null)
            {
                if (!string.IsNullOrEmpty(err))
                    err += Environment.NewLine;
                err += "GetExemplarResponse : (" + postingNumber + ") " + ParseOzonError(result.Item2);
            }
            if (result.Item1 != null)
            {
                return (data: result.Item1.Products.ToDictionary(k => k.Product_id, v => v.Exemplars), error: err);
            }
            else
            {
                if (!string.IsNullOrEmpty(err))
                    err += Environment.NewLine;
                err += "GetExemplarResponse : Empty response";
            }
            return (data: null, error: err);
        }
        public static async Task<(bool? result, string error)> SetExemplar(IHttpService httpService, string proxyHost, string clientId, string authToken,
            string postingNumber,
            int multiBoxQty,
            List<ProductExemplarRequest> requestProducts,
            CancellationToken cancellationToken
        )
        {
            var request = new ExemplarSetRequest();
            request.Posting_number = postingNumber;
            request.Multi_box_qty = multiBoxQty;
            request.Products = requestProducts;

            var result = await httpService.Exchange<ExemplarSetResponse, ErrorResponse>(
                $"https://{proxyHost}api-seller.ozon.ru/v5/fbs/posting/product/exemplar/set",
                HttpMethod.Post,
                GetOzonHeaders(clientId, authToken),
                request,
                cancellationToken);

            string err = "";
            if (result.Item2 != null)
            {
                if (!string.IsNullOrEmpty(err))
                    err += Environment.NewLine;
                err += "SetExemplarResponse : (" + postingNumber + ") " + ParseOzonError(result.Item2);
            }
            if (result.Item1 != null)
            {
                return (result: result.Item1.Result, error: err);
            }
            else
            {
                if (!string.IsNullOrEmpty(err))
                    err += Environment.NewLine;
                err += "SetExemplarResponse : No result in response";
            }
            return (result: null, error: err);
        }
        public static async Task<Tuple<List<PostingAdditionalData>?,string>> SetOrderPosting(IHttpService httpService, string proxyHost, string clientId, string authToken,
            string postingNumber,
            List<PostingPackage> packages,
            CancellationToken cancellationToken)
        {
            var request = new PostingOrderRequest();
            request.Posting_number = postingNumber;
            request.With = new PostingWith { additional_data = true };
            request.Packages = packages;
            var result = await httpService.Exchange<PostingOrderResponse, ErrorResponse>(
                $"https://{proxyHost}api-seller.ozon.ru/v4/posting/fbs/ship",
                HttpMethod.Post,
                GetOzonHeaders(clientId, authToken),
                request,
                cancellationToken);
            string err = "";
            if (result.Item2 != null)
            {
                if (!string.IsNullOrEmpty(err))
                    err += Environment.NewLine;
                err += "PostingOrderResponse : (" + postingNumber + ") " + ParseOzonError(result.Item2);
            }
            if ((result.Item1 != null) && (result.Item1.Additional_data != null) && (result.Item1.Additional_data.Count > 0))
            {
                return new(result.Item1.Additional_data,err);
            }
            else
            {
                if (!string.IsNullOrEmpty(err))
                    err += Environment.NewLine;
                err += "PostingOrderResponse : No additional_data in response";
            }
            return new(null,err);
        }
        public static async Task<Tuple<ProductListResult?,string>> ParseCatalog(IHttpService httpService, string proxyHost, string clientId, string authToken,
            string? nextPageToken,
            long limit,
            CancellationToken cancellationToken)
        {
            var request = new ProductListRequest {
                //Filter = new ProductFilter { Offer_id = new List<string> { "443030303533313935" }, Visibility = RequestFilterVisibility.ALL },
                Last_id = string.IsNullOrEmpty(nextPageToken) ? "" : nextPageToken, Limit = limit };
            var result = await httpService.Exchange<ProductListResponse, ErrorResponse>(
                $"https://{proxyHost}api-seller.ozon.ru/v2/product/list",
                HttpMethod.Post,
                GetOzonHeaders(clientId, authToken),
                request,
                cancellationToken);
            string err = "";
            if (result.Item2 != null)
            {
                if (!string.IsNullOrEmpty(err))
                    err += Environment.NewLine;
                err += ParseOzonError(result.Item2);
            }
            return new(((result.Item1 != null) && (result.Item1.Result != null)) ? result.Item1.Result : null, err);
        }
        public static async Task<Tuple<List<string>?, string>> UpdatePrice(IHttpService httpService, string proxyHost, string clientId, string authToken,
            List<PriceRequest> priceData,
            CancellationToken cancellationToken)
        {
            var request = new OzonPriceRequest();
            request.Prices = priceData;
            var result = await httpService.Exchange<OzonPriceResponse, ErrorResponse>(
                $"https://{proxyHost}api-seller.ozon.ru/v1/product/import/prices",
                HttpMethod.Post,
                GetOzonHeaders(clientId, authToken),
                request,
                cancellationToken);
            if (result.Item2 != null)
            {
                return new(null, ParseOzonError(result.Item2));
            }
            else if (result.Item1 != null)
            {
                if ((result.Item1.Result != null) && (result.Item1.Result.Count > 0))
                {
                    var tmpList = new List<string>();
                    var err = "";
                    foreach (var item in result.Item1.Result)
                    {
                        if ((item.Errors != null) && (item.Errors.Count > 0))
                        {
                            if (!string.IsNullOrEmpty(err))
                                err += Environment.NewLine;
                            err += item.Offer_id + " : " + string.Join(';', item.Errors.Select(x => x.Code + ": " + x.Message));
                        }
                        if (item.Updated)
                        {
                            tmpList.Add(item.Offer_id ?? "");
                        }
                    }
                    return new(tmpList, err);
                }
                else
                    return new (null, "Internal: Can't find Result in Ozon price response");
            }
            else
            {
                return new(null, "Internal: both sides are null");
            }

        }
        static async Task<List<(string OfferId, int Present, int Reserved)>> GetStockInfo(IHttpService httpService, string proxyHost, string clientId, string authToken,
            List<string> offerIds,
            CancellationToken cancellationToken)
        {
            var result = new List<(string OfferId, int Present, int Reserved)>();
            var request = new StockInfoRequest(offerIds);
            var response = await httpService.Exchange<StockInfoResponse, ErrorResponse>(
                $"https://{proxyHost}api-seller.ozon.ru/v3/product/info/stocks",
                HttpMethod.Post,
                GetOzonHeaders(clientId, authToken),
                request,
                cancellationToken);
            string err = "";
            if (response.Item2 != null)
            {
                if (!string.IsNullOrEmpty(err))
                    err += Environment.NewLine;
                err += ParseOzonError(response.Item2);
            }
            if (response.Item1?.Result?.Items?.Count > 0)
                foreach (var item in response.Item1.Result.Items)
                {
                    if (item.Stocks?.Count > 0)
                        result.Add((
                            OfferId: item.Offer_id ?? "",
                            Present: item.Stocks.Where(x => x.Type == StockType.fbs).Sum(x => x.Present),
                            Reserved: item.Stocks.Where(x => x.Type == StockType.fbs).Sum(x => x.Reserved)
                        ));
                }
            return result;
        }
        public static async Task<(List<string> updatedOfferIds, List<string> tooManyRequests, List<string> errorOfferIds, string errorMessage)> UpdateStock(IHttpService httpService, string proxyHost, string clientId, string authToken,
            List<StockRequest> stockData,
            CancellationToken cancellationToken)
        {
            var stockInfo = await GetStockInfo(httpService, proxyHost, clientId, authToken, stockData.Select(x => x.Offer_id ?? "").ToList(), cancellationToken);
            foreach (var data in stockData)
            {
                var info = stockInfo.FirstOrDefault(x => x.OfferId == data.Offer_id);
                if (info != default)
                    data.Stock = Math.Max(data.Stock - info.Reserved, 0);
            }
            var request = new OzonStockRequest();
            request.Stocks = stockData;
            var result = await httpService.Exchange<OzonStockResponse, ErrorResponse>(
                $"https://{proxyHost}api-seller.ozon.ru/v2/products/stocks", 
                HttpMethod.Post,
                GetOzonHeaders(clientId, authToken),
                request,
                cancellationToken);
            List<string> uploadIds = new List<string>();
            List<string> tooManyIds = new List<string>();
            List<string> errorIds = new List<string>();
            string err = "";
            if (result.Item2 != null)
            {
                if (!string.IsNullOrEmpty(err))
                    err += Environment.NewLine;
                err += ParseOzonError(result.Item2);
            }
            if (result.Item1 != null)
            {
                if ((result.Item1.Result != null) && (result.Item1.Result.Count > 0))
                {
                    foreach (var item in result.Item1.Result)
                    {
                        if ((item.Errors != null) && (item.Errors.Count > 0))
                        {
                            if (!string.IsNullOrEmpty(err))
                                err += Environment.NewLine;
                            err += "ClientId = " + clientId + " (" + item.Offer_id + ") : " + string.Join(';', item.Errors.Select(x => x.Code + ": " + x.Message));
                            if ((!item.Updated) && (!item.Errors.Any(x => x.Code == "TOO_MANY_REQUESTS")))
                                errorIds.Add(item.Offer_id ?? "");
                            if (!item.Updated && item.Errors.Any(x => x.Code == "TOO_MANY_REQUESTS"))
                                tooManyIds.Add(item.Offer_id ?? "");
                        }
                        if (item.Updated)
                        {
                            uploadIds.Add(item.Offer_id ?? "");
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(err))
                        err += Environment.NewLine;
                    err += "Internal: Can't find Result in Ozon stock response";
                }
            }
            return (updatedOfferIds: uploadIds, tooManyRequests: tooManyIds, errorOfferIds: errorIds, errorMessage: err);
        }
        public static async Task<Tuple<byte[]?,string?>> GetLabels(IHttpService httpService, string proxyHost, string clientId, string authToken,
            List<string> postingNumbers,
            CancellationToken cancellationToken)
        {
            var request = new PackageLabelRequest();
            request.Posting_number = postingNumbers;
            var result = await httpService.Exchange<byte[], ErrorResponse>(
                $"https://{proxyHost}api-seller.ozon.ru/v2/posting/fbs/package-label",
                HttpMethod.Post,
                GetOzonHeaders(clientId, authToken),
                request,
                cancellationToken);
            if (result.Item2 != null)
                return new(null, ParseOzonError(result.Item2));
            return new(result.Item1, null);
        }
        public static async Task<(double ComPercent, double ComAmount, double VolumeWeight, string Price, string? Error)> ProductComission(IHttpService httpService, 
            string proxyHost, string clientId, string authToken,
            string offerId,
            string searchTag,
            CancellationToken cancellationToken)
        {
            var request = new ProductInfoRequest { Offer_id = offerId };
            var result = await httpService.Exchange<ProductInfoResponse, ErrorResponse>(
                $"https://{proxyHost}api-seller.ozon.ru/v2/product/info",
                HttpMethod.Post,
                GetOzonHeaders(clientId, authToken),
                request,
                cancellationToken);
            if (result.Item2 != null)
                return (ComPercent: 0, ComAmount: 0, VolumeWeight: 0, Price: "", Error: "ProductInfoResponse : (" + offerId + ") " + ParseOzonError(result.Item2));
            if ((result.Item1 != null) && (result.Item1.Result != null) && 
                (result.Item1.Result.Commissions != null))
            {
                var comData = result.Item1.Result.Commissions
                    .Where(x => x.SaleSchema?.ToLower().Trim() == searchTag)
                    .Select(x => new { x.Percent, x.Value })
                    .FirstOrDefault();
                return (ComPercent: comData?.Percent ?? 0, ComAmount: comData?.Value ?? 0, VolumeWeight: result.Item1.Result.Volume_weight, Price: result.Item1.Result.Price ?? "", Error: null);
            }
            return (ComPercent: 0, ComAmount: 0, VolumeWeight: 0, Price: "", Error: null);
        }
        public static async Task<Tuple<List<string>?,string?>> ProductNotReady(IHttpService httpService, string proxyHost, string clientId, string authToken,
            List<string> offers,
            CancellationToken cancellationToken)
        {
            var request = new ProductInfoListRequest { Offer_id = offers };
            var result = await httpService.Exchange<ProductInfoListResponse, ErrorResponse>(
                $"https://{proxyHost}api-seller.ozon.ru/v2/product/info/list",
                HttpMethod.Post,
                GetOzonHeaders(clientId, authToken),
                request,
                cancellationToken);
            if (result.Item2 != null)
                return new(null, "ProductInfoListResponse : " + ParseOzonError(result.Item2));
            if ((result.Item1 != null) && (result.Item1.Result != null) && (result.Item1.Result.Items != null))
            {
                var items = result.Item1.Result.Items
                    .Where(x => (x.Status == null) || (x.Status.Is_failed) || ((x.Status.Item_errors != null) && (x.Status.Item_errors.Count > 0) && (x.Status.Item_errors.Any(e => e.Level != "warning"))))
                    .Select(x => x.Offer_id == null ? "" : x.Offer_id)
                    .ToList();
                return new(items,null);
            }
            return new(null,null);
        }
        public static async Task<(byte[]? data,string? error)> GetAct(IHttpService httpService, string clientId, string authToken,
            TimeSpan limitTime,
            long? deliveryMethodId,
            CancellationToken cancellationToken)
        {
            var requestActCreate = new ActCreateRequest();
            DateTime departureDate = limitTime <= DateTime.Now.TimeOfDay ? DateTime.Today.AddDays(2).AddTicks(-1) : DateTime.Today.AddDays(1).AddTicks(-1);
            if (departureDate.DayOfWeek == DayOfWeek.Saturday)
                departureDate = departureDate.AddDays(2);
            if (departureDate.DayOfWeek == DayOfWeek.Sunday)
                departureDate = departureDate.AddDays(1);
            requestActCreate.Departure_date = departureDate;
            requestActCreate.Delivery_method_id = deliveryMethodId;
            var result = await httpService.Exchange<ActCreateResponse, ErrorResponse>(
                "https://api-seller.ozon.ru/v2/posting/fbs/act/create",
                HttpMethod.Post,
                GetOzonHeaders(clientId, authToken),
                requestActCreate,
                cancellationToken);
            if (result.Item2 != null)
                return (data: null, error: "ActCreateResponse : " + ParseOzonError(result.Item2));
            long taskId = 0;
            if ((result.Item1 != null) && (result.Item1.Result != null))
                taskId = result.Item1.Result.Id;
            if (taskId > 0)
            {
                var request = new ActCheckGetRequest { Id = taskId };
                ActStatus status = ActStatus.NotFound;
                int tryCount = 5;
                TimeSpan sleepPeriod = TimeSpan.FromSeconds(1);
                while (true)
                {
                    var checkResult = await httpService.Exchange<ActCheckStatusResult, ErrorResponse>(
                    "https://api-seller.ozon.ru/v2/posting/fbs/digital/act/check-status",
                    HttpMethod.Post,
                    GetOzonHeaders(clientId, authToken),
                    request,
                    cancellationToken);
                    if (checkResult.Item2 != null)
                        return (data: null, error: "ActCheckStatusResponse : " + ParseOzonError(checkResult.Item2));
                    if (checkResult.Item1 != null)
                    {
                        status = checkResult.Item1.Status;
                        if ((status == ActStatus.FORMED) || (status == ActStatus.CONFIRMED) || (status == ActStatus.CONFIRMED_WITH_MISMATCH))
                        {
                            break;
                        }
                        else
                        {
                            if (--tryCount == 0)
                                break;
                            await Task.Delay(sleepPeriod);
                        }
                    }
                }
                if ((status == ActStatus.FORMED) || (status == ActStatus.CONFIRMED) || (status == ActStatus.CONFIRMED_WITH_MISMATCH))
                {
                    request.Doc_type = DocType.act_of_acceptance;
                    var getActResult = await httpService.Exchange<byte[], ErrorResponse>(
                    "https://api-seller.ozon.ru/v2/posting/fbs/digital/act/get-pdf",
                    HttpMethod.Post,
                    GetOzonHeaders(clientId, authToken),
                    request,
                    cancellationToken);
                    if (getActResult.Item2 != null)
                        return (data: null, error: "ActGetResponse : " + ParseOzonError(getActResult.Item2));
                    return (data: getActResult.Item1, error: null);
                }
                else
                    return (data: null, error: "CheckActStatus : no more retries");
            }
            return new(null, null);
        }
        public static async Task<(List<ReturnItem>? returns, long count, string? error)> ReturnOrders(IHttpService httpService, string proxyHost, string clientId, string authToken,
            long limit,
            long offset,
            CancellationToken cancellationToken)
        {
            ReturnsRequest request;
            if (limit == 0)
                request = new ReturnsRequest();
            else
                request = new ReturnsRequest(limit);
            request.Last_id = offset;
            var result = await httpService.Exchange<ReturnsResponse, ErrorResponse>(
                $"https://{proxyHost}api-seller.ozon.ru/v3/returns/company/fbs",
                HttpMethod.Post,
                GetOzonHeaders(clientId, authToken),
                request,
                cancellationToken);
            if (result.Item2 != null)
                return (returns: null, count: 0, error: "ReturnsResponse : " + result.Item2.Message + " Details: " + string.Join(',', result.Item2.Details.Select(x => x.Value)));
            if (result.Item1?.Returns != null)
                return (returns: result.Item1.Returns, count: result.Item1.Last_id, error: null);
            return (returns: null, count: 0, error: null);
        }
    }
}
