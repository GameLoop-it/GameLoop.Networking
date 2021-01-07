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
using GameLoop.Networking.Packets;
using GameLoop.Networking.Sockets;
using GameLoop.Networking.Statistics;
using GameLoop.Utilities.Asserts;
using GameLoop.Utilities.Logs;
using GameLoop.Utilities.Timers;

namespace GameLoop.Networking
{
    public sealed class NetworkPeer
    {
        public Action<NetworkConnection> OnConnectionFailed;

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
                    HandleConnectedPacket(connection, packet);
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
                buffer[2] = (byte) ConnectionRefusedReason.ServerFull;
                
                SendUnconnected(endpoint, buffer, 0, 3);

                _context.MemoryManager.Free(buffer);
                
                return;
            }

            var connection = CreateConnection(endpoint);
            connection.Statistics.IncreaseBytesReceived(packet.Length);

            HandleCommandPacket(connection, packet);
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
                    connection.ChangeState(ConnectionState.Connected);
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
                    connection.ChangeState(ConnectionState.Connected);
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
                    var reason = (ConnectionRefusedReason) packet.Data[2];
                    Logger.DebugWarning($"Connection refused because: {reason}");

                    RemoveConnection(connection);
                    OnConnectionFailed?.Invoke(connection);

                    break;
                default:
                    Assert.AlwaysFail();
                    break;
            }
        }

        public void SendUnconnected(IPEndPoint endpoint, byte[] data)
        {
            SendUnconnected(endpoint, data, 0, data.Length);
        }
        
        public void SendUnconnected(IPEndPoint endpoint, byte[] data, int offset, int length)
        {
            _socket.SendTo(endpoint, data, offset, length);
        }

        private void Send(NetworkConnection connection, byte[] data)
        {
            Send(connection, data, 0, data.Length);
        }

        private void Send(NetworkConnection connection, byte[] data, int offset, int length)
        {
            connection.LastSendPacketTime = _timer.GetElapsedSeconds();
            _socket.SendTo(connection.RemoteEndpoint, data, offset, length);
            connection.Statistics.IncreaseBytesSent(length);
        }

        private void SendCommand(NetworkConnection connection, CommandType command)
        {
            Logger.DebugInfo($"Sending command {command} to {connection}");

            var buffer = _context.MemoryManager.Allocate(2);
            buffer[0] = (byte) PacketType.Command;
            buffer[1] = (byte) command;

            Send(connection, buffer);

            _context.MemoryManager.Free(buffer);
        }

        private NetworkConnection CreateConnection(IPEndPoint endpoint)
        {
            var connection = new NetworkConnection(endpoint);
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

        public void Connect(IPEndPoint endpoint)
        {
            var connection = CreateConnection(endpoint);
            connection.ChangeState(ConnectionState.Connecting);
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
            }
        }

        private void UpdateConnecting(NetworkConnection connection)
        {
            var currentConnectionAttemptTime =
                connection.LastConnectionAttemptTime + _context.Settings.ConnectionAttemptInterval;

            if (currentConnectionAttemptTime < _timer.GetElapsedSeconds())
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
    }
}