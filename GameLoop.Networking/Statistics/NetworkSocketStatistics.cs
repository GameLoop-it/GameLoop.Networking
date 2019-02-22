namespace GameLoop.Networking.Statistics
{
    public struct NetworkSocketStatistics
    {
        public ulong BytesSent;
        public ulong BytesReceived;

        public static NetworkSocketStatistics Create()
        {
            return new NetworkSocketStatistics();
        }
    }
}