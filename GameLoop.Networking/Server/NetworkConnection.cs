using System.Net;

namespace GameLoop.Networking.Server
{
    public class NetworkConnection
    {
        public IPEndPoint RemoteEndpoint;
        public int UniqueId;

        public static NetworkConnection Create(IPEndPoint endpoint)
        {
            var connection = new NetworkConnection();
            connection.RemoteEndpoint = endpoint;

            return connection;
        }

        private NetworkConnection()
        {
            
        }
    }
}