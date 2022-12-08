using JsonExtensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Text;

namespace HttpExtensions
{
    public class HttpService : IHttpService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HttpService> _logger;
        public HttpService(HttpClient httpClient, ILogger<HttpService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }
        bool IsValidJson(string stringValue)
        {
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                return false;
            }

            var value = stringValue.Trim();

            if ((value.StartsWith("{") && value.EndsWith("}")) || //For object
                (value.StartsWith("[") && value.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JToken.Parse(value);
                    return true;
                }
                catch (JsonReaderException)
                {
                    return false;
                }
            }
            return false;
        }
        public async Task<Tuple<T, E>> Exchange<T, E>(string url, HttpMethod method, Dictionary<string, string> queryParameters, CancellationToken stoppingToken, [CallerMemberName] string callerName = "")
        {
            try
            {
                var request = new HttpRequestMessage(method, url) { Content = new FormUrlEncodedContent(queryParameters) };
                request.Headers.Add("User-Agent", "HttpClientFactory-StinClient");
                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    if (response.Content != null)
                    {
                        //_logger.LogError(await response.Content.ReadAsStringAsync());
                        var bytes = await response.Content.ReadAsByteArrayAsync();
                        if (typeof(T) == typeof(byte[]))
                            return new Tuple<T, E>((T)(object)bytes, default);
                        else
                        {
                            try
                            {
                                return new Tuple<T, E>(bytes.DeserializeObject<T>(), default);
                            }
                            catch
                            {
                                return new Tuple<T, E>(default, bytes.DeserializeObject<E>());
                            }
                        }
                    }
                    else
                    {
                        if (typeof(T) == typeof(bool))
                        {
                            return new Tuple<T, E>((T)(object)true, default);
                        }
                    }
                }
                else
                {
                    if (response.Content != null)
                    {
                        string r = await response.Content.ReadAsStringAsync();
                        if (IsValidJson(r))
                        {
                            var errBytes = await response.Content.ReadAsByteArrayAsync();
                            var obj = errBytes.DeserializeObject<E>();
                            return new Tuple<T, E>(default, obj);
                        }
                        else if (typeof(E) == typeof(string))
                        {
                            return new Tuple<T, E>(default, (E)(object)r);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(callerName + " : " + ex.Message);
            }
            return new Tuple<T, E>(default, default);
        }
        public async Task<Tuple<T, E>> Exchange<T, E>(string url, HttpMethod method, Dictionary<string, string> headers, object? content, CancellationToken stoppingToken, [CallerMemberName] string callerName = "")
        {
            try
            {
                string queryKey = "QueryParameter";
                var queryParams = headers
                    .Where(x => x.Key.StartsWith(queryKey))
                    .ToDictionary(k => k.Key.Substring(queryKey.Length), v => v.Value);
                HttpRequestMessage request;
                if (queryParams?.Count > 0)
                    request = new HttpRequestMessage(method, url) { Content = new FormUrlEncodedContent(queryParams) };
                else
                    request = new HttpRequestMessage(method, url);
                request.Headers.Add("User-Agent", "HttpClientFactory-StinClient");
                foreach (var header in headers.Where(x => !x.Key.StartsWith(queryKey)))
                    request.Headers.Add(header.Key, header.Value);
                if (content != null)
                {
                    var contentString = content.SerializeObject();
                    //Console.WriteLine(contentString);
                    if (!string.IsNullOrEmpty(contentString))
                    {
                        request.Content = new StringContent(contentString, Encoding.UTF8, "application/json");
                    }
                }
                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    if (response.Content != null)
                    {
                        var bytes = await response.Content.ReadAsByteArrayAsync();
                        //string r = await response.Content.ReadAsStringAsync();
                        //Console.WriteLine(r);
                        //System.IO.File.WriteAllText(@"f:\\tmp\15\r2.txt", r);
                        if (typeof(T) == typeof(byte[]))
                            return new Tuple<T, E>((T)(object)bytes, default);
                        else
                            return new Tuple<T, E>(bytes.DeserializeObject<T>(), default);
                    }
                    else
                    {
                        if (typeof(T) == typeof(bool))
                        {
                            return new Tuple<T, E>((T)(object)true, default);
                        }
                    }
                }
                else
                {
                    if (response.Content != null)
                    {
                        string r = await response.Content.ReadAsStringAsync();
                        if (IsValidJson(r))
                        {
                            var errBytes = await response.Content.ReadAsByteArrayAsync();
                            var obj = errBytes.DeserializeObject<E>();
                            return new Tuple<T, E>(default, obj);
                        }
                        else if (typeof(E) == typeof(string))
                        {
                            return new Tuple<T, E>(default, (E)(object)r);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(callerName + " : " + ex.Message);
            }
            return new Tuple<T, E>(default, default);
        }
        public async Task<byte[]?> DownloadFileAsync(string link, string code, CancellationToken stoppingToken, [CallerMemberName] string callerName = "")
        {
            try
            {
                return await _httpClient.GetByteArrayAsync(link);
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode != System.Net.HttpStatusCode.NotFound)
                    _logger.LogError(callerName + " : " + code + " : " + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(callerName + " : " + code + " : " + ex.Message);
            }
            return null;
        }
    }
}