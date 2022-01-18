using System;
using Utf8Json;

namespace ZeroMQ.Publishers.Bybit
{
    public class BybitPublisher : IDisposable
    {
        private Publisher _tradePub;
        private Publisher _candlePub;
        private Publisher _orderbookPub;
        
        public BybitPublisher(BybitZeroMQProperties options)
        {
            _candlePub = new Publisher(options.BybitCandlePort, isLocal: options.IsPublisherLocal);
            _tradePub = new Publisher(options.BybitTradePort, isLocal: options.IsPublisherLocal);
            _orderbookPub = new Publisher(options.BybitOrderbookPort, isLocal: options.IsPublisherLocal);
            
            _tradePub.Open();
            _candlePub.Open();
            _orderbookPub.Open();
        }
        
        public void Dispose()
        {
            _tradePub.Dispose();
            _orderbookPub.Dispose();
            _candlePub.Dispose();
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
    }
}