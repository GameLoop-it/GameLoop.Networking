/*
The MIT License (MIT)

Copyright (c) 2020 Emanuele Manzione

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
using GameLoop.Networking.Settings;
using NanoSockets;

namespace GameLoop.Networking.Sockets
{
    public class NanoSocket : INetworkSocket
    {
        private const int SendBufferSize     = NetworkSettings.PacketMtu;
        private const int ReceiverBufferSize = NetworkSettings.PacketMtu;

        private Socket _socket;

        public void Bind(IPEndPoint endpoint)
        {
            _socket = UDP.Create(SendBufferSize, ReceiverBufferSize);
        }

        public void Close()
        {
            throw new System.NotImplementedException();
        }

        public void SendTo(IPEndPoint endPoint, byte[] data)
        {
            throw new System.NotImplementedException();
        }

        public void SendTo(IPEndPoint endPoint, byte[] data, int offset, int length)
        {
            throw new System.NotImplementedException();
        }

        public bool Receive(out IPEndPoint endPoint, out byte[] buffer, out int receivedBytes)
        {
            throw new System.NotImplementedException();
        }
    }
}