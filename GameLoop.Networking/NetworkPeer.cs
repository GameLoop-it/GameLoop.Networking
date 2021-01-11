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
using System.Collections.Generic;
using System.Net;
using GameLoop.Networking.Buffers;
using GameLoop.Networking.Memory;
using GameLoop.Networking.Packets;
using GameLoop.Networking.Settings;
using GameLoop.Networking.Sockets;
using GameLoop.Utilities.Asserts;
using GameLoop.Utilities.Exceptions;
using GameLoop.Utilities.Logs;
using GameLoop.Utilities.Timers;

namespace GameLoop.Networking
{
    public sealed class NetworkPeer
    {
        public event Action<NetworkConnection, ConnectionFailedReason> OnConnectionFailed;
        public event Action<NetworkConnection>                         OnConnected;
        public event Action<NetworkConnection, DisconnectionReason>    OnDisconnected;
        public event Action<NetworkConnection, Packet>                 OnUnreliablePacket;

        private          Timer          _timer;
        private readonly INetworkSocket _socket;
        private readonly NetworkContext _context;

        private readonly Dictionary<IPEndPoint, NetworkConnection> _connections;

        public NetworkPeer(NetworkContext context)
        {
            _context     = context;
            _socket      = _context.SocketFactory.Create();
            _connections = new Dictionary<IPEndPoint, NetworkConnection>();
        }

        public void Start()
        {
            _timer = Timer.StartNew();
            _socket.Bind(_context.Settings.BindingEndpoint);
        }

        public void Close()
        {
            _socket.Close();
            _timer.Stop();
        }

        public void Update()
        {
            HandleReceiving();
            UpdateConnections();
        }

        private void HandleReceiving()
        {
            if (_socket.Receive(out var endpoint, out var buffer, out var receivedBytes))
            {
                var packet = new Packet()
                {
                    Data   = buffer,
                    Length = receivedBytes
                };

                if (_connections.TryGetValue(endpoint, out var connection))
                {
                    if (connection.ConnectionState != ConnectionState.Disconnected)
                    {
                        HandleConnectedPacket(connection, packet);
                    }
                }
                else
                {
                    HandleUnconnectedPacket(endpoint, packet);
                }
            }
        }

        private void HandleConnectedPacket(NetworkConnection connection, Packet packet)
        {
            connection.LastReceivedPacketTime = _timer.GetElapsedSeconds();
            connection.Statistics.IncreaseBytesReceived(packet.Length);

            switch ((PacketType) packet.Data[0])
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
                    Logger.Debug($"Received notify packet");
                    break;
            }
        }

        private void HandleUnconnectedPacket(IPEndPoint endpoint, Packet packet)
        {
            // If the length is less than 2, discard it: it's garbage.
            if (packet.Length < 2) return;

            // If the first byte is not PacketType.Command, discard it: it's not allowed.
            if ((PacketType) packet.Data[0] != PacketType.Command) return;
            // If the second byte is not CommandType.ConnectionRequest, discard it: it's not allowed.
            if ((CommandType) packet.Data[1] != CommandType.ConnectionRequest) return;

            if (_connections.Count >= _context.Settings.MaxConnectionsAllowed)
            {
                var buffer = _context.MemoryManager.Allocate(3);
                buffer[0] = (byte) PacketType.Command;
                buffer[1] = (byte) CommandType.ConnectionRefused;
                buffer[2] = (byte) ConnectionFailedReason.ServerFull;

                SendUnconnected(endpoint, buffer);

                _context.MemoryManager.Free(buffer);

                return;
            }

            var connection = CreateConnection(endpoint);
            connection.Statistics.IncreaseBytesReceived(packet.Length);

            HandleCommandPacket(connection, packet);
        }

        private void HandleUnreliablePacket(NetworkConnection connection, Packet packet)
        {
            packet.Offset = 1;

            OnUnreliablePacket?.Invoke(connection, packet);
        }

