using HttpExtensions;
using JsonExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using Top.Api;
using Top.Api.Request;
using Top.Api.Response;
//using Top.Api;
//using Top.Api.Request;
//using Top.Api.Response;

namespace HelloWorld
{
    class Program
    {
        static string GetResponse(string auth, string body)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    string url = @"https://localhost:44388/RegisterShipment";
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Headers.Add("User-Agent", "HttpTestClient");
                    request.Headers.Add("Authorization", auth);
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                    var response =client.Send(request);
                    if (response.Content != null)
                    {
                        Console.WriteLine(response.Content.ReadAsStringAsync());
                        return "1"; //response.Content.re.ReadAsStringAsync();
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        static string GetSign(IDictionary<string, string> parameters, string body, string secret)
        {
            IDictionary<string, string> sortedParams = new SortedDictionary<string, string>(parameters, StringComparer.Ordinal);
            IEnumerator<KeyValuePair<string, string>> dem = sortedParams.GetEnumerator();
            StringBuilder query = new StringBuilder();
            query.Append(secret);
            while (dem.MoveNext())
            {
                string key = dem.Current.Key;
                string value = dem.Current.Value;
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                {
                    query.Append(key).Append(value);
                }
            }
            if (!string.IsNullOrEmpty(body))
            {
                query.Append(body);
            }
            byte[] bytes;
            query.Append(secret);
            MD5 md5 = MD5.Create();
            bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(query.ToString()));
            //HMACMD5 hmac = new HMACMD5(Encoding.UTF8.GetBytes(secret));
            //bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(query.ToString()));

            StringBuilder result = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                result.Append(bytes[i].ToString("X2"));
            }
            return result.ToString();
        }
        static async Task Main(string[] args)
        {
            var url = "http://gw.api.taobao.com/router/rest"; //"https://api.taobao.com/router/rest";
            var secret = "c8174c131d123a878b4ccfddb9a72a88";
            var app_key = "33887460";
            var format = "json";
            var method = "aliexpress.solution.batch.product.price.update";
            var session = "50002700634rpATPerFloNwNufg4jVCmyeVudeDbq8KtwC1138f31bhlPLDzgpT95e1s";
            var timestamp = DateTime.UtcNow.AddHours(8).ToString("yyyy-MM-dd HH:mm:ss"); //"2022-07-01 09:33:00";
            var v = "2.0";
            var sign_method = "md5";//"hmac"; //

            var dataJson = Enumerable.Repeat(new 
            {
                product_id = 1005004177746198L,
                multiple_sku_update_list = Enumerable.Repeat(new 
                {
                    price = "4239.00",
                    discount_price = "4239.00",
                    sku_code = "D00040385"
                }, 1).ToList()
            }, 1).ToList();

            string mutiple_product_update_list = dataJson.SerializeObject();
            //data.Add(new
            //{
            //    product_id = 1005004177746198L,
            //    multiple_sku_update_list = new 
            //    { 
            //        price = "",
            //        discount_price = "",
            //        sku_code = ""
            //    }
            //})

            //string multiple_product_update_list = "[{\"multiple_sku_update_list\":[{\"price\":\"4238.0\",\"discount_price\":\"4237.0\",\"sku_code\":\"D00040385\"}],\"product_id\":1005004177746198}]";

            //System.IO.File.ReadAllText(@"f:\\tmp\15\body.txt", Encoding.UTF8);

            ITopClient client = new DefaultTopClient(url, app_key, secret);
            AliexpressSolutionOrderGetRequest req = new AliexpressSolutionOrderGetRequest();
            AliexpressSolutionOrderGetRequest.OrderQueryDomain obj1 = new AliexpressSolutionOrderGetRequest.OrderQueryDomain();
            obj1.CreateDateEnd = "2017-10-12 12:12:12";
            obj1.CreateDateStart = "2017-10-12 12:12:12";
            obj1.ModifiedDateStart = "2017-10-12 12:12:12";
            //obj1.OrderStatusList = "SELLER_PART_SEND_GOODS";
            obj1.BuyerLoginId = "test";
            obj1.PageSize = 20L;
            obj1.ModifiedDateEnd = "2017-10-12 12:12:12";
            obj1.CurrentPage = 1L;
            obj1.OrderStatus = "SELLER_PART_SEND_GOODS";
            req.Param0_ = obj1;
            AliexpressSolutionOrderGetResponse rsp = client.Execute(req, session);
            Console.WriteLine(rsp.Body);
            //ITopClient client = new DefaultTopClient(url, app_key, secret);
            //AliexpressSolutionBatchProductPriceUpdateRequest req = new AliexpressSolutionBatchProductPriceUpdateRequest();
            //List<AliexpressSolutionBatchProductPriceUpdateRequest.SynchronizeProductRequestDtoDomain> list2 = new List<AliexpressSolutionBatchProductPriceUpdateRequest.SynchronizeProductRequestDtoDomain>();
            //AliexpressSolutionBatchProductPriceUpdateRequest.SynchronizeProductRequestDtoDomain obj3 = new AliexpressSolutionBatchProductPriceUpdateRequest.SynchronizeProductRequestDtoDomain();
            //list2.Add(obj3);
            //obj3.ProductId = 1000005237852L;
            //List<AliexpressSolutionBatchProductPriceUpdateRequest.SynchronizeSkuRequestDtoDomain> list5 = new List<AliexpressSolutionBatchProductPriceUpdateRequest.SynchronizeSkuRequestDtoDomain>();
            //AliexpressSolutionBatchProductPriceUpdateRequest.SynchronizeSkuRequestDtoDomain obj6 = new AliexpressSolutionBatchProductPriceUpdateRequest.SynchronizeSkuRequestDtoDomain();
            //list5.Add(obj6);
            //obj6.Price = "19.99";
            //obj6.DiscountPrice = "14.99";
            //obj6.SkuCode = "123abc";
            //obj3.MultipleSkuUpdateList = list5;
            //AliexpressSolutionBatchProductPriceUpdateRequest.MultiCountryPriceConfigurationDtoDomain obj7 = new AliexpressSolutionBatchProductPriceUpdateRequest.MultiCountryPriceConfigurationDtoDomain();
            //obj7.PriceType = "absolute";
            //List<AliexpressSolutionBatchProductPriceUpdateRequest.SingleCountryPriceDtoDomain> list9 = new List<AliexpressSolutionBatchProductPriceUpdateRequest.SingleCountryPriceDtoDomain>();
            //AliexpressSolutionBatchProductPriceUpdateRequest.SingleCountryPriceDtoDomain obj10 = new AliexpressSolutionBatchProductPriceUpdateRequest.SingleCountryPriceDtoDomain();
            //list9.Add(obj10);
            //obj10.ShipToCountry = "FR";
            //List<AliexpressSolutionBatchProductPriceUpdateRequest.SingleSkuPriceByCountryDtoDomain> list12 = new List<AliexpressSolutionBatchProductPriceUpdateRequest.SingleSkuPriceByCountryDtoDomain>();
            //AliexpressSolutionBatchProductPriceUpdateRequest.SingleSkuPriceByCountryDtoDomain obj13 = new AliexpressSolutionBatchProductPriceUpdateRequest.SingleSkuPriceByCountryDtoDomain();
            //list12.Add(obj13);
            //obj13.SkuCode = "abc123";
            //obj13.Price = "15";
            //obj13.DiscountPrice = "13.99";
            //obj10.SkuPriceByCountryList = list12;
            //obj7.CountryPriceList = list9;
            //obj3.MultiCountryPriceConfiguration = obj7;
            //req.MutipleProductUpdateList_ = list2;
            //AliexpressSolutionBatchProductPriceUpdateResponse rsp = client.Execute(req, session);
            //Console.WriteLine(rsp.Body);


            //System.IO.File.ReadAllText(@"f:\\tmp\15\body.txt", Encoding.UTF8);
            //"%7B%22product_id%22%3A1005004177746198%2C%22multiple_sku_update_list%22%3A%5B%7B%22price%22%3A%224238.0%22%2C%22discount_price%22%3A%224238.0%22%2C%22sku_code%22%3A%22D00040385%22%7D%5D%7D";
            //null; //System.IO.File.ReadAllText(@"f:\\tmp\15\body.txt");
            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            queryParams.Add("app_key", app_key);
            queryParams.Add("format", format);
            queryParams.Add("method", method);
            queryParams.Add("session", session);
            queryParams.Add("timestamp", timestamp);
            queryParams.Add("v", v);
            queryParams.Add("sign_method", sign_method);
            queryParams.Add("mutiple_product_update_list", mutiple_product_update_list);
            //IDictionary<string, string> sortedParams = new SortedDictionary<string, string>(queryParams, StringComparer.Ordinal);
            //IEnumerator<KeyValuePair<string, string>> dem = sortedParams.GetEnumerator();
            //StringBuilder query = new StringBuilder();
            //query.Append(secret);
            //while (dem.MoveNext())
            //{
            //    string key = dem.Current.Key;
            //    string value = dem.Current.Value;
            //    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            //    {
            //        query.Append(key).Append(value);
            //    }
            //}
            //byte[] bytes;
            //query.Append(secret);
            //MD5 md5 = MD5.Create();
            //bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(query.ToString()));
            //StringBuilder result = new StringBuilder();
            //for (int i = 0; i < bytes.Length; i++)
            //{
            //    result.Append(bytes[i].ToString("X2"));
            //}
            string sign = GetSign(queryParams, null, secret);
            queryParams.Add("sign", sign);
            //using var client = new HttpClient();
            //var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(queryParams) };
            //var res = await client.SendAsync(req);
            //Console.WriteLine(res.Content);
            var cts = new CancellationTokenSource();

            var services = new ServiceCollection().AddHttpClient();
            services.AddHttpClient<IHttpService, HttpService>();
            var serviceProvider = services.BuildServiceProvider();

            var client2 = serviceProvider.GetService<IHttpService>();

            //var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();

            //var client = httpClientFactory.CreateClient();
            //setup our DI
            //var serviceProvider = new ServiceCollection()
            //    .AddLogging()
            //    .AddHttpClient()
            //    .AddHttpClient<IHttpService, HttpService>()
            //    .BuildServiceProvider();

            //var client = serviceProvider.GetService<IHttpClientFactory>();

            //configure console logging
            //serviceProvider
            //    .GetService<ILoggerFactory>()
            //    .AddConsole(LogLevel.Debug);

            //var logger = serviceProvider.GetService<ILoggerFactory>()
            //    .CreateLogger<Program>();
            //logger.LogDebug("Starting application");

            string auth = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhY2Nlc3NJRCI6IjExOGE3ZGI0LTkzOTUtNDRiYy1hYTMyLTg3MDcwNDMyM2Q2OCJ9.CHEH2iO2y_QODLQ4TX59Ct1t7oN4mHM4kbv53sEclR8";
            //using var client = new HttpClient();
            //var httpService = new HttpExtensions.HttpService(httpService, logger);
            var data = await WbClasses.Functions.GetCatalogInfo(client2, auth, cts.Token);
            //var statusResult = await YandexClasses.YandexOperators.OrderDetails("22162396", "031efc68e36241429b0d85ac288f00ce", "AQAAAABW9fzMAAeFNGqRShRjaUYBji2iT4tPk1k", "110626877");
            //if ((statusResult.Item1 == YandexClasses.StatusYandex.PROCESSING) &&
            //    (statusResult.Item2 == YandexClasses.SubStatusYandex.READY_TO_SHIP))
            //{
            //    Console.WriteLine("1");
            //}
            //else
            //{
            //    Console.WriteLine("2"); ;
            //}
            var reqData = new List<string>
                {
                    "Basic NTQ3NzE2VEwjMTAwMDAxOnJtLWFjY2VzczEzMw==",
                    "Basic NTQ3NzE2VEwjMTAwMDAxOnJtLWFjY2VzczEzMw==",
                    "Basic NTQ3NzE2VEwjMTAwMDAxOnJtLWFjY2VzczEzMw==",
                    "Basic NTQ3NzE2VEwjMTAwMDAxOnJtLWFjY2VzczEzMw==",
                    "Basic NTQ3NzE2VEwjMTAwMDAxOnJtLWFjY2VzczEzMw==",
                    "Basic NTQ3NzE2VEwjMTAwMDAxOnJtLWFjY2VzczEzMw==",
                    "Basic NTQ3NzE2VEwjMTAwMDAxOnJtLWFjY2VzczEzMw=="
                };
            string body = "{\"siteId\":9901,\"customerId\":100001,\"description\":\"FASHION\",\"shipmentRef\":\"HY450659207\",\"cost\":74.11,\"currency\":\"GBP\",\"customer\":{\"company\":\"NEW LOOK\",\"contact\":{},\"companyAddress\":{\"street\":\"PIT HEAD CLOSE\",\"district\":\"LYMEDALE BUSINESS PARK\",\"town\":\"NEWCASTLE UNDER L\",\"county\":\"STAFFORDSHIRE\",\"postcode\":\"ST5 9QG\",\"countryCode\":\"GB\"}},\"recipient\":{\"contact\":{\"name\":\"AMY THORNALLEY\",\"phone\":\"07593911130\",\"mobile\":\"07593911130\",\"email\":\"amyy_louise @live.com\"},\"companyAddress\":{\"country\":\"NORTH HUMBERSIDE\",\"street\":\"ST.ALBANS CHURCH\",\"district\":\"62 HALL ROAD\",\"town\":\"HULL\",\"postcode\":\"HU6 8SA\",\"countryCode\":\"GB\"}},\"shipment\":{\"packs\":1,\"carrier\":\"ROYAL MAIL\",\"serviceCode\":\"TPLN\",\"contractNo\":\"547716TL\",\"totalWeight\":1.133,\"addInsurance\":0,\"insValue\":0.0,\"despatchDate\":\"2022 - 05 - 10T00: 00:00 + 01:00\"},\"labelType\":3,\"invoiceType\":0}";
            var requests = reqData.Select
                (
                    x => Task.Factory.StartNew(() => GetResponse(x, body))
                ).ToList();

            //Wait for all the requests to finish
            await Task.WhenAll(requests);
            Console.WriteLine("Hello World!");
        }
    }
}