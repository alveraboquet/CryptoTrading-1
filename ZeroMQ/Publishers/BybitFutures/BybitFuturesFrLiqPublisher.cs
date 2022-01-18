using System;
using System.Collections.Generic;
using Utf8Json;

namespace ZeroMQ.Publishers.BybitFutures
{
    public class BybitFuturesFrLiqPublisher : IDisposable
    {
        private Publisher _liqTradePub;
        private Publisher _liqCandlePub;
        private Publisher _frCandlePub;
        private Publisher _allfundsPub;

        public BybitFuturesFrLiqPublisher(BybitZeroMQProperties options)
        {
            _liqTradePub = new Publisher(options.BybitFuturesLiqTradePort,
                isLocal: options.IsPublisherLocal);

            _liqCandlePub = new Publisher(options.BybitFuturesLiqCandlePort,
                isLocal: options.IsPublisherLocal);

            _frCandlePub = new Publisher(options.BybitFuturesFrCandlePort,
                isLocal: options.IsPublisherLocal);

            _allfundsPub = new Publisher(options.BybitFuturesAllfundsPort,
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

        public void PublishAllfunds(Dictionary<string, decimal> allfunds)
        {
            byte[] json = JsonSerializer.Serialize(allfunds);
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