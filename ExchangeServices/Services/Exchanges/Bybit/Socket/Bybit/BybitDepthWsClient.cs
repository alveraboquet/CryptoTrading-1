using DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatsonWebsocket;

namespace ExchangeServices.Services.Exchanges.Bybit.Socket
{
    public class BybitDepthWsClient : IDisposable
    {
        public const string URL = "wss://stream.bybit.com/spot/quote/ws/v1";
        private WatsonWsClient _client;
        public BybitDepthWsClient()
        {
            _client = new WatsonWsClient(new Uri(URL))
            {
                EnableStatistics = false
            };

            _client.ConfigureOptions((options) =>
            {
                options.SetBuffer(400, 400);
            });
            // _client.BufferSize = 400;
        }
        public WatsonWsClient Client { get => this._client; }

        public Task Connect() => _client.StartAsync();

        public async Task SubToAllSymbols(PairInfo[] symbols)
        {
            StringBuilder str = new StringBuilder();

            for (int i = 0; i < symbols.Length; i++)
                str.Append(symbols[i].Symbol + ",");

            string subMessage =
                    "{" +
                        "\"topic\": \"diffDepth\"," +
                        "\"event\": \"sub\"," +
                        $"\"symbol\": \"{str}\"," +
                        "\"params\": {" +
                            "\"binary\": false" +
                        "}" +
                    "}";

            await this._client.SendAsync(subMessage);
        }

        public void Dispose()
        {
            this._client.Dispose();
        }
    }
}
