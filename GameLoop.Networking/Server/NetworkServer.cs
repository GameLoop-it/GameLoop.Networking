using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GameLoop.Networking.Buffers;
using GameLoop.Networking.Common;
using GameLoop.Networking.Memory;
using GameLoop.Networking.Sockets;

namespace GameLoop.Networking.Server
{
    public sealed class NetworkServer
    {
        private NetworkPeer _peer;

        public NetworkServer(IMemoryPool memoryPool, int expectedConcurrentConnections)
        {
            _peer = new NetworkPeer(memoryPool, expectedConcurrentConnections);
        }
        
        


        private readonly Action<NetworkConnection> _onNewConnection;
        private readonly Action<NetworkConnection, NetworkMessage> _onDataArrived;
        private readonly Action<NetworkConnection> _onDisconnection;
        

        
        
        

        

        
    }
}