namespace GameLoop.Networking.Packets
{
    public enum CommandType : byte
    {
        // From client to server when connecting for the first time.
        ConnectionRequest = 1,
        
        // From server to client when the connect request has been accepted.
        ConnectionAccepted = 2,
    }
}