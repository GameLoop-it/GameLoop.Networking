namespace GameLoop.Networking.Statistics
{
    public class NetworkSocketStatistics
    {
        public ulong BytesSent;
        public ulong BytesReceived;
        public uint  MalformedReceivedPayloads;

        public static NetworkSocketStatistics Create()
        {
            return new NetworkSocketStatistics();
        }
    }
}