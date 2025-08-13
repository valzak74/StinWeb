using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Refresher1C.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StinClasses.Models;
using System.Net.Http;
using System;
using Polly;
using Polly.Extensions.Http;
using HttpExtensions;
using StinClasses.—правочники.Functions;
using StinClasses.–егистры;
using StinClasses.MarketCommission;

namespace Refresher1C
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .UseSerilog((ctx, config) => { config.ReadFrom.Configuration(ctx.Configuration); })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddMemoryCache();
                IConfiguration configuration = hostContext.Configuration;
                services.AddDbContext<StinDbContext>(opts => opts
                    .UseSqlServer(hostContext.Configuration.GetConnectionString("DB")));
                //, sqlOptions =>
                //{
                //    sqlOptions.EnableRetryOnFailure(
                //        maxRetryCount: 5,
                //        maxRetryDelay: System.TimeSpan.FromSeconds(10),
                //        errorNumbersToAdd: new List<int> { 4060 });
                //}));
                
                services.AddHttpClient<IHttpService, HttpService>()
                    .AddPolicyHandler(GetRetryPolicy());
                services.AddScoped<IDocCreateOrUpdate, DocCreateOrUpdate>();
                services.AddScoped<IMarketplaceService, MarketplaceService>();

                services.AddScoped<IStockFunctions, StockFunctions>();
                services.AddScoped<IFirmaFunctions, FirmaFunctions>();
                services.AddScoped<IMarketplaceFunctions, MarketplaceFunctions>();
                services.AddScoped<INomenklaturaFunctions, NomenklaturaFunctions>();
                services.AddScoped<IOrderFunctions, OrderFunctions>();
                services.AddScoped<IWildberriesHelper, WildberriesHelper>();

                services.AddScoped<I–егистрќстатки“ћ÷, –егистр_ќстатки“ћ÷>();
                services.AddScoped<I–егистр–езервы“ћ÷, –егистр_–езервы“ћ÷>();
                services.AddScoped<I–егистр—топЋист«„, –егистр_—топЋист«„>();
                services.AddScoped<I–егистрЌаборЌа—кладе, –егистр_ЌаборЌа—кладе>();

                services.AddScoped<IMarkupFactorPercentDictionary, MarkupFactorPercentDictionary>();

                if (configuration.GetSection("YouKassa:enable").Get<bool>())
                {
                    services.AddScoped<IYouKassaService, YouKassaService>();
                    services.AddHostedService<Worker>();
                }
                if (configuration.GetSection("Stocker:enable").Get<bool>())
                {
                    services.AddScoped<IStocker, Stocker>();
                    services.AddHostedService<WorkerMarketplaceStocker>();
                }
                if (configuration.GetSection("Orderer:enable").Get<bool>())
                {
                    services.AddHostedService<WorkerMarketplaceOrderer>();
                    services.AddHostedService<WorkerMarketplaceOrdererSlow>();
                }
                if (configuration.GetSection("Marketplace:enable").Get<bool>())
                    services.AddHostedService<WorkerMarketplaces>();
                if (configuration.GetSection("Pricer:enable").Get<bool>())
                {
                    services.AddScoped<IPricer, Pricer>();
                    services.AddHostedService<WorkerMarketplacePricer>();
                }
                if (configuration.GetSection("Catalog:enable").Get<bool>())
                    services.AddHostedService<WorkerMarketplaceCatalog>();
                if (configuration.GetSection("Returns:enable").Get<bool>())
                    services.AddHostedService<WorkerReturns>();
                if (configuration.GetSection("OncePerDay:enable").Get<bool>())
                    services.AddHostedService<WorkerOncePerDay>();
            });
        static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                //.OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }
    }
}
