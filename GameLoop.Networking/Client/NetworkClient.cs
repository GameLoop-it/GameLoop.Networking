using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GameLoop.Networking.Buffers;
using GameLoop.Networking.Common;
using GameLoop.Networking.Memory;
using GameLoop.Networking.Sockets;

namespace GameLoop.Networking.Client
{
    public sealed class NetworkClient
    {
        public NetworkClientState State => _state;
        
        private NetworkSocket _socket;
        private IPEndPoint _endpoint;
        private NetworkClientState _state;
        private Action _onConnectionCallback;
        private Action _waitForConnectionDelegate;

        private IMemoryPool _memoryPool;

        private Queue<NetworkMessage> _sendingQueue;
        
        public NetworkClient()
        {
            var memoryAllocator = new SimpleManagedAllocator();
            _memoryPool = new SimpleMemoryPool(memoryAllocator);
            
            _state = NetworkClientState.Initialized;

            _waitForConnectionDelegate = WaitForConnection;
        }

        public void Connect(IPEndPoint endpoint, int protocolId, Action onConnectionCallback)
        {
            _socket = new NetworkSocket(_memoryPool, protocolId);
            _socket.Bind(endpoint);
            _endpoint = endpoint;
            _state = NetworkClientState.Connecting;
            _onConnectionCallback = onConnectionCallback;

            Task.Run(_waitForConnectionDelegate);

            SendConnectProtocol();
        }

        private void SendConnectProtocol()
        {
            var writer = NetworkMessage.CreateConnectionMessage(_memoryPool);
            Send(ref writer);
        }
        
        public void Send(ref NetworkWriter writer)
        {
            var buffer = _memoryPool.Rent(writer.GetSize());
            writer.ToByteArray(ref buffer);
            _socket.SendTo(_endpoint, buffer);
            _memoryPool.Release(buffer);
        }

        public void Close()
        {
            SendDisconnectProtocol();
            _socket.Close();
        }

        private void SendDisconnectProtocol()
        {
            var writer = default(NetworkWriter);
            writer.Initialize(_memoryPool, 1);
            writer.Write(true);
            Send(ref writer);
        }

        public bool Poll(out NetworkArrivedData arrivedData)
        {
            if (_state != NetworkClientState.Connected) throw new Exception("The client is not connected.");
            return _socket.Poll(out arrivedData);
        }

        private void WaitForConnection()
        {
            while (_state == NetworkClientState.Connecting)
            {
                while (_socket.Poll(out NetworkArrivedData data))
                {
                    if (!data.EndPoint.Equals(_endpoint)) continue;

                    var reader = default(NetworkReader);
                    reader.Initialize(ref data.Data);

                    bool isConnectionAccepted = reader.ReadBool();
                    
                    if (isConnectionAccepted)
                    {
                        _state = NetworkClientState.Connected;
                        _onConnectionCallback();
                        return;
                    }
                }

                Thread.Sleep(10);
            }
        }
    }
}