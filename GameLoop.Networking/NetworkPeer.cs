using System.Net;
using GameLoop.Networking.Settings;
using GameLoop.Networking.Sockets;

namespace GameLoop.Networking.Common
{
    public sealed class NetworkPeer
    {
        private          INetworkSocket  _socket;
        private readonly NetworkSettings _settings;

        public NetworkPeer(NetworkSettings settings)
        {
            _settings = settings;
        }

        public void Start()
        {
            _socket = new NetworkSocket();
            _socket.Bind(_settings.BindingEndpoint);
        }

        public void Close()
        {
            _socket.Close();
        }

        public void SendUnconnected(IPEndPoint endpoint, byte[] data)
        {
            _socket.SendTo(endpoint, data);
        }

        public void Update()
        {
            _socket.Receive();
        }
    }
}