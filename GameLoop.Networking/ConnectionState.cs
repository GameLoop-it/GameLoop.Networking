namespace GameLoop.Networking
{
    public enum ConnectionState : byte
    {
        Created    = 1,
        Connecting = 2,
        Connected  = 3,
    }
}