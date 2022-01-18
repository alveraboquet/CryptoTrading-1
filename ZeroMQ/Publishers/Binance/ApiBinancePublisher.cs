using DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;

namespace ZeroMQ
{
    public class ApiBinancePublisher : IDisposable
    {
        private readonly Publisher _candlePublisher;
        private readonly Publisher _heatmapPublisher;
        private readonly Publisher _footprintPublisher;

        public ApiBinancePublisher(BinanceZeroMQProperties options)
        {
            _candlePublisher = new Publisher(options.BinanceCandleApiPort, isLocal: options.IsPublisherLocal);
            _footprintPublisher = new Publisher(options.BinanceFootprintApiPort, isLocal: options.IsPublisherLocal);
            _heatmapPublisher = new Publisher(options.BinanceHeatmapApiPort, isLocal: options.IsPublisherLocal);

            _candlePublisher.Open();
            _footprintPublisher.Open();
            _heatmapPublisher.Open();
        }

        public void Dispose()
        {
            _candlePublisher.Dispose();
            _heatmapPublisher.Dispose();
            _footprintPublisher.Dispose();
        }

        public void PublishCandle(OpenCandle candle)
        {
            byte[] json = JsonSerializer.Serialize(candle);
            _candlePublisher.Publish(json);
        }
        public void PublishCandle(DataLayer.Candle candle) => this.PublishCandle((OpenCandle)candle);

        public void PublishHeatmap(OpenHeatmap heatmap)
        {
            byte[] json = JsonSerializer.Serialize(heatmap);
            _heatmapPublisher.Publish(json);
        }

        public void PublishFootprint(OpenFootprint footprint)
        {
            byte[] json = JsonSerializer.Serialize(footprint);
            _footprintPublisher.Publish(json);
        }
    }
}