        private void HandleCommandPacket(NetworkConnection connection, Packet packet)
        {
            switch ((CommandType) packet.Data[1])
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
                    Logger.Warning($"Unknown command {packet.Data[1]} received from {connection}");
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
                    var reason = (ConnectionFailedReason) packet.Data[2];
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
            DisconnectConnection(connection, (DisconnectionReason) packet.Data[3], false);
        }

        public void SendUnconnected(IPEndPoint endpoint, MemoryBlock data)
        {
            SendUnconnected(endpoint, data.Buffer, 0, data.Size);
        }

        public void SendUnconnected(IPEndPoint endpoint, byte[] data, int offset, int length)
        {
            _socket.SendTo(endpoint, data, offset, length);
        }

        private void Send(NetworkConnection connection, MemoryBlock data)
        {
            Send(connection, data.Buffer, 0, data.Size);
        }

        private void Send(NetworkConnection connection, byte[] data)
        {
            Send(connection, data, 0, data.Length);
        }

        private void Send(NetworkConnection connection, byte[] data, int offset, int length)
        {
            Assert.Check(connection.ConnectionState < ConnectionState.Disconnected);

            connection.LastSentPacketTime = _timer.GetElapsedSeconds();
            _socket.SendTo(connection.RemoteEndpoint, data, offset, length);
            connection.Statistics.IncreaseBytesSent(length);
        }

        private void SendCommand(NetworkConnection connection, CommandType command, byte commandData = 0)
        {
            Assert.Check(connection.ConnectionState < ConnectionState.Disconnected);

            Logger.DebugInfo($"Sending command {command} to {connection}");

            // Check if the command is a pre-allocated one.
            switch (command)
            {
                case CommandType.ConnectionRequest:
                    Send(connection, _context.PacketPool.ConnectionRequestPacket);
                    return;
                case CommandType.ConnectionAccepted:
                    Send(connection, _context.PacketPool.ConnectionAcceptedPacket);
                    return;
            }

            var size = 2;
            if (commandData != 0)
                size = 3;

            var buffer = _context.MemoryManager.Allocate(size);
            buffer[0] = (byte) PacketType.Command;
            buffer[1] = (byte) command;

            if (commandData != 0)
                buffer[2] = commandData;

            Send(connection, buffer);

            _context.MemoryManager.Free(buffer);
        }

