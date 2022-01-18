using System;
using Utf8Json;

namespace ZeroMQ
{
    public class BinanceFuturesUsdFrLiqPublisher : IDisposable
    {
        private Publisher _liqTradePub;
        private Publisher _liqCandlePub;
        private Publisher _frCandlePub;
        private Publisher _allfundsPub;

        public BinanceFuturesUsdFrLiqPublisher(BinanceZeroMQProperties options)
        {
            _liqTradePub = new Publisher(options.BinanceFuturesUsdLiqTradePort,
                isLocal: options.IsPublisherLocal);

            _liqCandlePub = new Publisher(options.BinanceFuturesUsdLiqCandlePort,
                isLocal: options.IsPublisherLocal);

            _frCandlePub = new Publisher(options.BinanceFuturesUsdFrCandlePort,
                isLocal: options.IsPublisherLocal);

            _allfundsPub = new Publisher(options.BinanceFuturesUsdAllfundsPort,
                isLocal: options.IsPublisherLocal);


            _liqTradePub.Open();
            _liqCandlePub.Open();
            _frCandlePub.Open();
            _allfundsPub.Open();
        }

        public void PublishFrCandle(OpenCandle candle)
        {
            byte[] json = JsonSerializer.Serialize(candle);
            _frCandlePub.Publish(json);
        }

        public void PublishLiqCandle(OpenCandle candle)
        {
            byte[] json = JsonSerializer.Serialize(candle);
            _liqCandlePub.Publish(json);
        }

        public void PublishLiqTrade(Trade trade)
        {
            byte[] json = JsonSerializer.Serialize(trade);
            _liqTradePub.Publish(json);
        }

        public void PublishAllfunds(byte[] json)
        {
            this._allfundsPub.Publish(json);
        }


        public void Dispose()
        {
            _liqTradePub?.Dispose();
            _liqCandlePub?.Dispose();
            _frCandlePub?.Dispose();
            _allfundsPub?.Dispose();
        }
    }
}