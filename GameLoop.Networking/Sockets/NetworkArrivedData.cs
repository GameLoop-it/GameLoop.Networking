using System.Net;

namespace GameLoop.Networking.Sockets
{
    public struct NetworkArrivedData
    {
        public IPEndPoint EndPoint;
        public byte[] Data;
    }
}