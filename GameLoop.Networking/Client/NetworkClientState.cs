namespace GameLoop.Networking.Client
{
    public enum NetworkClientState : byte
    {
        Initialized,
        Connecting,
        Connected,
        Disconnecting,
        Disconnected
    }
}