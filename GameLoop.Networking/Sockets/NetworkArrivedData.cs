using System.Net;

namespace GameLoop.Networking.Sockets
{
    public struct NetworkArrivedData
    {
        public EndPoint EndPoint;
        public byte[] Data;
    }
}