using GameLoop.Networking.Server;

namespace GameLoop.Networking.Common.Handlers
{
    public abstract class DisconnectionHandler
    {
        public abstract void Handle(NetworkConnection connection);
    }
}