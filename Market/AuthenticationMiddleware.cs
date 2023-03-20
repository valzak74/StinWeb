using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Market
{
    public class IpAddressRange
    {
        readonly long _startIp;
        readonly long _endIp;
        public IpAddressRange(string startIpAddr, string endIpAddr)
        {
            _startIp = BitConverter.ToInt32(IPAddress.Parse(startIpAddr).GetAddressBytes().Reverse().ToArray(), 0);
            _endIp = BitConverter.ToInt32(IPAddress.Parse(endIpAddr).GetAddressBytes().Reverse().ToArray(), 0);
        }
        public bool IsValid(string address)
        {
            long ip = BitConverter.ToInt32(IPAddress.Parse(address).GetAddressBytes().Reverse().ToArray(), 0);
            return ip > _startIp && ip < _endIp;
        }
    }
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private string _yandexFbsToken;
        private string _yandexDbsToken;
        private string _sberFbsToken;
        private readonly ILogger _logger;
        private readonly Dictionary<string,List<IpAddressRange>> _whiteList;
        public AuthenticationMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<AuthenticationMiddleware> logger)
        {
            var defFirma = configuration["Settings:Firma"];
            _next = next;
            _yandexFbsToken = configuration["Settings:" + defFirma + ":YandexFBS"];
            _yandexDbsToken = configuration["Settings:" + defFirma + ":YandexDBS"];
            _sberFbsToken = "Basic " + Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(configuration["Settings:" + defFirma + ":SberFBS"]));
            _logger = logger;
            var whiteList = Enumerable.Repeat(new
            {
                Market = "",
                StartIp = "",
                EndIp = ""
            }, 0).ToList();
            foreach (var item in configuration.GetSection("WhiteListRanges").GetChildren())
            {
                var configData = item.AsEnumerable();
                whiteList.Add(new
                {
                    Market = configData.FirstOrDefault(x => x.Key.EndsWith("Market")).Value,
                    StartIp = configData.FirstOrDefault(x => x.Key.EndsWith("StartIp")).Value,
                    EndIp = configData.FirstOrDefault(x => x.Key.EndsWith("EndIp")).Value
                });
            }
            _whiteList = whiteList
                .GroupBy(x => x.Market)
                .Select(gr => new
                {
                    Market = gr.Key,
                    Data = gr.Select(x => new IpAddressRange(x.StartIp, x.EndIp)).ToList()
                })
                .ToDictionary(k => k.Market, v => v.Data);
        }
        bool IsWhiteListAddress(string controller, string address)
        {
            if (_whiteList.TryGetValue(controller, out var whiteList))
            {
                foreach (var item in whiteList)
                {
                    if (item.IsValid(address))
                        return true;
                }
                return false;
            }
            else
                return true; 
        }
        public async Task Invoke(HttpContext context)
        {
            var controller = context.GetRouteValue("controller")?.ToString().ToUpper();
            var action = context.GetRouteValue("action")?.ToString().ToUpper();

            _logger.LogInformation($"Header: {Newtonsoft.Json.JsonConvert.SerializeObject(context.Request.Headers, Newtonsoft.Json.Formatting.Indented)}");

            context.Request.EnableBuffering();

            //using SHA256 sha256Hash = SHA256.Create();
            //using MemoryStream ms = new MemoryStream();
            //await context.Request.Body.CopyToAsync(ms);
            //var originalBody = ms.ToArray();
            //// ComputeHash - returns byte array  
            //byte[] bytes = sha256Hash.ComputeHash(originalBody);
            //// Convert byte array to a string   
            //StringBuilder builder = new StringBuilder();
            //for (int i = 0; i < bytes.Length; i++)
            //{
            //    builder.Append(bytes[i].ToString("x2"));
            //}
            //_logger.LogInformation($"Sha256 Body: {builder.ToString()}"); 
            //context.Request.Body.Position = 0;

            var body = await new System.IO.StreamReader(context.Request.Body).ReadToEndAsync();
            _logger.LogInformation($"Body: {body}");
            context.Request.Body.Position = 0;

            _logger.LogInformation($"Host: {context.Request.Host.Host}");
            _logger.LogInformation($"Client IP: {context.Connection.RemoteIpAddress}");

            if (!IsWhiteListAddress(controller, context.Request.Host.Host))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return;
            }

            string auth = "";
            if (context.Request.Query.ContainsKey("auth-token"))
                auth = context.Request.Query["auth-token"];
            if (context.Request.Headers.ContainsKey("Authorization"))
                auth = context.Request.Headers["Authorization"];

            List<string> validTokens = controller switch
            {
                "YANDEX" => _yandexFbsToken.Split(',').ToList(),
                "YANDEXDBS" => _yandexDbsToken.Split(',').ToList(),
                "SBER" => new List<string> { _sberFbsToken },
                _ => null
            };

            if ((controller != "OZON") && (string.IsNullOrEmpty(auth) || (validTokens == null) || !validTokens.Contains(auth)))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden; 
                return;
            }
            else if (action == "GETPRODUCTFEED")
            {
                await _next.Invoke(context);
                _logger.LogInformation($"{context.Response.StatusCode}: file content");
            }
            else
            {
                //Copy a pointer to the original response body stream
                var originalBodyStream = context.Response.Body;
                //Create a new memory stream...
                using (var responseBody = new MemoryStream())
                {
                    //...and use that for the temporary response body
                    context.Response.Body = responseBody;

                    //Continue down the Middleware pipeline, eventually returning to this class
                    await _next.Invoke(context);

                    //Format the response from the server
                    var response = await FormatResponse(context.Response);

                    _logger.LogInformation(response);

                    //Copy the contents of the new memory stream (which contains the response) to the original stream, which is then returned to the client.
                    await responseBody.CopyToAsync(originalBodyStream);
                }
            }
            //await _next.Invoke(context);
        }
        private async Task<string> FormatResponse(HttpResponse response)
        {
            //We need to read the response stream from the beginning...
            response.Body.Seek(0, SeekOrigin.Begin);

            //...and copy it into a string
            string text = await new StreamReader(response.Body).ReadToEndAsync();

            //We need to reset the reader for the response so that the client can read it.
            response.Body.Seek(0, SeekOrigin.Begin);

            //Return the string for the response, including the status code (e.g. 200, 404, 401, etc.)
            return $"{response.StatusCode}: {text}";
        }

    }
}
