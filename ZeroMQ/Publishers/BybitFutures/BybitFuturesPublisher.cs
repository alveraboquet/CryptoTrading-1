using System;
using Utf8Json;

namespace ZeroMQ.Publishers.BybitFutures
{
    public class BybitFuturesPublisher : IDisposable
    {
        private Publisher _tradePub;
        private Publisher _candlePub;
        private Publisher _orderbookPub;

        public BybitFuturesPublisher(BybitZeroMQProperties options)
        {
            _tradePub = new Publisher(options.BybitFuturesTradePort,
                isLocal: options.IsPublisherLocal);

            _candlePub = new Publisher(options.BybitFuturesCandlePort,
                isLocal: options.IsPublisherLocal);

            _orderbookPub = new Publisher(options.BybitFuturesOrderbookPort,
                isLocal: options.IsPublisherLocal);

            _tradePub.Open();
            _candlePub.Open();
            _orderbookPub.Open();
        }
        
        public void PublishCandle(OpenCandle candle)
        {
            byte[] json = JsonSerializer.Serialize(candle);
            _candlePub.Publish(json);
        }

        public void PublishCandle(DataLayer.Candle candle)
        {
            this.PublishCandle((OpenCandle)candle);
        }

        public void PublishOrderbook(ZeroMQ.OrderBook orderbook)
        {
            byte[] json = JsonSerializer.Serialize(orderbook);
            _orderbookPub.Publish(json);
        }

        public void PublishTrade(Trade trade)
        {
            byte[] json = JsonSerializer.Serialize(trade);
            _tradePub.Publish(json);
        }

        public void Dispose()
        {
            _tradePub?.Dispose();
            _candlePub?.Dispose();
            _orderbookPub?.Dispose();
        }
    }
}