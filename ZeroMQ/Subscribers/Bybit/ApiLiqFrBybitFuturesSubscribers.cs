using NetMQ;
using NetMQ.Sockets;

namespace ZeroMQ.Subscribers.Bybit
{
    public class ApiLiqFrBybitFuturesSubscribers
    {
        private readonly SubscriberSocket _liqSubscriber;
        private readonly SubscriberSocket _frSubscriber;

        public ApiLiqFrBybitFuturesSubscribers(BybitZeroMQProperties options)
        {
            string address = $"tcp://{options.PublisherIPAddress}:";

            _frSubscriber = SubPubFactory.NewSubscriber(1000);
            _liqSubscriber = SubPubFactory.NewSubscriber(1000);

            // Fr
            _frSubscriber.Connect(address + options.BybitFuturesFrCandlesApiPort);
            _frSubscriber.SubscribeToAnyTopic();

            // Liq
            _liqSubscriber.Connect(address + options.BybitFuturesLiqCandlesApiPort);
            _liqSubscriber.SubscribeToAnyTopic();
        }

        /// <summary>
        /// waits till recieved any bytes from ServerApplication
        /// </summary>
        public OpenCandle GetFrCandle()
        {
            byte[] json = _frSubscriber.ReceiveFrameBytes();
            return Utf8Json.JsonSerializer.Deserialize<OpenCandle>(json);
        }

        /// <summary>
        /// waits till recieved any bytes from ServerApplication
        /// </summary>
        public OpenCandle GetLiqCandle()
        {
            byte[] json = _liqSubscriber.ReceiveFrameBytes();
            return Utf8Json.JsonSerializer.Deserialize<OpenCandle>(json);
        }
    }
}