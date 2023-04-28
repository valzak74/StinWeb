using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text;
using StinClasses;
using System.Net;
using WbClasses;
using SixLabors.Fonts;
using System.Globalization;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using JsonExtensions;
using HttpExtensions;
using MigraDocCore.DocumentObjectModel;
using StinClasses.Справочники;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PdfHelper;

namespace HelloWorld
{
    class Program
    {

        //public static IEnumerable<int> MyWhere(this IEnumerable<int> source, Func<int,bool> predicate)
        //{
        //    foreach (int item in source)
        //        if (predicate(item))
        //            yield return item;
        //}
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
        static void temp(object o)
        {
            string ass = "";
        }
        static List<T> DeserializeObject<T>(string data) where T : class, new() 
        { 
            //string json = System.IO.File.ReadAllText(@"f:\tmp\31\text.txt", Encoding.UTF8);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<List<T>>(data,
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                    Converters = { new SingleObjectOrArrayJsonConverter<T>() }
                });
        }
        public static string GetWindowsFamilyName(string fontN = "EAN-13")
   {
        // Note that many fonts are charged, so find free fonts, if not, download free fonts from the official website and install
        // Free fonts (commercially available) : Founder Black Body (FZHei-B01S), Founder Book Song (FZShuSong-Z01S), Founder Fang Song (FZFangSong-Z02S), Founder Regular Regular (FZKai-Z03S) 
       var fontDir = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\Fonts");
            DirectoryInfo d = new DirectoryInfo(fontDir); //Assuming Test is your Folder

            FileInfo[] Files = d.GetFiles(); //Getting Text files
            //string str = "";

            foreach (FileInfo file in Files)
            {
                //string fontPathFile = Path.Combine($"{fontDir}\\{file}.ttf ");
                try
                {
                    FontDescription fontDescription = FontDescription.LoadDescription(file.FullName);
                    string fontName = fontDescription.FontName(CultureInfo.InvariantCulture);
                    //string fontFamilyName = fontDescription.FontFamily(CultureInfo.InvariantCulture);
                    if (fontName == "Code 128")
                    {
                        return file.Name;
                    }
                }
                catch { }
                //str = str + ", " + file.Name;
            }
   
        return "";
    }

        //    foreach (var fontFile in Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.Fonts)))
        //    {
        //        var fc = new PrivateFontCollection();

        //        if (File.Exists(fontFile))
        //            fc.AddFontFile(fontFile);

        //        if ((!fc.Families.Any()))
        //            continue;

        //        var name = fc.Families[0].Name;

        //        // If you care about bold, italic, etc, you can filter here.
        //        if (!fontNameToFiles.TryGetValue(name, out var files))
        //        {
        //            files = new List<string>();
        //            fontNameToFiles[name] = files;
        //        }

        //        files.Add(fontFile);
        //    }

        //    if (!fontNameToFiles.TryGetValue(fontName, out var result))
        //        return null;

        //    return result;
        //}
        static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        static async Task Main(string[] args)
        {
            var f = new List<byte[]> { System.IO.File.ReadAllBytes(@"f:\\tmp\15\label3.pdf") };
            var rt = PdfFunctions.Instance.MergePdf(f);
            File.WriteAllBytes(@"f:\\tmp\15\label4.pdf", rt);
            //using Stream stream = new MemoryStream(f);
            //stream.Position = 0;
            //using PdfDocument pdfFile = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
            //try
            //{
            //    using var pdf = new PdfDocument(@"f:\\tmp\15\label.pdf");
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.ToString());
            //}

            //var label = System.IO.File.ReadAllBytes(@"f:\\tmp\15\label.pdf");
            //label[1000] = 255;
            //File.WriteAllBytes(@"f:\\tmp\15\label3.pdf", label);
            var signature = Convert.FromBase64String("MEQCIEvYEHjmiAuraVFE6ORq1ag88mp+lxCJ353CbOgeeTyFAiBTWPfKs+uePhKKGb4Qamgw25lPJN+Vrn9pJj78cfTMzw==");
            StringBuilder builder = new StringBuilder();
            for (int ii = 0; ii < signature.Length; ii++)
            {
                builder.Append(signature[ii].ToString("x2"));
            }
            Console.WriteLine(builder.ToString());
            string plainData = "{\"message_type\": \"TYPE_PING\", \"time\": \"2023-03-14T08:14:24Z\"}";
            Console.WriteLine("Raw data: {0}", plainData);
            string hashedData = ComputeSha256Hash(plainData);
            Console.WriteLine("Hash {0}", hashedData);
            Console.ReadLine();

            int e = 0;
            //IPAddress.TryParse("192.168.229.145", out IPAddress ip);
            //if (ip != null)
            ////foreach (var ip in Dns.GetHostAddresses(Dns.GetHostName()))
            //{
            //    var endPoint = new IPEndPoint(ip, 0);
            //    Console.WriteLine("Request from: " + ip);
            //    var request = (HttpWebRequest)HttpWebRequest.Create("https://api.onlinemarket.su/marketip/sber/products/feed_43956_001.xml");
            //    WebProxy myproxy = new WebProxy("192.168.229.145");
            //    myproxy.BypassProxyOnLocal = false;
            //    request.Proxy = myproxy;

            //    //request.ServicePoint.BindIPEndPointDelegate = delegate {
            //    //    return endPoint;
            //    //};
            //    var response = (HttpWebResponse)request.GetResponse();
            //    Console.WriteLine("Actual IP: " + response.GetResponseHeader("X-YourIP"));
            //    response.Close();
            //}
            //Console.ReadKey();
            //var barcodeText = "%97W%2ALA9D%%";
            //var barcodeBytes = PdfHelper.PdfFunctions.Instance.GenerateBarcode128(barcodeText);
            //var co = SystemFonts.Collection;
            //var fo = SystemFonts.Families.FirstOrDefault(x => x.Name == "Code 128");
            //Font font = new Font(fo, 24, FontStyle.Regular);
            //Console.WriteLine(GetSystemFontFileName(font));
            //var sre = GetFilesForFont("Code 128");
            //Console.WriteLine(GetWindowsFamilyName("7fonts.ru_code128"));
            File.WriteAllBytes("f://tmp/15/1/test.pdf", PdfHelper.PdfFunctions.Instance.ProductSticker("785621511", "Полотенце 40 х 40", "артикул", "бежевый", "XL"));
            string re = "{\"code\": \"NotFound\", \"message\": \"Не найдено\"}";
            //string re = $"[\r\n  {{\r\n    \"code\": \"SubjectDBSRestriction\",\r\n    \"message\": \"Категория товара недоступна для продажи по схеме 'Везу на склад Wildberries'.\",\r\n    \"data\": [\r\n      {{\r\n        \"sku\": \"skuTest1\",\r\n        \"stock\": 0\r\n      }}\r\n    ]\r\n  }},\r\n  {{\r\n    \"code\": \"SubjectFBSRestriction\",\r\n    \"message\": \"Категория товара недоступна для продажи по схеме 'Везу самостоятельно до клиента'.\",\r\n    \"data\": [\r\n      {{\r\n        \"sku\": \"skuTest2\",\r\n        \"stock\": 1\r\n      }}\r\n    ]\r\n  }},\r\n  {{\r\n    \"code\": \"UploadDataLimit\",\r\n    \"message\": \"Превышен лимит загружаемых данных\",\r\n    \"data\": [\r\n      {{\r\n        \"sku\": \"skuTest2\",\r\n        \"stock\": 10001\r\n      }}\r\n    ]\r\n  }},\r\n  {{\r\n    \"code\": \"CargoWarehouseRestriction\",\r\n    \"message\": \"Выбранный склад не предназначен для крупногабаритных товаров. Добавьте их на соответствующий склад\",\r\n    \"data\": [\r\n      {{\r\n        \"sku\": \"skuTest3\",\r\n        \"stock\": 10\r\n      }}\r\n    ]\r\n  }},\r\n  {{\r\n    \"code\": \"NotFound\",\r\n    \"message\": \"Не найдено\",\r\n    \"data\": [\r\n      {{\r\n        \"sku\": \"skuTest4\",\r\n        \"stock\": 10\r\n      }}\r\n    ]\r\n  }}\r\n]";
            byte[] bytes = Encoding.UTF8.GetBytes(re);
            var rez = DeserializeObject<StockError>(re);
            var rez2 = bytes.DeserializeObjectToList<StockError>();
            var stoppingToken = new CancellationTokenSource();
            var _httpService = new HttpExtensions.HttpService(new HttpClient(), null);
            List<long> logisticsOrderIds = new List<long> { 9552897 };
            string authToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJzZWxsZXJfaWQiOjM1MzAyOTc2MTYsInRva2VuX2lkIjoyNDI1fQ.LhSOb9x-4fa8axB8TUYoxw6BCViZgDgGh1USgBWwUd-9cJFNU7FoyymVzNw_bj3690yWXBugYoQwcDTYGnOLs25nXdt-DhV8ObFQLmoo9c6I5-n5tjpU4HK2mDZa8pILWYiaPkCPFsipSRt2V40PKIiVInIIkaDz3Morm4BGs3qM7qxZh_fccZMKy5qqn-uxp5Oq09o4tsH-UDxsnfeqEG9PYOnXTpCxLOr88LlOTcIMOgJsTF2Y7UjLL5uiccCuZm8BNAvlaS_CqwufSqeSkge1ULWf2VUUwPTZpdsnrDIhNd43YK9SWxZL1t7qmNSTBHsSeE9Tu2QdG8fQ5DHThM5nhHJ5xz4MiWtWdirnWIGelZbGErLj2OUwQN7HeNL3YiHSgRu4TUJOKgBOQtdgrtZzAIYsGBoNn0M5e8Pj_j-W5Vp5xv4ub8LEpM1aFMqWnJeygRpSzPVbm0nPo_eFqiWgZ4VZ8orU0eaqjkV9_0PgXquzWwmmyVRTReIRBiuKNIr_NHGv2arxsz7OVzrUC1K4liIwWUnL1SSf5IjZBvoekNnDRMDUSrL-NH0G_1DoHjRy_OhYAth6zFIa18atpcP1YLLGcyhceb-nugzdOkjjmXfmeUg1bl6YCGkDWu9vBz9EkjJKJpNRCe2OR7j7Dwo1HkdOVcQfuLqFs_hl4Ac";
            var result = await AliExpressClasses.Functions.GetLabels(_httpService,"" , authToken,
                logisticsOrderIds,
                stoppingToken.Token);
            if (result.pdf != null)
            {
                File.WriteAllBytes(@"f:\\tmp\15\label.pdf", result.pdf);
            }
            double sec = 27024581;
            var ts = TimeSpan.FromSeconds(sec);
            decimal r = 9m;
            Console.WriteLine(r.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
            string login = "dXNlclRlc3Q6dXNlcnBhcw==";
            var encoding = Encoding.GetEncoding("iso-8859-1");
            var credentials = encoding.GetString(Convert.FromBase64String(login));
            Console.WriteLine("Basic " + credentials);
            string code = "K00033539";
            string decCode = code.EncodeDecString();
            string decoded = decCode.TryDecodeDecString();
            Console.WriteLine(code);
            Console.WriteLine(decCode);
            Console.WriteLine(decoded);
            int i = 10;
            var arr = new[] { 10, 15, 20 };
            var query = arr.Where(x => x == i).Where(x => x <= 20);
            i = 15;
            var result1 = query.ToList();
            Console.WriteLine(result1.Count());
            Console.WriteLine(result1[0]);
            Console.ReadLine();
            var rooooo = Enumerable.Range(0, 4);
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

            //ITopClient client = new DefaultTopClient(url, app_key, secret);
            //AliexpressSolutionOrderGetRequest req = new AliexpressSolutionOrderGetRequest();
            //AliexpressSolutionOrderGetRequest.OrderQueryDomain obj1 = new AliexpressSolutionOrderGetRequest.OrderQueryDomain();
            //obj1.CreateDateEnd = "2017-10-12 12:12:12";
            //obj1.CreateDateStart = "2017-10-12 12:12:12";
            //obj1.ModifiedDateStart = "2017-10-12 12:12:12";
            ////obj1.OrderStatusList = "SELLER_PART_SEND_GOODS";
            //obj1.BuyerLoginId = "test";
            //obj1.PageSize = 20L;
            //obj1.ModifiedDateEnd = "2017-10-12 12:12:12";
            //obj1.CurrentPage = 1L;
            //obj1.OrderStatus = "SELLER_PART_SEND_GOODS";
            //req.Param0_ = obj1;
            //AliexpressSolutionOrderGetResponse rsp = client.Execute(req, session);
            //Console.WriteLine(rsp.Body);
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
            //var data = await WbClasses.Functions.GetCatalogInfo(client2, auth, cts.Token);
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