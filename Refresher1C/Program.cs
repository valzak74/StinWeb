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
                    services.AddScoped<IYouKassaService, YouKassaService>();
                    services.AddScoped<IMarketplaceService, MarketplaceService>();
                    services.AddHostedService<WorkerMarketplaceStocker>();
                    services.AddHostedService<WorkerMarketplaceOrderer>();
                    services.AddHostedService<Worker>();
                    services.AddHostedService<WorkerMarketplaces>();
                    services.AddHostedService<WorkerMarketplacePricer>();
                    services.AddHostedService<WorkerMarketplaceCatalog>();
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
