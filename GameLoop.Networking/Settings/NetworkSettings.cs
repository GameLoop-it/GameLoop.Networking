namespace GameLoop.Networking.Settings
{
    public static class NetworkSettings
    {
        // Max Transmission Unit.
        // MTU on Ethernet is 1500 bytes. 
        // - IP Header: 20 bytes
        // - UDP Header: 8 bytes
        // So they are 28 bytes just for packet's header. Let's say 32 bytes.
        // Our payload MTU is: 1500 - 32 = 1468 bytes.
        public const int PacketMtu = 1500 - 32;
    }
}