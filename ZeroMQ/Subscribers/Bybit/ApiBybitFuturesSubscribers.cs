using NetMQ;
using NetMQ.Sockets;

namespace ZeroMQ.Subscribers.Bybit
{
    public class ApiBybitFuturesSubscribers
    {
        private readonly SubscriberSocket _footprintSubscriber;
        private readonly SubscriberSocket _heatmapSubscriber;
        private readonly SubscriberSocket _candleSubscriber;

        public ApiBybitFuturesSubscribers(BybitZeroMQProperties options)
        {
            _candleSubscriber = SubPubFactory.NewSubscriber(1000);
            _heatmapSubscriber = SubPubFactory.NewSubscriber(1000);
            _footprintSubscriber = SubPubFactory.NewSubscriber(1000);
            string address = $"tcp://{options.PublisherIPAddress}:";

            // candle
            _candleSubscriber.Connect(address + options.BybitFuturesCandleApiPort);
            _candleSubscriber.SubscribeToAnyTopic();

            // footprint
            _footprintSubscriber.Connect(address + options.BybitFuturesFootprintApiPort);
            _footprintSubscriber.SubscribeToAnyTopic();

            // heatmap
            _heatmapSubscriber.Connect(address + options.BybitFuturesHeatmapApiPort);
            _heatmapSubscriber.SubscribeToAnyTopic();
        }
        
        public OpenCandle GetCandle()
        {
            byte[] json = _candleSubscriber.ReceiveFrameBytes();
            return Utf8Json.JsonSerializer.Deserialize<OpenCandle>(json);
        }
        
        public OpenFootprint GetFootprint()
        {
            byte[] json = _footprintSubscriber.ReceiveFrameBytes();
            return Utf8Json.JsonSerializer.Deserialize<OpenFootprint>(json);
        }
        
        public OpenHeatmap GetHeatmap()
        {
            byte[] json = _heatmapSubscriber.ReceiveFrameBytes();
            return Utf8Json.JsonSerializer.Deserialize<OpenHeatmap>(json);
        }
    }
}