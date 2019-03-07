using GameLoop.Networking.Buffers;

namespace GameLoop.Networking.Common
{
    public struct NetworkHeader
    {
        public const int HeaderSize = 4;
        
        public bool IsData;
        public bool IsConnection;
        public bool IsDisconnection;
        public bool IsCompressed;
        public bool IsEncrypted;
        public ushort Tag;
        public ushort Length;

        public static NetworkHeader ReadHeader(ref NetworkReader reader)
        {
            var header = default(NetworkHeader);

            header.IsData = reader.ReadBool();
            header.IsConnection = reader.ReadBool();
            header.IsDisconnection = reader.ReadBool();
            header.IsCompressed = reader.ReadBool();
            header.IsEncrypted = reader.ReadBool();
            header.Tag = reader.ReadUShort();
            header.Length = reader.ReadUShort(11);

            return header;
        }

        public static void WriteHeader(ref NetworkWriter writer, NetworkHeader header)
        {
            writer.Write(header.IsData);
            writer.Write(header.IsConnection);
            writer.Write(header.IsDisconnection);
            writer.Write(header.IsCompressed);
            writer.Write(header.IsEncrypted);
            writer.Write(header.Tag);
            writer.Write(header.Length, 11);
        }
    }
}