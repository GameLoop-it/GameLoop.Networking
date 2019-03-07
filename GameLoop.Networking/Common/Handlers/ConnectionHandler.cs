using GameLoop.Networking.Server;

namespace GameLoop.Networking.Common.Handlers
{
    public abstract class ConnectionHandler
    {
        public abstract void Handle(NetworkConnection connection);
    }
}