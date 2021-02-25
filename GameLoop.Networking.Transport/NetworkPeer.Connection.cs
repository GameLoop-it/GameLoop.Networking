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

using GameLoop.Networking.Sockets;
using GameLoop.Networking.Transport.Collections;
using GameLoop.Networking.Transport.Packets;
using GameLoop.Utilities.Asserts;

#if ENABLE_TRANSPORT_LOGS
using GameLoop.Utilities.Logs;
#endif

namespace GameLoop.Networking.Transport
{
    public partial class NetworkPeer
    {
        private readonly ConnectionCollection _connections;

        private NetworkConnection CreateConnection(NetworkAddress address)
        {
            var connection = new NetworkConnection(_context, address);
            connection.LastReceivedPacketTime = _timer.GetElapsedSeconds();
            _connections.Add(address, connection);
#if ENABLE_TRANSPORT_LOGS
            Logger.DebugInfo($"New connection from {connection}.");
#endif
            return connection;
        }

        private void RemoveConnection(NetworkConnection connection)
        {
            Assert.Check(connection.ConnectionState != ConnectionState.Removed);
            var removed = _connections.Remove(connection.RemoteAddress);
            connection.ChangeState(ConnectionState.Removed);
            Assert.Check(removed);
        }

        private void SetConnectionAsConnected(NetworkConnection connection)
        {
            connection.ChangeState(ConnectionState.Connected);
            OnConnected?.Invoke(connection);
        }

        private void UpdateConnections()
        {
            foreach (var connection in _connections.Connections)
            {
                UpdateConnection(connection);
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

            if (currentConnectionAttemptTime < _timer.Now())
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
            if (connection.LastReceivedPacketTime + _context.Settings.ConnectionTimeout < _timer.Now())
            {
                // If I am here, the connection timed out. The last packet has been received too much time ago, so
                // I assume we can disconnect the connection.
                DisconnectConnection(connection, DisconnectionReason.Timeout);
            }

            if (connection.LastSentPacketTime + _context.Settings.KeepAliveInterval < _timer.Now())
            {
                // If I am here, the connection has not sent data in the past KeepAliveInterval seconds. So to keep
                // the connection alive, I am going to send a keep alive signal.
                SendKeepAlive(connection);
            }
        }

        private void UpdateDisconnected(NetworkConnection connection)
        {
            if (connection.DisconnectionTime + _context.Settings.DisconnectionIdleTime < _timer.Now())
            {
                RemoveConnection(connection);
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
            connection.DisconnectionTime = _timer.Now();

            OnDisconnected?.Invoke(connection, reason);
        }
    }
}