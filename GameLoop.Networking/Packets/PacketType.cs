namespace GameLoop.Networking.Packets
{
    public enum PacketType : byte
    {
        Command = 1,
        Unreliable = 2,
        Notify = 3,
    }
}