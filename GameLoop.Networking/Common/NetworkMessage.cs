using GameLoop.Networking.Buffers;
using GameLoop.Networking.Memory;

namespace GameLoop.Networking.Common
{
    public static class NetworkMessage
    {
        internal static NetworkWriter CreateConnectionMessage(IMemoryPool pool)
        {
            var header = default(NetworkHeader);
            header.IsConnection = true;
            header.IsData = false;
            header.IsDisconnection = false;

            var writer = default(NetworkWriter);
            writer.Initialize(pool, NetworkHeader.HeaderSize);
            NetworkHeader.WriteHeader(ref writer, header);

            return writer;
        }
        
        internal static NetworkWriter CreateDisconnectionMessage(IMemoryPool pool)
        {
            var header = default(NetworkHeader);
            header.IsConnection = false;
            header.IsData = false;
            header.IsDisconnection = true;

            var writer = default(NetworkWriter);
            writer.Initialize(pool, NetworkHeader.HeaderSize);
            NetworkHeader.WriteHeader(ref writer, header);

            return writer;
        }
        
        public static NetworkWriter CreateDataMessage(IMemoryPool pool, int payloadSize)
        {
            var header = default(NetworkHeader);
            header.IsConnection = false;
            header.IsData = true;
            header.IsDisconnection = false;

            var writer = default(NetworkWriter);
            writer.Initialize(pool, NetworkHeader.HeaderSize + payloadSize);
            NetworkHeader.WriteHeader(ref writer, header);

            return writer;
        }
    }
}