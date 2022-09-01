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
                if (configuration.GetSection("YouKassa:enable").Get<bool>())
                {
                    services.AddScoped<IYouKassaService, YouKassaService>();
                    services.AddHostedService<Worker>();
                }
                if (configuration.GetSection("Stocker:enable").Get<bool>())
                    services.AddHostedService<WorkerMarketplaceStocker>();
                if (configuration.GetSection("Orderer:enable").Get<bool>())
                    services.AddHostedService<WorkerMarketplaceOrderer>();
                if (configuration.GetSection("Marketplace:enable").Get<bool>())
                    services.AddHostedService<WorkerMarketplaces>();
                if (configuration.GetSection("Pricer:enable").Get<bool>())
                    services.AddHostedService<WorkerMarketplacePricer>();
                if (configuration.GetSection("Catalog:enable").Get<bool>())
                    services.AddHostedService<WorkerMarketplaceCatalog>();
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
