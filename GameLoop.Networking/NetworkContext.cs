using GameLoop.Networking.Settings;
using GameLoop.Networking.Sockets;

namespace GameLoop.Networking
{
    public class NetworkContext
    {
        public NetworkSettings       Settings      = new NetworkSettings();
        public INetworkSocketFactory SocketFactory = new NetworkSocketFactory();
    }
}