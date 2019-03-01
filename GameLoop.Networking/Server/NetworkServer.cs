using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
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
        public NetworkServerState State => _state;
        
        private NetworkSocket _socket;
        private readonly IMemoryPool _memoryPool;
        private bool _quitRequested = false;
        private sbyte _ticksPerSecond;
        private double _tickTime;
        private NetworkServerState _state;
        private NetworkRunningState _sendingRunningState;
        private readonly ManualResetEventSlim _closingWaitHandle;

        private readonly NetworkConnectionCollection _connections;

        private readonly Action _onNewConnection;
        private readonly Action _onDataArrived;
        private readonly Action _onDisconnection;
        
        public NetworkServer(int expectedConcurrentPlayers)
        {
            var memoryAllocator = new SimpleManagedAllocator();
            _memoryPool = new SimpleMemoryPool(memoryAllocator);
            _sendingRunningState = NetworkRunningState.NotRunning;
            _state = NetworkServerState.Initialized;
            _closingWaitHandle = new ManualResetEventSlim();
            _connections = new NetworkConnectionCollection(expectedConcurrentPlayers);
        }

        public void Start(int port, int protocolId, sbyte ticksPerSecond = 30)
        {
            _socket = new NetworkSocket(_memoryPool, protocolId, HandleArrivedData);
            _socket.Bind(new IPEndPoint(IPAddress.Any, port));

            _ticksPerSecond = ticksPerSecond;
            _tickTime = 1000f / ticksPerSecond;
            _sendingRunningState = NetworkRunningState.Running;
            
            StartNetworkSendingThread();
            _state = NetworkServerState.Running;
        }
        
        private void StartNetworkSendingThread()
        {
            Task.Factory.StartNew(() =>
            {
                var timer = new Stopwatch();
                var timespan = new TimeSpan();
                double current = timer.Elapsed.TotalMilliseconds;
                while (!_quitRequested)
                {
                    timer.Start();
                    
                    // TODO: do stuff
                    
                    timer.Stop();

                    double elapsed = timer.Elapsed.TotalMilliseconds - current;

                    if (elapsed < _tickTime)
                    {
                        _sendingRunningState = NetworkRunningState.Running;
                        Thread.Sleep((int)elapsed);
                    }
                    else
                    {
                        // This network thread is overloaded.
                        // Probably a burst of data or just 
                        // too many connections/packets.
                        // We can think about a strategy to 
                        // cut down the tick rate until it can
                        // keep up.
                        _sendingRunningState = NetworkRunningState.Overloaded;
                    }
                }

                _closingWaitHandle.Set();
            }, TaskCreationOptions.LongRunning);
        }

        public void Close()
        {
            _socket.StopAcceptingData();
            DisconnectAll();
            
            _quitRequested = true;
            _closingWaitHandle.Wait();
            
            _socket.Close();
            _state = NetworkServerState.Closed;
            _sendingRunningState = NetworkRunningState.NotRunning;
        }

        private void HandleArrivedData(NetworkArrivedData data)
        {
            var address = data.EndPoint;
            var packet = data.Data;

            var reader = default(NetworkReader);
            reader.Initialize(ref packet);

            // Read how many messages arrived in this packet.
            byte messagesAmount = reader.ReadByte();

            for (byte i = 0; i < messagesAmount; i++)
            {
                var header = NetworkHeader.ReadHeader(ref reader);

                if (header.IsData)
                {
                    if (_connections.GetConnection(address, out var connection))
                    {
                        
                    }
                }
                else if (header.IsConnection)
                {
                    var connection = _connections.AddConnection(address);
                    TriggerConnection(connection);
                }
                else if (header.IsDisconnection)
                {
                    _connections.GetConnection(address, out var connection); 
                    _connections.RemoveConnection(connection);
                    TriggerDisconnection(connection);
                    break;
                }
            }
        }

        private void TriggerConnection(NetworkConnection connection)
        {
            _onNewConnection?.Invoke();
        }

        private void TriggerDisconnection(NetworkConnection connection)
        {
            _onDisconnection?.Invoke();
        }
        
        private void DisconnectAll()
        {
            
        }
    }
}