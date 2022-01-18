using DataLayer;
using ExchangeModels.BybitFutures;
using ExchangeServices.Services.Exchanges.Bybit.Socket.BybitFutures;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using ServerApplication.Bybit.Caching;
using ServerApplication.Bybit.Queues.BybitFutures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerApplication.Bybit.Models;
using Utf8Json;
using Utilities;
using WatsonWebsocket;

namespace ServerApplication.Bybit.StreamingServices.BybitFutures
{
    public class BybitUsdtFuturesKlineStreaming
    {
        #region Private Variables

        private List<PairInfo> _usdtPairs;
        private const string Exchange = ApplicationValues.BybitFuturesName;
        private readonly IMemoryCache _cache;
        private readonly ILog _logger;

        private readonly BybitFuturesUsdtKlineWsClient _usdtClient;

        private readonly BybitFuturesKlineMessageQueue _receivedKline;
        #endregion

        public BybitUsdtFuturesKlineStreaming(IMemoryCache cache, BybitFuturesKlineMessageQueue receivedKline)
        {
            this._cache = cache;
            _usdtClient = new();
            _usdtPairs = new List<PairInfo>();
            _logger = LogManager.GetLogger(typeof(BybitUsdtFuturesKlineStreaming));
            this._receivedKline = receivedKline;
        }

        public void Connect(List<PairInfo> usdtPairs)
        {
            if (!usdtPairs.Any())
                return;

            this._usdtPairs = usdtPairs;

            _usdtClient.Client.MessageReceived += OnMessageReceived;
            _usdtClient.Client.ServerConnected += OnConnected;
            _usdtClient.Client.ServerDisconnected += OnDisconnected;
            _usdtClient.ConnectAsync().Wait();
        }

        private void OnDisconnected(object sender, EventArgs e)
        {
            _logger.Info("Disconnected");
            _cache.SetKlineSymbolIsStreaming(Exchange, _usdtPairs.Select(p => p.Symbol).ToArray(), false);
            Dispose();
        }

        private async void OnConnected(object sender, EventArgs e)
        {
            _logger.Info($"Bybit Futures USDT Client Connected with {_usdtPairs.Count} symbols.");
            await _usdtClient.SubscribeToSymbolsAsync(_usdtPairs.ToArray());
            _cache.SetKlineSymbolIsStreaming(Exchange, _usdtPairs.Select(p => p.Symbol).ToArray(), true);
        }
        
        private void OnMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            try
            {
                if (!args.Data[2].Equals(0x73)) // s | if it is not the 'success' message
                {
                    var klines = JsonSerializer.Deserialize<BybitMessage<BybitFuturesCandle[]>>(args.Data);

                    if (klines.Topic.EndsWith("USDT"))
                    {
                        foreach (var kline in klines.Data)
                            _receivedKline.Enqueue(new BybitFuturesExtendedCandle(kline, klines.Topic));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        public void Dispose()
        {
            _usdtClient?.Dispose();
        }
    }
}
