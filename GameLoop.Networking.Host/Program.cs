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
using System.Threading;
using GameLoop.Networking.Sockets;
using GameLoop.Networking.Transport.Packets;
using GameLoop.Networking.Transport.Simulators;
using GameLoop.Utilities.Logs;

namespace GameLoop.Networking.Transport.Host
{
    public class TestPeer
    {
        public const ushort ServerPort = 25005;

        public const int NumberCount = 16;

        public NetworkPeer Peer;

        private NetworkContext _context;

        public bool IsServer => _isServer;
        public bool IsClient => !_isServer;

        private bool              _isServer;
        private NetworkConnection _remoteConnection;

        public static NetworkAddress ServerEndpoint => NetworkAddress.CreateLocalhost(ServerPort);

        private int          _numberCounter;
        private HashSet<int> _numberSet;

        public TestPeer(bool isServer)
        {
            _isServer               =  isServer;
            Peer                    =  new NetworkPeer(GetNetworkContext(isServer));
            Peer.OnConnected        += PeerOnConnected;
            Peer.OnUnreliablePacket += PeerOnUnreliablePacket;
            Peer.OnNotifyPacketLost += PeerOnNotifyPacketLost;
            Peer.OnNotifyPacketDelivered += PeerOnNotifyPacketDelivered;
            Peer.OnNotifyPacketReceived += PeerOnNotifyPacketReceived;

            _numberSet = new HashSet<int>(NumberCount);

            if (IsClient)
                Peer.Connect(ServerEndpoint);
        }

        private void PeerOnNotifyPacketReceived(NetworkConnection connection, Packet packet)
        {
            
        }

        private void PeerOnNotifyPacketDelivered(NetworkConnection connection, object userData)
        {
            Logger.Warning($"Delivered: {userData}");
        }

        private void PeerOnNotifyPacketLost(NetworkConnection connection, object userData)
        {
            Logger.Error($"Lost: {userData}");
        }

        private void PeerOnUnreliablePacket(NetworkConnection connection, Packet packet)
        {
            unsafe
            {
                var number = BitConverter.ToInt32(new ReadOnlySpan<byte>(((byte*) packet.Data + packet.Offset), sizeof(int)));
                Logger.DebugInfo($"Unreliably received: {number}");
            }
        }

        private void PeerOnConnected(NetworkConnection connection)
        {
            _remoteConnection = connection;
        }

        private NetworkContext GetNetworkContext(bool isServer)
        {
            _context = new NetworkContext();
            GetNetworkSettings(isServer, _context);

            return _context;
        }

        private void GetNetworkSettings(bool isServer, NetworkContext context)
        {
            if (isServer)
                context.Settings.BindingEndpoint = ServerEndpoint;
            else
                context.Settings.BindingEndpoint = NetworkAddress.CreateAny(0);

            context.LossSimulator = new RandomLossSimulator(.25f);
        }

        public void Update()
        {
            Peer?.Update();

            if (_remoteConnection != null)
            {
                if (IsClient && _numberCounter < NumberCount)
                {
                    var block = _context.MemoryManager.Allocate(4);
                    block.CopyFrom(BitConverter.GetBytes(++_numberCounter), 0, 4);
                    
                    Peer?.SendNotify(_remoteConnection, block, _numberCounter);
                    
                    _context.MemoryManager.Free(block);
                }
                else
                {
                    var block = _context.MemoryManager.Allocate(1);
                    
                    Peer?.SendNotify(_remoteConnection, block, null);
                    
                    _context.MemoryManager.Free(block);
                }
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Logger.InitializeForConsole();

            var peers = new List<TestPeer>();
            peers.Add(new TestPeer(true));
            peers.Add(new TestPeer(false));

            foreach (var peer in peers)
            {
                peer.Peer.Start();
            }

            while (true)
            {
                foreach (var peer in peers)
                {
                    peer.Update();
                }

                Thread.Sleep(100);
            }
        }
    }
}