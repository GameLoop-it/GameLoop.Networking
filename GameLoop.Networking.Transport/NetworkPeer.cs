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
using GameLoop.Networking.Transport.Collections;
using GameLoop.Networking.Transport.Packets;
using GameLoop.Utilities.Logs;
using GameLoop.Utilities.Timers;

namespace GameLoop.Networking.Transport
{
    public partial class NetworkPeer
    {
        public event Action<NetworkConnection, ConnectionFailedReason> OnConnectionFailed;
        public event Action<NetworkConnection, DisconnectionReason>    OnDisconnected;

        public event Action<NetworkConnection>         OnConnected;
        public event Action<NetworkConnection, Packet> OnUnreliablePacket;

        public event Action<NetworkConnection, object> OnNotifyPacketLost;
        public event Action<NetworkConnection, object> OnNotifyPacketDelivered;
        public event Action<NetworkConnection, Packet> OnNotifyPacketReceived;

        private          Timer          _timer;
        private          NativeSocket   _socket;
        private readonly NetworkContext _context;
        
        public NetworkPeer(NetworkContext context)
        {
            _context     = context;
            _socket      = new NativeSocket();
            _connections = new ConnectionCollection();
        }

        public void Start()
        {
            _timer = Timer.StartNew();
            _socket.Bind(ref _context.Settings.BindingEndpoint);
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
        
        public void Connect(NetworkAddress address)
        {
            var connection = CreateConnection(address);
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
    }
}