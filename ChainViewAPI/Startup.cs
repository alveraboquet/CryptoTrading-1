using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataLayer;
using DataLayer.Models;
using Microsoft.Extensions.Options;
using DatabaseRepository;
using System.Text;
using Bitmex.NET;
using Bitfinex.Net;
using FtxApi;
using Binance.Net;
using ExchangeServices;
using Bitmex.NET.Models;
using Coinbase.Pro;
using System.Diagnostics;
using System.IO;
using System.Net;
using Microsoft.OpenApi.Models;
using ChainViewAPI.Worker;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http;
using UserRepository;
using Microsoft.EntityFrameworkCore;
using Redis;
using ZeroMQ;
using ZeroMQ.Subscribers;
using ChainViewAPI.Services;

namespace ChainViewAPI
{
    public class Startup
    {
        #region Cors Policies
        private readonly string publicPolicy = "publicPolicy";
        #endregion

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public bool GetIsSwaggerOn()
        {
            try
            {
                return Configuration.GetValue<bool>("IsSwaggerOn");
            }
            catch
            {
                return false;
            }
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            #region swagger
            if (GetIsSwaggerOn())
                services.AddSwaggerGen(c =>
                {
                    c.OperationFilter<AddRequiredHeaderParameter>();
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
                    var filePath = Path.Combine(System.AppContext.BaseDirectory, "ChainViewAPI.xml");
                    c.IncludeXmlComments(filePath);
                });
            #endregion

            #region device detection
            services.AddDetection();

            // Needed by Wangkanai Detection
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromSeconds(10);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            #endregion

            #region Redis
            var factory = new ConnectionFactory(Configuration.GetConnectionString("RedisConnection"));
            services.AddSingleton(factory);
            services.AddSingleton<ICacheService, InMemoryCacheService>();
            #endregion
            
            #region ZeroMQ
            var options = Configuration.GetSection("ZeroMQ").Get<ZeroMQ.BinanceZeroMQProperties>();
            services.AddSingleton(options);
            services.AddSingleton<ApiBinanceSubscriber>();
            services.AddSingleton<ApiBinanceFuturesUsdSubscriber>();
            services.AddSingleton<ApiLiqFrBinanceFuturesUsdSubscriber>();
            #endregion

            services.AddMemoryCache();
            //services.AddResponseCaching();
            #region From ServerApplication
            // FTX
            var ftxClient = new FtxApi.Client("_aM2z91uZOFUYbWZTELma-FLLMT1xejrRn96I1vH", "xtG8V-E07hHPWjQTlFQNPoY46qytKml2_4YqsM4D");
            services.AddSingleton(ftxClient);
            services.AddSingleton<FtxRestApi>();

            // Bitfinex
            services.AddSingleton<BitfinexClient>();

            // Binance
            services.AddSingleton<BinanceClient>();

            // Api Services
            services.AddSingleton<IBinanceServices, BinanceServices>();
            services.AddSingleton<IBinanceFuturesCoinServices, BinanceFuturesCoinServices>();
            services.AddSingleton<IBinanceFuturesUsdtServices, BinanceFuturesUsdtServices>();
            services.AddSingleton<SymbolsStartAndEndTimeProvider>();




            // Bitmex | Not Don
            var bitmexAuthorization = new BitmexAuthorization()
            {
                BitmexEnvironment = BitmexEnvironment.Test,
                Key = "_aM12z91uZ1OFUYbfasfWZTELma-FLL51awFMT1xejrRn96I1vH",
                Secret = "1565xftG8V-FE01sa7hHPWjQTl5sFSd51FQNPoY46qytKml2_4YqsM4D"
            };
            var bitmexApiService = BitmexApiService.CreateDefaultApi(bitmexAuthorization);
            services.AddSingleton<IBitmexApiService>(bitmexApiService);

            // CoinBase | Not Don
            services.AddSingleton<CoinbaseProClient>();


            services.AddSingleton<IPairInfoRepository, PairInfoService>();

            services.Configure<ChartDatabaseSettings>(
              Configuration.GetSection(nameof(ChartDatabaseSettings)));

            services.AddSingleton<IChartDatabaseSettings>(sp =>
                sp.GetRequiredService<IOptions<ChartDatabaseSettings>>().Value);

            services.AddSingleton<ICandleService, CandleRepository>();
            services.AddSingleton<IPairInfoRepository, PairInfoService>();
            #endregion

            #region CORS
            services.AddCors();
            services.AddCors((options) =>
            {
                options.AddPolicy(publicPolicy, (builder) =>
                {
                    builder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });
            #endregion

            services.Configure<ChartDatabaseSettings>(
                 Configuration.GetSection(nameof(ChartDatabaseSettings)));

            services.AddSingleton<IChartDatabaseSettings>(sp =>
                sp.GetRequiredService<IOptions<ChartDatabaseSettings>>().Value);
            services.AddControllers();

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                   ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownProxies.Add(IPAddress.Parse("127.0.10.1"));
            });

            //Workers
            services.AddHostedService<APIWorker>();
            services.AddHostedService<ClearCacheWorker>();
            services.AddHostedService<BinanceZeroMQWorker>();
            services.AddHostedService<BinanceFuturesUsdZeroMqWorker>();
            services.AddHostedService<BinanceFuturesUsdLiqFrZeroMqWorker>();

            // DbContexts
            string mySqlConnectionStr = Configuration.GetConnectionString("MySql");

            services.AddDbContext<UserContext>((options) => options.UseMySql(mySqlConnectionStr,
                ServerVersion.AutoDetect(mySqlConnectionStr)));
            services.AddScoped<IUserRepository, UserRepo>();
            services.AddScoped<ILayerRepository, LayerRepo>();
            services.AddScoped<IDrawingRepository, DrawingRepo>();

            services.AddSingleton<IPairStreamInfoRepository, PairStreamInfoService>();
            services.AddSingleton<IBinanceCollectCandles, BinanceCollectCandles>();
            services.AddSingleton<IBinanceFuturesUsdCollectCandles, BinanceFuturesUsdCollectCandles>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }


            if (GetIsSwaggerOn())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Api v1"));
            }

            app.UseCors(publicPolicy);

            app.Use(async (context, next) =>
            {
                var user = context.RequestServices.GetService<IUserRepository>();
                if (context.Request.Path.StartsWithSegments("/v1/api") &&
                    !(context.Request.Path.StartsWithSegments("/v1/api/login") ||
                    context.Request.Path.StartsWithSegments("/v1/api/ping") ||
                    context.Request.Path.StartsWithSegments("/v1/api/register") ||
                    context.Request.Path.StartsWithSegments("/v1/api/search")))
                {
                    bool isExist = false;
                    if (int.TryParse(context.Request.Headers["account-id"], out int id))
                        isExist = await user.IsExistSession(id, context.Request.Headers["account-token"]);

                    if (isExist)
                        await next();
                    else
                        context.Response.StatusCode = 401;
                }
                else
                    await next();
            });

            app.UseDetection();
            app.UseRouting();
            app.UseSession();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
