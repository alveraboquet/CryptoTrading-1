using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ.Sockets;

namespace ZeroMQ
{
    public static class SubPubFactory
    {
        public static SubscriberSocket NewSubscriber(int receiveHighWatermark = 1000)
        {
            var subscriber = new SubscriberSocket();
            subscriber.Options.ReceiveHighWatermark = receiveHighWatermark;
            return subscriber;
        }
    }
}
