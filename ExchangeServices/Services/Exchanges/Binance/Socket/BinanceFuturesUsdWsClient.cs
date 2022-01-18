using DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatsonWebsocket;

namespace ExchangeServices
{
    public class BinanceFuturesUsdTradeKlineWSClient : IDisposable
    {
        private const string URL = "wss://fstream.binance.com/stream?streams=";
        private WatsonWsClient _client;
        public WatsonWsClient Client { get => this._client; }

        public void Init(PairInfo[] symbols)
        {
            StringBuilder str = new StringBuilder();
            str.Append(URL);

            for (int i = 0; i < symbols.Length; i++)
            {
                str.Append(CreateStreams(symbols[i]));
            }

            _client = new WatsonWsClient(new Uri(str.ToString()))
            {
                EnableStatistics = false,
            };

            _client.ConfigureOptions((options) =>
            {
                options.SetBuffer(400, 400);
            });
            _client.BufferSize = 400;
        }
        public Task Connect() => _client.StartAsync();

        private string CreateStreams(PairInfo symbol)
        {
            string s = symbol.Symbol.ToLower();
            StringBuilder str = new StringBuilder();

            str.Append($"{s}@aggTrade");
            foreach (var tf in symbol.TimeFrameOptions)
            {
                str.Append($"/{s}@kline_{tf.TimeFrame.ToLower()}/");
            }
            return str.ToString();
        }

        public void Dispose() => _client.Dispose();
    }

    public class BinanceFuturesUsdDepthWSClient : IDisposable
    {
        private const string URL = "wss://fstream.binance.com/stream?streams=";
        private WatsonWsClient _client;
        public WatsonWsClient Client { get => this._client; }

        public void Init(string[] symbols)
        {
            StringBuilder str = new StringBuilder();
            str.Append(URL);

            for (int i = 0; i < symbols.Length; i++)
            {
                str.Append($"{symbols[i].ToLower()}@depth@100ms/");
            }

            _client = new WatsonWsClient(new Uri(str.ToString()))
            {
                EnableStatistics = false
            };

            //_client.ConfigureOptions((options) =>
            //{
            //    options.SetBuffer(1024 * 30, 1024 * 30);
            //});
            _client.BufferSize = 1024 * 16;
        }

        public Task Connect() => _client.StartAsync();

        public void Dispose() => _client.Dispose();
    }


    public class BinanceFuturesUsdFrLiqWSClient : IDisposable
    {
        private const string URL = "wss://fstream.binance.com/stream?streams=!markPrice@arr/!forceOrder@arr";
        private WatsonWsClient _client;
        public WatsonWsClient Client { get => this._client; }

        public void Init()
        {
            _client = new WatsonWsClient(new Uri(URL))
            {
                EnableStatistics = false
            };

            //_client.ConfigureOptions((options) =>
            //{
            //    options.SetBuffer(1024 * 30, 1024 * 30);
            //});
            _client.BufferSize = 1024 * 8;
        }

        public Task Connect() => _client.StartAsync();

        public void Dispose() => _client.Dispose();
    }
}