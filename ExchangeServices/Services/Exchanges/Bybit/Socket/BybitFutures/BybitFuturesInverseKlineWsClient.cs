using System;
using DataLayer;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatsonWebsocket;
using DataLayer.Models;
using ExchangeServices.ExtensionMethods;

namespace ExchangeServices.Services.Exchanges.Bybit.Socket.BybitFutures
{
    public class BybitFuturesInverseKlineWsClient : IDisposable
    {
        private const string WebsocketURL = "wss://stream.bybit.com/realtime";
        private WatsonWsClient _client;

        public WatsonWsClient Client => this._client;

        public BybitFuturesInverseKlineWsClient()
        {
            _client = new WatsonWsClient(new Uri(WebsocketURL))
            {
                EnableStatistics = false
            };

            _client.ConfigureOptions((options) => { options.SetBuffer(400, 400); });
        }

        public Task ConnectAsync() => _client.StartAsync();

        public async Task SubscribeToSymbolsAsync(IEnumerable<TimeFrameOption> timeframes)
        {
            foreach (var timeframe in timeframes)
            {
                var actualTimeFrame = timeframe.TimeFrame.ToBybitPerpetualTimeframe();
                string subMessage = @"{""op"":""subscribe"",""args"":[""klineV2." + actualTimeFrame + @".*""]}";
                // string subMessage = @"{""op"":""subscribe"",""args"":[""klineV2." + actualTimeFrame + @".BTCUSD""]}";
                await this._client.SendAsync(subMessage);
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
