using JsonExtensions;
using Microsoft.Extensions.Logging;
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
                        var errBytes = await response.Content.ReadAsByteArrayAsync();
                        var obj = errBytes.DeserializeObject<E>();
                        return new Tuple<T, E>(default, obj);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(callerName + " : " + ex.Message);
            }
            return new Tuple<T, E>(default, default);
        }
        public async Task<Tuple<T, E>> Exchange<T, E>(string url, HttpMethod method, Dictionary<string, string> headers, object content, CancellationToken stoppingToken, [CallerMemberName] string callerName = "")
        {
            try
            {
                var request = new HttpRequestMessage(method, url);
                request.Headers.Add("User-Agent", "HttpClientFactory-StinClient");
                foreach (var header in headers)
                    request.Headers.Add(header.Key, header.Value);
                if (content != null)
                {
                    var contentString = content.SerializeObject();
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
                        var errBytes = await response.Content.ReadAsByteArrayAsync();
                        var obj = errBytes.DeserializeObject<E>();
                        return new Tuple<T, E>(default, obj);
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