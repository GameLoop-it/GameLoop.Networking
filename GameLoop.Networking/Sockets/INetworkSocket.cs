using System.Net;

namespace GameLoop.Networking.Sockets
{
    public interface INetworkSocket
    {
        void Bind(IPEndPoint endpoint);
        
        void Close();
        
        void SendTo(IPEndPoint endPoint, byte[] data);
        void SendTo(IPEndPoint endPoint, byte[] data, int offset, int length);

        bool Receive(out IPEndPoint endPoint, out byte[] buffer, out int receivedBytes);
    }
}