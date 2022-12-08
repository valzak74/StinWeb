using System.Runtime.CompilerServices;

namespace HttpExtensions
{
    public interface IHttpService
    {
        Task<Tuple<T, E>> Exchange<T, E>(string url, HttpMethod method, Dictionary<string, string> queryParameters, CancellationToken stoppingToken, [CallerMemberName] string callerName = "");
        Task<Tuple<T, E>> Exchange<T, E>(string url, HttpMethod method, Dictionary<string, string> headers, object? content, CancellationToken stoppingToken, [CallerMemberName] string callerName = "");
        Task<byte[]?> DownloadFileAsync(string link, string code, CancellationToken stoppingToken, [CallerMemberName] string callerName = "");
    }
}
