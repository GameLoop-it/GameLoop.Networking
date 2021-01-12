/*
The MIT License (MIT)

Copyright (c) 2020 Emanuele Manzione, Fredrik Holmstrom

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System.Net;

namespace GameLoop.Networking.Transport.Settings
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

        public const int AckMaskBits = sizeof(ulong) * 8;

        public IPEndPoint BindingEndpoint;
        public int        MaxConnectionsAllowed     = 32;
        public int        MaxConnectionsAttempts    = 10;
        public double     ConnectionAttemptInterval = .25f;
        public double     ConnectionTimeout         = 5f;
        public double     DisconnectionIdleTime     = 2f;
        public double     KeepAliveInterval         = 1f;
        public int        SequenceNumberBytes       = 2;
        public int        SendWindowSize            = 512;
        public double     SimulatedLoss             = 0;
    }
}