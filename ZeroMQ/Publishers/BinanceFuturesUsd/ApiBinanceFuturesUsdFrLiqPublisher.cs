using System;
using Utf8Json;

namespace ZeroMQ
{
    public class ApiBinanceFuturesUsdFrLiqPublisher : IDisposable
    {
        private readonly Publisher _frCandlePublisher;
        private readonly Publisher _liqCandlePublisher;

        public ApiBinanceFuturesUsdFrLiqPublisher(BinanceZeroMQProperties options)
        {
            _frCandlePublisher = new Publisher(options.BinanceFuturesUsdFrCandlesApiPort,
                isLocal: options.IsPublisherLocal);

            _liqCandlePublisher = new Publisher(options.BinanceFuturesUsdLiqCandlesApiPort,
                isLocal: options.IsPublisherLocal);

            _liqCandlePublisher.Open();
            _frCandlePublisher.Open();
        }

        public void PublishFrCandle(OpenCandle candle)
        {
            byte[] json = JsonSerializer.Serialize(candle);
            _frCandlePublisher.Publish(json);
        }
        public void PublishLiqCandle(OpenCandle candle)
        {
            byte[] json = JsonSerializer.Serialize(candle);
            _liqCandlePublisher.Publish(json);
        }

        public void Dispose()
        {
            _frCandlePublisher?.Dispose();
            _liqCandlePublisher?.Dispose();
        }
    }
}