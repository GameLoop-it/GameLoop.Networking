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

#if ENABLE_TRANSPORT_LOGS
using GameLoop.Utilities.Logs;
#endif

namespace GameLoop.Networking.Transport
{
    public partial class NetworkPeer
    {
        private void HandleReceiving()
        {
            var buffer = _context.MemoryManager.Allocate(NetworkSettings.PacketMtu);

            while (_socket.Receive(out var address, buffer, out var receivedBytes))
            {
#if ENABLE_LOSS_SIMULATION
                if (_context.LossSimulator != null)
                {
                    if (_context.LossSimulator.IsLost())
                    {
                        _context.MemoryManager.Free(buffer);
                        return;
                    }
                }
#endif

                var packet = Packet.Create(buffer.Buffer, receivedBytes);

                if (_connections.TryGet(address, out var connection))
                {
                    if (connection.ConnectionState != ConnectionState.Disconnected)
                    {
                        HandleConnectedPacket(connection, packet);
                    }
                }
                else
                {
                    HandleUnconnectedPacket(address, packet);
                }
            }

            _context.MemoryManager.Free(buffer);
        }

        private void HandleConnectedPacket(NetworkConnection connection, Packet packet)
        {
            connection.LastReceivedPacketTime = _timer.GetElapsedSeconds();
            connection.Statistics.IncreaseBytesReceived(packet.Length);

            switch ((PacketType) packet.Span[0])
            {
                case PacketType.Command:
                    HandleCommandPacket(connection, packet);
                    break;
                case PacketType.Unreliable:
                    HandleUnreliablePacket(connection, packet);
                    break;
                case PacketType.KeepAlive:
                    // Just do nothing. It's just a keepalive packet to update the LastReceivedTime
                    // for this connection.
                    break;
                case PacketType.Notify:
                    HandleNotifyPacket(connection, packet);
                    break;
            }
        }

        private void HandleUnconnectedPacket(NetworkAddress address, Packet packet)
        {
            // If the length is less than 2, discard it: it's garbage.
            // The only unconnected packets we accept is ConnectionRequest, ConnectionAccepted and ConnectionRefused.
            if (packet.Length < 2) return;

            var packetSpan = packet.Span;

            // If the first byte is not PacketType.Command, discard it: it's not allowed.
            if ((PacketType) packetSpan[0] != PacketType.Command) return;
            // If the second byte is not CommandType.ConnectionRequest, discard it: it's not allowed.
            if ((CommandType) packetSpan[1] != CommandType.ConnectionRequest) return;

            if (_connections.Count >= _context.Settings.MaxConnectionsAllowed)
            {
                SendConnectionRefused(address, ConnectionFailedReason.ServerFull);
                return;
            }

            var connection = CreateConnection(address);
            connection.Statistics.IncreaseBytesReceived(packet.Length);

            HandleCommandPacket(connection, packet);
        }
        
        private void HandleCommandPacket(NetworkConnection connection, Packet packet)
        {
            var commandType = (CommandType) packet.Span[1];
            
            switch (commandType)
            {
                case CommandType.ConnectionRequest:
                    HandleConnectionRequestCommand(connection, packet);
                    break;
                case CommandType.ConnectionAccepted:
                    HandleConnectionAcceptedCommand(connection, packet);
                    break;
                case CommandType.ConnectionRefused:
                    HandleConnectionRefusedCommand(connection, packet);
                    break;
                case CommandType.Disconnect:
                    HandleDisconnection(connection, packet);
                    break;
                default:
                    Logger.Warning($"Unknown command {commandType} received from {connection}");
                    break;
            }
        }
        
        private void HandleConnectionRequestCommand(NetworkConnection connection, Packet packet)
        {
            switch (connection.ConnectionState)
            {
                case ConnectionState.Created:
                    // This is the first time I receive the ConnectionRequest after the
                    // connection creation.
                    SetConnectionAsConnected(connection);
                    SendCommand(connection, CommandType.ConnectionAccepted);
                    break;
                case ConnectionState.Connecting:
                    Assert.AlwaysFail();
                    break;
                case ConnectionState.Connected:
                    // If it's already connected but I am still handling a connection request,
                    // maybe the previous ConnectionAccepted command from the server was lost.
                    // This is due to the unreliable nature of UDP.
                    // I just resend the ConnectionAccepted command.
                    SendCommand(connection, CommandType.ConnectionAccepted);
                    break;
            }
        }
        
        private void HandleUnreliablePacket(NetworkConnection connection, Packet packet)
        {
            packet.Offset = 1;

            OnUnreliablePacket?.Invoke(connection, packet);
        }
        
