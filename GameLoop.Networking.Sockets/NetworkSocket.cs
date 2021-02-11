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

using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace GameLoop.Networking.Transport.Sockets
{
    public sealed class NetworkSocket : INetworkSocket
    {
        private const int ReceiverBufferSize = 256 * 1024;

        private Socket   _socket;
        private EndPoint _listeningEndPoint;
        private EndPoint _receivingFromEndPoint;
        private byte[]   _receiveBuffer;

        public NetworkSocket()
        {
            _receiveBuffer = new byte[ReceiverBufferSize];
        }

        public void Bind(IPEndPoint endpoint)
        {
            _socket = InitializeSocket(endpoint.AddressFamily);
            _socket.Bind(endpoint);
            NetworkSocketUtils.SetConnectionReset(_socket);

            _listeningEndPoint = endpoint;
            _receivingFromEndPoint = new IPEndPoint(
                (_listeningEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
                    ? IPAddress.IPv6Any
                    : IPAddress.Any,
                0);
        }

        private static Socket InitializeSocket(AddressFamily addressFamily)
        {
            Socket socket;

            switch (addressFamily)
            {
                case AddressFamily.InterNetwork:
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    break;
                case AddressFamily.InterNetworkV6:
                    if (!Socket.OSSupportsIPv6) throw new Exception("IPv6 is not supported on this OS.");
                    socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                    socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName) 27, false);
                    break;
                default:
                    throw new Exception("The address family isn't supported.");
            }

            socket.Blocking          = false;
            socket.DontFragment      = true;
            socket.ReceiveBufferSize = ReceiverBufferSize;
            socket.SendBufferSize    = ReceiverBufferSize;

            return socket;
        }

        public bool Receive(out IPEndPoint endPoint, out byte[] buffer, out int receivedBytes)
        {
            endPoint = null;
            buffer   = null;

            if (_socket.Poll(0, SelectMode.SelectRead) == false)
            {
                receivedBytes = 0;
                return false;
            }

            receivedBytes = _socket.ReceiveFrom(GetReceiveBuffer(), SocketFlags.None, ref _receivingFromEndPoint);

            if (receivedBytes > 0)
            {
                //Logger.Debug($"Received {receivedBytes} bytes from {_receivingFromEndPoint}");
                endPoint = (IPEndPoint) _receivingFromEndPoint;
                buffer   = _receiveBuffer;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte[] GetReceiveBuffer()
        {
            return _receiveBuffer;
        }

        public void Close()
        {
            _socket.Close(1);
        }

        public void SendTo(IPEndPoint endPoint, byte[] data)
        {
            SendTo(endPoint, data, 0, data.Length);
        }

        public void SendTo(IPEndPoint endPoint, byte[] data, int offset, int length)
        {
            _socket.SendTo(data, offset, length, SocketFlags.None, endPoint);
        }
    }
}