        public void SendUnreliable(NetworkConnection connection, byte[] data, int offset, int length)
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
            SendUnreliable(connection, data.Buffer, 0, data.Size);
        }

        private void SendKeepAlive(NetworkConnection connection)
        {
            Send(connection, _context.PacketPool.KeepAlivePacket);
        }

        public bool SendNotify(NetworkConnection connection, MemoryBlock data, object userData = null)
        {
            if (connection.SendWindow.IsFull) return false;

            // The notify packet is composed by:
            // - PacketType.Notify                  - 1 byte
            // - Sequence number for this packet    - _context.Settings.SequenceNumberBytes bytes
            // - Last received sequence number      - _context.Settings.SequenceNumberBytes bytes
            // - ReceivedHistoryMask                - 8 bytes

            var headerSize = 1 + 8 + (_context.Settings.SequenceNumberBytes * 2);

            if (data.Size > (NetworkSettings.PacketMtu - headerSize))
            {
                throw new InvalidOperationException();
            }

            var sequenceNumber = connection.SendSequencer.Next();
            var sendTime       = _timer.Now;

            var offset = 0;
            
            var buffer = _context.MemoryManager.Allocate(data.Size + headerSize);
            
            buffer[0] =  (byte) PacketType.Notify;
            offset    += 1;
            
            BufferUtility.WriteUInt64(buffer.Buffer, sequenceNumber, offset, _context.Settings.SequenceNumberBytes);
            offset += _context.Settings.SequenceNumberBytes;
            
            BufferUtility.WriteUInt64(buffer.Buffer, connection.LastReceivedSequenceNumber, offset, _context.Settings.SequenceNumberBytes);
            offset += _context.Settings.SequenceNumberBytes;
            
            BufferUtility.WriteUInt64(buffer.Buffer, connection.ReceivedHistoryMask, offset);
            offset += sizeof(ulong);
            
            buffer.CopyFrom(data, offset, data.Size);
            
            connection.SendWindow.Push(new SendEnvelope()
            {
                Sequence = sequenceNumber,
                Time = sendTime,
                UserData = userData
            });
            
            Send(connection, buffer);

            _context.MemoryManager.Free(buffer);

            return true;
        }

        private NetworkConnection CreateConnection(IPEndPoint endpoint)
        {
            var connection = new NetworkConnection(_context, endpoint);
            connection.LastReceivedPacketTime = _timer.GetElapsedSeconds();
            _connections.Add(endpoint, connection);

            Logger.DebugInfo($"New connection from {connection}.");

            return connection;
        }

        private void RemoveConnection(NetworkConnection connection)
        {
            Assert.Check(connection.ConnectionState != ConnectionState.Removed);
            var removed = _connections.Remove(connection.RemoteEndpoint);
            connection.ChangeState(ConnectionState.Removed);
            Assert.Check(removed);
        }

        private void SetConnectionAsConnected(NetworkConnection connection)
        {
            connection.ChangeState(ConnectionState.Connected);
            OnConnected?.Invoke(connection);
        }

        public void Connect(IPEndPoint endpoint)
        {
            var connection = CreateConnection(endpoint);
            connection.ChangeState(ConnectionState.Connecting);
        }

        public void Disconnect(NetworkConnection connection)
        {
            if (connection.ConnectionState != ConnectionState.Connected)
            {
                Logger.Error($"Cannot disconnect {connection} with state {connection.ConnectionState}");
                return;
            }

            DisconnectConnection(connection, DisconnectionReason.RequestedByPeer);
        }

        private void UpdateConnections()
        {
            foreach (var entry in _connections)
            {
                UpdateConnection(entry.Value);
            }
        }

        private void UpdateConnection(NetworkConnection connection)
        {
            switch (connection.ConnectionState)
            {
                case ConnectionState.Connecting:
                    UpdateConnecting(connection);
                    break;
                case ConnectionState.Connected:
                    UpdateConnected(connection);
                    break;
                case ConnectionState.Disconnected:
                    UpdateDisconnected(connection);
                    break;
            }
        }

        private void UpdateConnecting(NetworkConnection connection)
        {
            var currentConnectionAttemptTime =
                connection.LastConnectionAttemptTime + _context.Settings.ConnectionAttemptInterval;

            if (currentConnectionAttemptTime < _timer.Now)
            {
                if (connection.ConnectionAttempts >= _context.Settings.MaxConnectionsAttempts)
                {
                    Assert.AlwaysFail("Connection failed. Handle this with a callback.");
                    return;
                }

                connection.ConnectionAttempts        += 1;
                connection.LastConnectionAttemptTime =  _timer.GetElapsedSeconds();

                SendCommand(connection, CommandType.ConnectionRequest);
            }
        }

        private void UpdateConnected(NetworkConnection connection)
        {
            if (connection.LastReceivedPacketTime + _context.Settings.ConnectionTimeout < _timer.Now)
            {
                // If I am here, the connection timed out. The last packet has been received too much time ago, so
                // I assume we can disconnect the connection.
                DisconnectConnection(connection, DisconnectionReason.Timeout);
            }

            if (connection.LastSentPacketTime + _context.Settings.KeepAliveInterval < _timer.Now)
            {
                // If I am here, the connection has not sent data in the past KeepAliveInterval seconds. So to keep
                // the connection alive, I am going to send a keep alive signal.
                SendKeepAlive(connection);
            }
        }

        private void DisconnectConnection(NetworkConnection connection, DisconnectionReason reason,
                                          bool              sendToOthers = true)
        {
            if (sendToOthers)
            {
                SendCommand(connection, CommandType.Disconnect, (byte) reason);
            }

            connection.ChangeState(ConnectionState.Disconnected);
            connection.DisconnectionTime = _timer.Now;

            OnDisconnected?.Invoke(connection, reason);
        }

        private void UpdateDisconnected(NetworkConnection connection)
        {
            if (connection.DisconnectionTime + _context.Settings.DisconnectionIdleTime < _timer.Now)
            {
                RemoveConnection(connection);
            }
        }
    }
}