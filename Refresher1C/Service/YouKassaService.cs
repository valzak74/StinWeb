using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Refresher1C.Models.YouKassa;
using Microsoft.Extensions.Configuration;
using JsonExtensions;
using StinClasses.Models;

namespace Refresher1C.Service
{
    class YouKassaService: IYouKassaService
    {
        private readonly StinDbContext _context;
        private IServiceScopeFactory _serviceScopeFactory;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<YouKassaService> _logger;
        private readonly string _url;
        private protected bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            this.disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public YouKassaService(ILogger<YouKassaService> logger, IServiceScopeFactory serviceScopeFactory, StinDbContext context, IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _context = context;
            _serviceScopeFactory = serviceScopeFactory;
            _clientFactory = clientFactory;
            var config = serviceScopeFactory.CreateScope().
                     ServiceProvider.GetRequiredService<IConfiguration>();
            _url = config["YouKassa:url"];
            if (!_url.EndsWith('/'))
                _url += "/";
        }
        public async Task CheckPaymentStatusAsync(CancellationToken stoppingToken)
        {
            IDictionary<PaymentStatus, string> СтатусыПлатежа = new Dictionary<PaymentStatus, string>
            {
                { PaymentStatus.Pending, "   AOW   " },
                { PaymentStatus.WaitingForCapture, "   AOX   " },
                { PaymentStatus.Succeeded, "   AOY   " },
                { PaymentStatus.Canceled, "   AOZ   " },
                { PaymentStatus.Unsupported, "     0   " },
            };
            List<string> ОтслеживаемыеСтатусы = СтатусыПлатежа
                .Where(x => x.Key == PaymentStatus.Pending || x.Key == PaymentStatus.WaitingForCapture)
                .Select(y => y.Value)
                .ToList();
            var Payments = from doc in _context.Dh13849s
                           join j in _context._1sjourns on doc.Iddoc equals j.Iddoc
                           where j.Closed == 1 && doc.Sp13837 == 1 && !string.IsNullOrWhiteSpace(doc.Sp13831) && ОтслеживаемыеСтатусы.Contains(doc.Sp13833)
                           select new
                           {
                               IdDoc = doc.Iddoc,
                               ShopId = doc.Sp13829.Trim(),
                               SecretKey = doc.Sp13830.Trim(),
                               PaymentId = doc.Sp13831.Trim(),
                               DBStatus = doc.Sp13833,
                               DocSum = doc.Sp13845
                           };
            foreach (var p in Payments)
            {
                var Status = СтатусыПлатежа.Where(x => x.Value == p.DBStatus).Select(y => y.Key).FirstOrDefault();
                var CurrentResult = await GetPayment(p.ShopId, p.SecretKey, p.PaymentId);
                if (CurrentResult.status != Status)
                {
                    if (p.DocSum != CurrentResult.paymentAmount)
                    {
                        _logger.LogError("Сумма оплаты не совпадает с суммой документа '{0}'!", p.IdDoc);
                    }
                    else
                    {
                        using IDocCreateOrUpdate DocScope = _serviceScopeFactory.CreateScope()
                               .ServiceProvider.GetService<IDocCreateOrUpdate>();
                        await DocScope.CompleteSuccessPaymentAsync(p.IdDoc, СтатусыПлатежа.FirstOrDefault(x => x.Key == CurrentResult.status).Value, stoppingToken);
                    }
                }
            }
        }
        private async Task<(PaymentStatus status, decimal paymentAmount)> GetPayment(string shopId, string secretKey, string paymentId)
        {
            using var client = _clientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, _url + paymentId);
            request.Headers.Add("User-Agent", "HttpClientFactory-StinClient");
            var byteArray = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", shopId, secretKey));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            try
            {
                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    using var responseStream = await response.Content.ReadAsStreamAsync();
                    var options = new JsonSerializerOptions
                    {
                        Converters =
                    {
                        new JsonStringEnumConverter( JsonNamingPolicy.CamelCase),
                        new JsonDecimalConverter(),
                        new JsonDateTimeConverter()
                    },
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    };
                    var payment = await JsonSerializer.DeserializeAsync<Payment>(responseStream, options);
                    return (status: payment.Status, paymentAmount: payment.Amount.Value);
                }
            }
            catch 
            {
            }
            return (status: PaymentStatus.Unsupported, paymentAmount: 0m);
        }
    }
}
