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
using GameLoop.Networking.Sockets;
using GameLoop.Networking.Transport.Buffers;
using GameLoop.Networking.Transport.Packets;
using GameLoop.Networking.Transport.Settings;
using GameLoop.Utilities.Asserts;
using GameLoop.Utilities.Logs;
using GameLoop.Utilities.Memory;

#if ENABLE_TRANSPORT_LOGS
using GameLoop.Utilities.Logs;
#endif

namespace GameLoop.Networking.Transport
{
    public partial class NetworkPeer
    {
        public void SendUnconnected(NetworkAddress address, MemoryBlock data)
        {
            SendUnconnected(address, data.Buffer, data.Size);
        }
        
        public void SendUnconnected(NetworkAddress address, IntPtr data, int length)
        {
            _socket.SendTo(address, data, length);
        }
        
        private void Send(NetworkConnection connection, MemoryBlock data)
        {
            Send(connection, data.Buffer, data.Size);
        }
        
        private void Send(NetworkConnection connection, Packet data)
        {
            Send(connection, data.Data, data.Length);
        }

        private void Send(NetworkConnection connection, IntPtr data, int length)
        {
            Assert.Check(connection.ConnectionState < ConnectionState.Disconnected);

            connection.LastSentPacketTime = _timer.GetElapsedSeconds();
            _socket.SendTo(connection.RemoteAddress, data, length);
            connection.Statistics.IncreaseBytesSent(length);
        }
        
        private void SendCommand(NetworkConnection connection, CommandType command, byte commandData = 0)
        {
            Assert.Check(connection.ConnectionState < ConnectionState.Disconnected);

#if ENABLE_TRANSPORT_LOGS
            Logger.DebugInfo($"Sending command {command} to {connection}");
#endif

            // Check if the command is a pre-allocated one.
            switch (command)
            {
                case CommandType.ConnectionRequest:
                    Send(connection, _context.PacketPool.Statics.ConnectionRequestPacket);
                    return;
                case CommandType.ConnectionAccepted:
                    Send(connection, _context.PacketPool.Statics.ConnectionAcceptedPacket);
                    return;
            }

            var size = 2;
            if (commandData != 0)
                size = 3;

            var buffer     = _context.MemoryManager.Allocate(size);
            var bufferSpan = buffer.Span;
            bufferSpan[0] = (byte) PacketType.Command;
            bufferSpan[1] = (byte) command;

            if (commandData != 0)
                bufferSpan[2] = commandData;

            Send(connection, buffer);

            _context.MemoryManager.Free(buffer);
        }
        
        private void SendConnectionRefused(NetworkAddress address, ConnectionFailedReason reason)
        {
            var buffer     = _context.MemoryManager.Allocate(3);
            var bufferSpan = buffer.Span;

            bufferSpan[0] = (byte) PacketType.Command;
            bufferSpan[1] = (byte) CommandType.ConnectionRefused;
            bufferSpan[2] = (byte) reason;

            SendUnconnected(address, buffer);

            _context.MemoryManager.Free(buffer);
        }
        
        public void SendUnreliable(NetworkConnection connection, IntPtr data, int length)
        {
            if (length > NetworkSettings.PacketMtu - 1)
            {
                Logger.Error($"Cannot send data, it's above MTU - 1: {length}");
                return;
            }

            var buffer = _context.MemoryManager.Allocate(length + 1);
            buffer.CopyFrom(data, 1, length);
            buffer[0] = (byte) PacketType.Unreliable;

            Send(connection, buffer);

            _context.MemoryManager.Free(buffer);
        }
        
        public void SendUnreliable(NetworkConnection connection, MemoryBlock data)
        {
            SendUnreliable(connection, data.Buffer, data.Size);
        }
        
        public void SendUnreliable(NetworkConnection connection, Packet data)
        {
            SendUnreliable(connection, data.Data, data.Length);
        }
        
        private void SendKeepAlive(NetworkConnection connection)
        {
            Send(connection, _context.PacketPool.Statics.KeepAlivePacket);
        }
        
        public bool SendNotify(NetworkConnection connection, MemoryBlock data, object userData = null)
        {
            if (connection.SendWindow.IsFull) return false;

            var headerSize = _context.Settings.NotifyHeaderSize;

            if (data.Size > (NetworkSettings.PacketMtu - headerSize))
            {
                throw new InvalidOperationException();
            }

            var sequenceNumber = connection.SendNetworkSequencer.Next();
            var sendTime       = _timer.Now();

            var offset = 0;

            var buffer     = _context.MemoryManager.Allocate(data.Size + headerSize);
            var bufferSpan = buffer.Span;

            bufferSpan[0] =  (byte) PacketType.Notify;
            offset    += 1;

            BufferUtility.WriteUInt64(bufferSpan, sequenceNumber, offset, _context.Settings.SequenceNumberBytes);
            offset += _context.Settings.SequenceNumberBytes;

            BufferUtility.WriteUInt64(bufferSpan, connection.LastReceivedSequenceNumber, offset,
                                      _context.Settings.SequenceNumberBytes);
            offset += _context.Settings.SequenceNumberBytes;

            BufferUtility.WriteUInt64(bufferSpan, connection.ReceivedHistoryMask, offset);
            offset += sizeof(ulong);

            buffer.CopyFrom(data, offset, data.Size);

            connection.SendWindow.Push(new SendEnvelope()
            {
                Sequence = sequenceNumber,
                Time     = sendTime,
                UserData = userData
            });

            Send(connection, buffer);

            _context.MemoryManager.Free(buffer);

            return true;
        }
    }
}