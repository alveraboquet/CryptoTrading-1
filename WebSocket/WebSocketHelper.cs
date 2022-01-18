using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocket
{
    public static class WebSocketHelper
    {
        public static string GetZeroMQAddress(string address, int port) => $"tcp://{address}:{port}";
    }
}
