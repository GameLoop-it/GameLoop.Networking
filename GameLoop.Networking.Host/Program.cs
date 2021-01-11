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
using GameLoop.Networking.Packets;
using GameLoop.Utilities.Logs;

namespace GameLoop.Networking.Host
{
    public class TestPeer
    {
        public const int ServerPort = 25005;

        public NetworkPeer Peer;

        public bool IsServer => _isServer;
        public bool IsClient => !_isServer;

        private bool _isServer;

        public static IPEndPoint ServerEndpoint => new IPEndPoint(IPAddress.Loopback, ServerPort);

        public TestPeer(bool isServer)
        {
            _isServer               =  isServer;
            Peer                    =  new NetworkPeer(GetNetworkContext(isServer));
            Peer.OnConnected        += PeerOnConnected;
            Peer.OnUnreliablePacket += PeerOnUnreliablePacket;

            if (IsClient)
                Peer.Connect(ServerEndpoint);
        }

        private void PeerOnUnreliablePacket(NetworkConnection connection, Packet packet)
        {
            var number = BitConverter.ToInt32(packet.Data, packet.Offset);
            Logger.DebugInfo($"Unreliably received: {number}");
        }

        private void PeerOnConnected(NetworkConnection connection)
        {
            if (IsClient)
            {
                Peer.SendNotify(connection, BitConverter.GetBytes(int.MaxValue));
            }
        }

        private static NetworkContext GetNetworkContext(bool isServer)
        {
            var context = new NetworkContext();
            GetNetworkSettings(isServer, context);

            return context;
        }

        private static void GetNetworkSettings(bool isServer, NetworkContext context)
        {
            if (isServer)
                context.Settings.BindingEndpoint = ServerEndpoint;
            else
                context.Settings.BindingEndpoint = new IPEndPoint(IPAddress.Any, 0);
        }

        public void Update()
        {
            Peer?.Update();
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

                Thread.Sleep(15);
            }
        }
    }
}