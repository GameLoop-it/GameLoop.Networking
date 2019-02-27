using GameLoop.Networking.Buffers;

namespace GameLoop.Networking.Common
{
    public struct NetworkHeader
    {
        public const int HeaderSize = 1;
        
        public bool IsData;
        public bool IsConnection;
        public bool IsDisconnection;

        public static NetworkHeader ReadHeader(ref NetworkReader reader)
        {
            var header = default(NetworkHeader);

            return header;
        }

        public static void WriteHeader(ref NetworkWriter writer, NetworkHeader header)
        {
            writer.Write(header.IsData);
            writer.Write(header.IsConnection);
            writer.Write(header.IsDisconnection);
        }
    }
}