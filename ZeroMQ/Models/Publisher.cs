using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace ZeroMQ
{
    internal class Publisher : IDisposable
    {
        public bool IsOpened { get; private set; }
        public readonly int PORT;
        private PublisherSocket _server;
        private string _address;
        public Publisher(int port, int sendHighWatermark = 10000, bool isLocal = true)
        {
            this.PORT = port;

            if (isLocal)
                this._address = $"tcp://localhost:{this.PORT}";
            else
                this._address = $"tcp://*:{this.PORT}";

            _server = new PublisherSocket();
            _server.Options.SendHighWatermark = sendHighWatermark;
        }

        public void Open()
        {
            _server.Bind(_address);
            this.IsOpened = true;
        }

        public void Close()
        {
            _server.Close();
            _server.Disconnect(_address);
            this.IsOpened = false;
        }

        public void Dispose()
        {
            this._server?.Dispose();
        }

        public void Publish(byte[] msg) => _server.SendFrame(msg);
    }
}