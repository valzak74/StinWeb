using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Market
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private string _yandexFbsToken;
        private string _yandexDbsToken;
        private string _ozonFbsToken;
        private readonly ILogger _logger;
        public AuthenticationMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<AuthenticationMiddleware> logger)
        {
            var defFirma = configuration["Settings:Firma"];
            _next = next;
            _yandexFbsToken = configuration["Settings:" + defFirma + ":YandexFBS"];
            _yandexDbsToken = configuration["Settings:" + defFirma + ":YandexDBS"];
            _ozonFbsToken = configuration["Settings:" + defFirma + ":OzonFBS"];
            _logger = logger;
        }
        public async Task Invoke(HttpContext context)
        {
            bool isYandexFBS = context.Request.Path.StartsWithSegments("/yandex", StringComparison.InvariantCultureIgnoreCase);
            bool isYandexDBS = context.Request.Path.StartsWithSegments("/yandexDBS", StringComparison.InvariantCultureIgnoreCase);
            bool isOzonFBS = context.Request.Path.StartsWithSegments("/ozon", StringComparison.InvariantCultureIgnoreCase);

            _logger.LogInformation($"Header: {Newtonsoft.Json.JsonConvert.SerializeObject(context.Request.Headers, Newtonsoft.Json.Formatting.Indented)}");

            context.Request.EnableBuffering();
            var body = await new System.IO.StreamReader(context.Request.Body).ReadToEndAsync();
            _logger.LogInformation($"Body: {body}");
            context.Request.Body.Position = 0;

            _logger.LogInformation($"Host: {context.Request.Host.Host}");
            _logger.LogInformation($"Client IP: {context.Connection.RemoteIpAddress}");

            string queryAuth = "";
            string headerAuth = "";
            string token = isYandexFBS ? _yandexFbsToken : 
                isYandexDBS ? _yandexDbsToken :
                isOzonFBS ? _ozonFbsToken : "";
            if (context.Request.Query.ContainsKey("auth-token"))
                queryAuth = context.Request.Query["auth-token"];
            if (context.Request.Headers.ContainsKey("Authorization"))
                headerAuth = context.Request.Headers["Authorization"];

            if ((string.IsNullOrEmpty(queryAuth) && string.IsNullOrEmpty(headerAuth)) ||
                ((!string.IsNullOrEmpty(queryAuth) && (queryAuth != token)) ||
                (!string.IsNullOrEmpty(headerAuth) && (headerAuth != token))))
            {
                context.Response.StatusCode = 403; //Forbidden
                return;
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
