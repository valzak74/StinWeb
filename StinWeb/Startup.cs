using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using StinWeb.Models.DataManager;
using StinWeb.Models.Repository;
using Microsoft.Extensions.Logging;
using StinClasses.Models;
using HttpExtensions;
using Polly.Extensions.Http;
using Polly;
using System.Net.Http;
using StinClasses.Справочники.Functions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;

namespace StinWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            sConfiguration = configuration;
        }

        public IConfiguration Configuration { get; }
        public static IConfiguration sConfiguration { get; set; }

        public static readonly ILoggerFactory MyLoggerFactory
            = LoggerFactory.Create(builder => { builder.AddConsole(); builder.AddDebug(); });        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddLogging(opt => { opt.AddConsole(); opt.AddDebug(); });
            services.AddMemoryCache();
            services.AddDbContext<StinDbContext>(opts => opts.UseLoggerFactory(MyLoggerFactory).EnableSensitiveDataLogging().UseSqlServer(Configuration["ConnectionString:DB"]));
            services.AddSession(option => option.IdleTimeout = TimeSpan.FromHours(23));
            services.AddHttpClient<IHttpService, HttpService>()
                .AddPolicyHandler(GetRetryPolicy());
            services.AddScoped<IEmailSender,EmailSender>();
            services.AddScoped<IFileDownloader, FileDownloader>();
            services.AddScoped<IOrderFunctions, OrderFunctions>();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromHours(23);
                    options.LoginPath = new Microsoft.AspNetCore.Http.PathString("/Account/Login");
                    options.AccessDeniedPath = new Microsoft.AspNetCore.Http.PathString("/Account/Login");
                    options.Events = new CookieAuthenticationEvents()
                    {
                        OnRedirectToLogin = (context) =>
                        {
                            var uri = context.RedirectUri;
                            UriHelper.FromAbsolute(uri, out var scheme, out var host, out var path, out var query, out var fragment);
                            string defUser = context.Request.Query["user"];
                            string defPass = context.Request.Query["password"];
                            if (!string.IsNullOrEmpty(defUser) && !string.IsNullOrEmpty(defPass))
                            {
                                query = query.Add("userName", defUser);
                                query = query.Add("password", defPass);
                                uri = UriHelper.BuildAbsolute(scheme, host, "/Account", "/SetUserFromQueryParams", query);
                            }
                            else
                                uri = UriHelper.BuildAbsolute(scheme, host, path);
                            context.Response.Redirect(uri);
                            return Task.CompletedTask;
                        }
                    };
                });
            services.AddAuthorization(opts => {
                opts.AddPolicy("ОтделПродаж", policy => {
                    policy.RequireClaim("Отдел", "Любой", "Отдел продаж");
                });
                opts.AddPolicy("Сервис", policy => {
                    policy.RequireClaim("Отдел", "Любой", "Сервис");
                });
            });
            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var supportedCultures = new[] { "ru-RU" }; //, "en-US"
            var localizationOptions = new RequestLocalizationOptions().SetDefaultCulture(supportedCultures[0])
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures);

            app.UseRequestLocalization(localizationOptions);
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "отчеты",
                    pattern: "{controller=Отчеты/Home}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                    name: "обработки",
                    pattern: "{controller=Обработки/Home}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
        static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                //.OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }
    }
}
