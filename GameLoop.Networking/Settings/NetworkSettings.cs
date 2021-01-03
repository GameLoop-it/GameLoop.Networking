namespace GameLoop.Networking.Settings
{
    public class NetworkSettings
    {
        // Max Transmission Unit.
        // Rationale:
        // The minimum MTU size that an host can set is 576 bytes (for IPv4) and 1280 bytes (for IPv6).
        // We are sure that these amounts will not be fragmented on any device.
        // For modern devices (IPv6-enabled) 1280 bytes is a reasonably safe MTU.
        // At this amount we subtract the UDP + IP header: 1280 - (8 + 20).
        // Find more at:
        // https://en.wikipedia.org/wiki/Maximum_transmission_unit
        // https://en.wikipedia.org/wiki/User_Datagram_Protocol
        public const int PacketMtu = 1280 - (8 + 20);
    }
}