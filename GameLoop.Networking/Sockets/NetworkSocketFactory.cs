namespace GameLoop.Networking.Sockets
{
    public class NetworkSocketFactory : INetworkSocketFactory
    {
        public INetworkSocket Create()
        {
            return new NetworkSocket();
        }
    }
}