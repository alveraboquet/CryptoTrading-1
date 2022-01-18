using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using DatabaseRepository;
using DataLayer;
using ExchangeServices.Services.Exchanges.Bybit.API;
using log4net;
using log4net.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Redis;

namespace ServerApplication.Bybit
{
    public class Program
    {
        public static void Main(string[] args)
        {
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
                    var options = hostContext.Configuration.GetSection("ZeroMQ").Get<ZeroMQ.BybitZeroMQProperties>();
                    services.AddSingleton(options);
                    services.AddBybitZeroMqPublishers();
                    #endregion
                    
                    #region MongoDB
                    services.Configure<ChartDatabaseSettings>(hostContext.Configuration.GetSection(nameof(ChartDatabaseSettings)));
                    services.AddSingleton<IChartDatabaseSettings>(sp => sp.GetRequiredService<IOptions<ChartDatabaseSettings>>().Value);
                    services.AddSingleton<ICandleService, CandleRepository>();
                    services.AddSingleton<IPairInfoRepository, PairInfoService>();
                    services.AddSingleton<IPairStreamInfoRepository, PairStreamInfoService>();
                    #endregion
                    
                    #region API Services
                    services.AddSingleton<IBybitService, BybitService>();
                    services.AddSingleton<IBybitFuturesService, BybitFuturesService>();
                    #endregion
                    
                    #region Queues
                    services.AddBybitSpotQueues();
                    services.AddBybitLiqFrQueues();
                    services.AddBybitFuturesQueue();
                    #endregion

                    #region Workers
                    services.AddBybitSpotWorkers();
                    services.AddBybitFuturesWorkers();
                    #endregion
                });
    }
}