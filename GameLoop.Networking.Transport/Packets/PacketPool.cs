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

using System;
using System.Collections.Generic;
using GameLoop.Utilities.Memory;

namespace GameLoop.Networking.Transport.Packets
{
    public class PacketPool : IDisposable
    {
        internal class PacketStatics
        {
            public Packet ConnectionRequestPacket;
            public Packet ConnectionAcceptedPacket;
            public Packet KeepAlivePacket;
            
            public PacketStatics(IMemoryManager memoryManager)
            {
                InitializeConnectionRequestPacket(memoryManager);
                InitializeConnectionAcceptedPacket(memoryManager);
                InitializeKeepAlivePacket(memoryManager);
            }
            
            private void InitializeConnectionRequestPacket(IMemoryManager memoryManager)
            {
                ConnectionRequestPacket = new Packet(memoryManager.Allocate(2));
                var span = ConnectionRequestPacket.Span;
                span[0] = (byte) PacketType.Command;
                span[1] = (byte) CommandType.ConnectionRequest;
            }
        
            private void InitializeConnectionAcceptedPacket(IMemoryManager memoryManager)
            {
                ConnectionAcceptedPacket = new Packet(memoryManager.Allocate(2));
                var span = ConnectionAcceptedPacket.Span;
                span[0] = (byte) PacketType.Command;
                span[1] = (byte) CommandType.ConnectionAccepted;
            }
        
            private void InitializeKeepAlivePacket(IMemoryManager memoryManager)
            {
                KeepAlivePacket = new Packet(memoryManager.Allocate(1));
                var span = KeepAlivePacket.Span;
                span[0] = (byte) PacketType.KeepAlive;
            }
        }

        internal PacketStatics Statics;

        private readonly IMemoryManager _memoryManager;
        private readonly Queue<Packet>  _packetsPool;
        
        public PacketPool(IMemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
            _packetsPool   = new Queue<Packet>();
            Statics        = new PacketStatics(memoryManager);
        }

        public Packet GetPacket(IntPtr buffer, int length)
        {
            Packet packet;
            
            if (_packetsPool.Count > 0)
            {
                packet = _packetsPool.Dequeue();
            }
            else
            {
                packet = new Packet();
            }
            
            packet.Data   = buffer;
            packet.Offset = 0;
            packet.Length = length;
            
            return packet;
        }

        public void RecyclePacket(Packet packet)
        {
            _packetsPool.Enqueue(packet);
        }

        public void Dispose()
        {
        }
    }
}