using System.Threading.Tasks;
using GameLoop.Networking.Buffers;
using GameLoop.Networking.Server;

namespace GameLoop.Networking.Common.Handlers
{
    public abstract class ConcurrentMessageHandler : MessageHandler
    {
        public override bool IsConcurrent => true;
    }
}