namespace GameLoop.Networking.Statistics
{
    public struct NetworkSocketStatistics
    {
        public ulong BytesSent;
        public ulong BytesReceived;
        public uint MalformedReceivedPayloads;

        public static NetworkSocketStatistics Create()
        {
            return default;
        }
    }
}