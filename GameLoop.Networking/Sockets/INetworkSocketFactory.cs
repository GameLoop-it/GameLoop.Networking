namespace GameLoop.Networking.Sockets
{
    public interface INetworkSocketFactory
    {
        INetworkSocket Create();
    }
}