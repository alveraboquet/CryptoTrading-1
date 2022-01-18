using ZeroMQ;
using System;
using System.Collections.Generic;
using System.IO;
using Binance.Net;
using Binance.Net.Objects.Spot;
using Bitfinex.Net;
using Bitfinex.Net.Objects;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Logging;
using FtxApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ExchangeServices;
using ServerApplication.Workers;
using Bitmex.NET;
using Bitmex.NET.Models;
using Coinbase.Pro;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Redis;
using DatabaseRepository;
using Microsoft.Extensions.Options;
using DataLayer;
using log4net;
using System.Xml;
using System.Reflection;
using log4net.Config;

namespace ServerApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Configuring Binance Api, it is not necessary for our use, just for doing trading.
            BinanceClient.SetDefaultOptions(new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials("ZqpyeQVX807KvXkiPPS3aPQvkyrgxvNTOgj3fMXmCJsQxGo7HCgQSDSVqDmaUyXB",
                    "HFwphLTj0JXWPrAwMtQ4zRKoQVTZuYUfRA05WtwqLRx49pmmhYCQMqgbzHsOkzig"),
                LogVerbosity = LogVerbosity.Debug,
                //LogWriters = new List<TextWriter> { Console.Out }
            });

            // Configuring Bitfinex Api
            BitfinexClient.SetDefaultOptions(new BitfinexClientOptions()
            {
                ApiCredentials = new ApiCredentials("nQVIGNbpoJMaMPNBzvDPqsGqhUkD3EndigDV5UoZtlL",
                    "tcDSm3NwceaEL8jZsZlZTBczuibJPVWiSEMIQkPvK0n"),
                LogVerbosity = LogVerbosity.Debug,
                LogWriters = new List<TextWriter> { Console.Out }
            });

            #region log4net
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.Load(File.OpenRead("log4net.config"));

            XmlConfigurator.Configure(logRepository, log4netConfig["log4net"]);
            #endregion

            CreateHostBuilder(args).Build().Run();
        }
        

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddMemoryCache();
                    #region Redis
                    var factory = new ConnectionFactory(hostContext.Configuration.GetConnectionString("RedisConnection"));
                    services.AddSingleton(factory);
                    services.AddSingleton<ICacheService, InMemoryCacheService>();
                    #endregion

                    #region ZeroMQ
                    var options = hostContext.Configuration.GetSection("ZeroMQ").Get<ZeroMQ.BinanceZeroMQProperties>();
                    services.AddSingleton(options);

                    services.AddBinanceZeroMqPublishers();
                    services.AddBinanceFuturesUsdZeroMqPublishers();
                    #endregion

                    #region MongoDB
                    services.Configure<ChartDatabaseSettings>(hostContext.Configuration.GetSection(nameof(ChartDatabaseSettings)));
                    services.AddSingleton<IChartDatabaseSettings>(sp =>
                        sp.GetRequiredService<IOptions<ChartDatabaseSettings>>().Value);

                    services.AddSingleton<ICandleService, CandleRepository>();

                    services.AddSingleton<IPairInfoRepository, PairInfoService>();
                    services.AddSingleton<IPairStreamInfoRepository, PairStreamInfoService>();
                    #endregion

                    #region Coin Base
                    services.AddSingleton<CoinbaseProClient>();
                    #endregion

                    #region Bitstamp
                    var bitmexAuthorization = new BitmexAuthorization()
                    {
                        BitmexEnvironment = BitmexEnvironment.Test,
                        Key = "your api key",
                        Secret = "your api secret"
                    };
                    var bitmexApiService = BitmexApiService.CreateDefaultApi(bitmexAuthorization);
                    services.AddSingleton<IBitmexApiService>(bitmexApiService);
                    #endregion
                    
                    #region FTX
                    var ftxClient = new FtxApi.Client("_aM2z91uZOFUYbWZTELma-FLLMT1xejrRn96I1vH", "xtG8V-E07hHPWjQTlFQNPoY46qytKml2_4YqsM4D");
                    services.AddSingleton<FtxApi.Client>(ftxClient);
                    services.AddSingleton<FtxRestApi>();
                    #endregion

                    #region Bitfinex
                    services.AddSingleton<BitfinexClient>();
                    #endregion

                    #region Binance
                    services.AddSingleton<BinanceClient>();
                    #endregion

                    #region API Services
                    services.AddSingleton<IBinanceServices, BinanceServices>();
                    services.AddSingleton<IBinanceFuturesCoinServices, BinanceFuturesCoinServices>();
                    services.AddSingleton<IBinanceFuturesUsdtServices, BinanceFuturesUsdtServices>();
                    #endregion

                    #region Queues

                    // Binance
                    services.AddBinanceQueues();

                    // Binance Futures Usd
                    services.AddBinanceFuturesUsdQueues();

                    #endregion
                    
                    #region Workers
                    services.AddBinanceWorkers();
                    services.AddBinanceFuturesUsdWorkers();
                    services.AddBinanceFuturesUsdLiqFrWorkers();
                    #endregion
                });
    }
}