        private void HandleNotifyPacket(NetworkConnection connection, Packet packet)
        {
            if (packet.Length < _context.Settings.NotifyHeaderSize) return;

            var packetSpan = packet.Span;
            
            packet.Offset = 1;

            var packetSequenceNumber =
                BufferUtility.ReadUInt64(packetSpan, packet.Offset, _context.Settings.SequenceNumberBytes);
            packet.Offset += _context.Settings.SequenceNumberBytes;

            var sequenceDistance =
                connection.SendNetworkSequencer.Distance(packetSequenceNumber, connection.LastReceivedSequenceNumber);

            // Check if the sequence distance is too far out of bounds.
            // If so, I just can't restore the connection. Just disconnect it.
            if (Math.Abs(sequenceDistance) > _context.Settings.SendWindowSize)
            {
                DisconnectConnection(connection, DisconnectionReason.SequenceOutOfBounds);
                return;
            }

            // Check if the sequence distance is negative or zero.
            // If so, the packet is old or re-ordered. I can ignore it.
            if (sequenceDistance <= 0)
            {
                return;
            }

            // Update the local connection's last received sequence number with the new received sequence number.
            connection.LastReceivedSequenceNumber = packetSequenceNumber;

            // Received History Mask is ulong, so 64 bit large. If the sequenceDistance is larger than 64,
            // it means that I'll have to shift my mask until it wraps, so I can just reset the ReceivedHistoryMask.
            if (sequenceDistance >= NetworkSettings.AckMaskBits)
            {
                connection.ReceivedHistoryMask =
                    1UL; // 00000000 00000000 00000000 00000000 00000000 00000000 00000000 00000001
            }
            else
            {
                // If it's less than 64 bit, I can proceed with the update.
                // I shift the history mask of sequenceDistance positions and I push 1 to the end (for this latest
                // packet I received).
                // Example (with a smaller mask, but it works the same with 64 bit masks):
                // ReceivedHistoryMask: 10101101 10101100
                // SequenceDistance: 3
                // 10101101 10101100 << 3 = 01101101 01100000
                // 01101101 01100000 | 1  = 01101101 01100001
                connection.ReceivedHistoryMask = (connection.ReceivedHistoryMask << (int) sequenceDistance) | 1;
            }

            var remoteLastReceivedSequence =
                BufferUtility.ReadUInt64(packetSpan, packet.Offset, _context.Settings.SequenceNumberBytes);
            packet.Offset += _context.Settings.SequenceNumberBytes;

            var remoteReceivedHistoryMask = BufferUtility.ReadUInt64(packetSpan, packet.Offset);
            packet.Offset += sizeof(ulong);

            AckPackets(connection, remoteLastReceivedSequence, remoteReceivedHistoryMask);

            OnNotifyPacketReceived?.Invoke(connection, packet);
        }
        
        private void AckPackets(NetworkConnection connection, ulong lastReceivedSequence, ulong receivedHistoryMask)
        {
            while (!connection.SendWindow.IsEmpty)
            {
                var envelope = connection.SendWindow.Peek();
                var distance = (int) connection.SendNetworkSequencer.Distance(envelope.Sequence, lastReceivedSequence);

                if (distance > 0) break;

                connection.SendWindow.Pop();

                // If the distance is 0, this is the last received packet on the other end and I can use it to
                // calculate the RoundTripTime.
                if (distance == 0)
                {
                    connection.RoundTripTime = _timer.Now() - envelope.Time;
                }

                // If the distance is less than -64 (so I haven't acked the packet yet and it isn't in the history
                // mask anymore) OR the history mask does not contain the ack, the packet is most likely lost.
                // -> Example 1: <-
                // distance: -70
                // It is <= -64? Yes. Packet is most likely lost.
                // -> Example 2: <-
                // distance: -3
                // receivedHistoryMask =        10101101 10100100 &
                // 1UL << -distance = 1 << 3 =  00000000 00001000 =
                //                              00000000 00000000
                // It is == 0? Yes. Packet is not acked yet, most likely lost.
                if ((distance <= -NetworkSettings.AckMaskBits) || ((receivedHistoryMask & (1UL << -distance)) == 0))
                {
                    OnNotifyPacketLost?.Invoke(connection, envelope.UserData);
                }
                else
                {
                    OnNotifyPacketDelivered?.Invoke(connection, envelope.UserData);
                }
            }
        }
        
        private void HandleConnectionAcceptedCommand(NetworkConnection connection, Packet packet)
        {
            switch (connection.ConnectionState)
            {
                case ConnectionState.Created:
                    Assert.AlwaysFail();
                    break;
                case ConnectionState.Connecting:
                    SetConnectionAsConnected(connection);
                    break;
                case ConnectionState.Connected:
                    // It never happens, ignore it.
                    break;
            }
        }
        
        private void HandleConnectionRefusedCommand(NetworkConnection connection, Packet packet)
        {
            switch (connection.ConnectionState)
            {
                case ConnectionState.Connecting:
                    var reason = (ConnectionFailedReason) packet.Span[2];
                    Logger.DebugWarning($"Connection refused because: {reason}");

                    RemoveConnection(connection);
                    OnConnectionFailed?.Invoke(connection, reason);

                    break;
                default:
                    Assert.AlwaysFail();
                    break;
            }
        }
        
        private void HandleDisconnection(NetworkConnection connection, Packet packet)
        {
            DisconnectConnection(connection, (DisconnectionReason) packet.Span[2], false);
        }
    }
}