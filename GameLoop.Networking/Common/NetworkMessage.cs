using GameLoop.Networking.Buffers;
using GameLoop.Networking.Memory;

namespace GameLoop.Networking.Common
{
    public struct NetworkMessage
    {
        public NetworkHeader Header;
        public NetworkWriter Writer;
        
        internal static NetworkMessage CreateConnectionMessage(IMemoryPool pool)
        {
            var header = default(NetworkHeader);
            header.IsConnection = true;
            header.IsData = false;
            header.IsDisconnection = false;
            header.IsCompressed = false;
            header.IsEncrypted = false;
            header.Tag = 0;
            header.Length = 0;

            var writer = default(NetworkWriter);
            writer.Initialize(pool, NetworkHeader.HeaderSize);
            NetworkHeader.WriteHeader(ref writer, header);

            var message = default(NetworkMessage);
            message.Header = header;
            message.Writer = writer;

            return message;
        }
        
        internal static NetworkMessage CreateDisconnectionMessage(IMemoryPool pool)
        {
            var header = default(NetworkHeader);
            header.IsConnection = false;
            header.IsData = false;
            header.IsDisconnection = true;
            header.IsCompressed = false;
            header.IsEncrypted = false;
            header.Tag = 0;
            header.Length = 0;

            var writer = default(NetworkWriter);
            writer.Initialize(pool, NetworkHeader.HeaderSize);
            NetworkHeader.WriteHeader(ref writer, header);

            var message = default(NetworkMessage);
            message.Header = header;
            message.Writer = writer;

            return message;
        }
        
        public static NetworkMessage CreateDataMessage(IMemoryPool pool, int payloadSize, ushort tag)
        {
            var header = default(NetworkHeader);
            header.IsConnection = false;
            header.IsData = true;
            header.IsDisconnection = false;
            header.IsCompressed = false;
            header.IsEncrypted = false;
            header.Tag = tag;
            header.Length = (ushort)payloadSize;

            var writer = default(NetworkWriter);
            writer.Initialize(pool, NetworkHeader.HeaderSize);
            NetworkHeader.WriteHeader(ref writer, header);

            var message = default(NetworkMessage);
            message.Header = header;
            message.Writer = writer;

            return message;
        }
    }
}