namespace GameLoop.Networking.Packets
{
    public class PacketPool
    {
        public readonly byte[] ConnectionRequestPacket = {(byte) PacketType.Command, (byte) CommandType.ConnectionRequest };
        public readonly byte[] ConnectionAcceptedPacket = {(byte) PacketType.Command, (byte) CommandType.ConnectionAccepted };
        public readonly byte[] KeepAlivePacket = {(byte) PacketType.KeepAlive};
    }
}