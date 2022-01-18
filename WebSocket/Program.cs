using log4net;
using log4net.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetCoreServer;
using Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml;
using UserRepository;
using WebSocket.Workers;
using ZeroMQ;

namespace WebSocket
{
    public class Program
    {
        private static ILog _logger = LogManager.GetLogger(typeof(Program));
        public static void Main(string[] args)
        {
            #region log4net
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.Load(File.OpenRead("log4net.config"));

            XmlConfigurator.Configure(logRepository, log4netConfig["log4net"]);
            #endregion

            int port = GetPort();
            var host = CreateHostBuilder(args, port).Build();
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args, int port) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    #region MySql
                    DbContextOptionsBuilder<UserContext> builder = new DbContextOptionsBuilder<UserContext>();

                    string mySqlConnectionStr = hostContext.Configuration.GetConnectionString("MySql");

                    builder.UseMySql(mySqlConnectionStr, ServerVersion.AutoDetect(mySqlConnectionStr));
                    var dbContext = new UserContext(builder.Options);

                    services.AddSingleton(dbContext);
                    IUserRepository usersRepository = new UserRepo(dbContext);
                    #endregion

                    #region Redis
                    var factory = new ConnectionFactory(hostContext.Configuration.GetConnectionString("RedisConnection"));
                    ICacheService _redis = new InMemoryCacheService(factory);

                    services.AddSingleton(factory);
                    services.AddSingleton(_redis);
                    #endregion

                    #region ZeroMQ
                    var binanceOptions = hostContext.Configuration.GetSection("BinanceZeroMQ").Get<BinanceZeroMQProperties>();
                    var bybitOptions = hostContext.Configuration.GetSection("BybitZeroMQ").Get<BybitZeroMQProperties>();
                    services.AddSingleton(binanceOptions);
                    services.AddSingleton(bybitOptions);
                    #endregion

                    #region Start SocketServer
                    _logger.Info($"WebSocket server port: {port}\n");

                    string certificatePass = hostContext.Configuration.GetValue<string>("CertificatePass");
                    var context = new SslContext(SslProtocols.Tls12, new X509Certificate2("server.pfx", certificatePass));

                    var server = new SocketServer(context, IPAddress.Any, port, _redis, usersRepository);

                    // Start the server
                    _logger.Info("Starting server...");
                    server.Start();

                    services.AddSingleton(server);
                    #endregion

                    // Add socket publishers
                    services.AddHostedService<BinanceTradeWorker>();
                    services.AddHostedService<BinanceOrderbookWorker>();
                    services.AddHostedService<BinanceCandleWorker>();


                    services.AddHostedService<BinanceFuturesUsdTradeWorker>();
                    services.AddHostedService<BinanceFuturesUsdOrderbookWorker>();
                    services.AddHostedService<BinanceFuturesUsdCandleWorker>();

                    services.AddHostedService<BinanceFuturesUsdAllfundsWorker>();
                    services.AddHostedService<BinanceFuturesUsdFrCandlePort>();
                    services.AddHostedService<BinanceFuturesUsdLiqTradeWorker>();
                    services.AddHostedService<BinanceFuturesUsdLiqCandleWorker>();

                    services.AddBybitWorkers();
                });

        public static int GetPort()
        {
            int port;
            while (true)
            {
                try
                {
                    Console.Write("Please enter the port: ");
                    port = int.Parse(Console.ReadLine());
                    if (port <= 65535 && port >= 0)
                    {
                        break;
                    }
                }
                catch { }
                Console.WriteLine($"Invalid port.");
            }
            return port;
        }
    }
}
