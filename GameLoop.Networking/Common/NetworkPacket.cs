namespace GameLoop.Networking.Common
{
    // A packet contains more messages.
    // Its size has to fit into PacketMTU.
    internal struct NetworkPacket
    {
        public int ProtocolId;
        public byte MessagesAmount;
    }
}