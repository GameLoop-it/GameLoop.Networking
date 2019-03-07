using GameLoop.Networking.Buffers;
using GameLoop.Networking.Server;

namespace GameLoop.Networking.Common.Handlers
{
    public abstract class MessageHandler
    {
        public virtual bool IsConcurrent => false;
        
        public abstract void Handle(NetworkConnection connection, NetworkReader reader);
    }
}