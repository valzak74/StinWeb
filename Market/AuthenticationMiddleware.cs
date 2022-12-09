using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WbClasses;

namespace Market
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private string _yandexFbsToken;
        private string _yandexDbsToken;
        private string _yandexExpressToken;
        private string _sberFbsToken;
        private readonly ILogger _logger;
        public AuthenticationMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<AuthenticationMiddleware> logger)
        {
            var defFirma = configuration["Settings:Firma"];
            _next = next;
            _yandexFbsToken = configuration["Settings:" + defFirma + ":YandexFBS"];
            _yandexDbsToken = configuration["Settings:" + defFirma + ":YandexDBS"];
            _yandexExpressToken = configuration["Settings:" + defFirma + ":YandexExpress"];
            _sberFbsToken = "Basic " + Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(configuration["Settings:" + defFirma + ":SberFBS"]));
            _logger = logger;
        }
        public async Task Invoke(HttpContext context)
        {
            var controller = context.GetRouteValue("controller").ToString().ToUpper();
            var action = context.GetRouteValue("action").ToString().ToUpper();

            _logger.LogInformation($"Header: {Newtonsoft.Json.JsonConvert.SerializeObject(context.Request.Headers, Newtonsoft.Json.Formatting.Indented)}");

            context.Request.EnableBuffering();
            var body = await new System.IO.StreamReader(context.Request.Body).ReadToEndAsync();
            _logger.LogInformation($"Body: {body}");
            context.Request.Body.Position = 0;

            _logger.LogInformation($"Host: {context.Request.Host.Host}");
            _logger.LogInformation($"Client IP: {context.Connection.RemoteIpAddress}");

            string auth = "";
            if (context.Request.Query.ContainsKey("auth-token"))
                auth = context.Request.Query["auth-token"];
            if (context.Request.Headers.ContainsKey("Authorization"))
                auth = context.Request.Headers["Authorization"];

            var validTokens = new List<string> { controller switch
            {
                "YANDEX" => _yandexFbsToken,
                "YANDEXDBS" => _yandexDbsToken,
                "SBER" => _sberFbsToken,
                _ => ""
            } };
            if ((controller == "YANDEX") && !string.IsNullOrEmpty(_yandexExpressToken))
                validTokens.Add(_yandexExpressToken);

            if (string.IsNullOrEmpty(auth) || !validTokens.Contains(auth))
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
