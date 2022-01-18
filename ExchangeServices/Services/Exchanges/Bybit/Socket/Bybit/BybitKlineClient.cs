using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DataLayer;
using DataLayer.Models;
using WatsonWebsocket;

namespace ExchangeServices.Services.Exchanges.Bybit.Socket
{
    public class BybitKlineClient : IDisposable
    {
        private const string WebsocketURL = "wss://stream.bybit.com/spot/quote/ws/v1";
        private WatsonWsClient _client;

        public WatsonWsClient Client => this._client;

        public BybitKlineClient()
        {
            _client = new WatsonWsClient(new Uri(WebsocketURL))
            {
                EnableStatistics = false
            };

            _client.ConfigureOptions((options) => { options.SetBuffer(400, 400); });
            // _client.BufferSize = 400;
        }

        public Task ConnectAsync() => _client.StartAsync();

        public async Task SubscribeToSymbolsAsync(PairInfo[] symbols)
        {
            // putting together symbols
            var str = new StringBuilder();
            foreach (var symbol in symbols)
            {
                str.Append(symbol.Symbol + ",");
            }

            // removing extra ',' at the end
            var symbolPayload = str.ToString().Substring(0, str.Length - 1);

            foreach (TimeFrameOption timeFrame in symbols[0].TimeFrameOptions)
            {
                var subRequest = "{" +
                                 $"\"topic\": \"kline_{timeFrame.TimeFrame.ToLower()}\"," +
                                 "\"event\": \"sub\"," +
                                 $"\"symbol\": \"{symbolPayload}\"," +
                                 "\"params\": { \"binary\": false}" +
                                 "}";
                // Console.WriteLine(subRequest);
                await _client.SendAsync(subRequest);
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}