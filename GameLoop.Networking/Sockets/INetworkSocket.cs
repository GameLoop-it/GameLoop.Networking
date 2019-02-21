using System.Net;

namespace GameLoop.Networking.Sockets
{
    public interface INetworkSocket
    {
        void Bind(IPEndPoint endpoint);
        void Close();
        void SendTo(IPEndPoint endPoint, byte[] data);
        bool Poll(out NetworkArrivedData data);
    }
}