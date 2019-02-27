using System;
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
    public class NetworkServer
    {
        public NetworkServerState State => _state;
        
        private NetworkSocket _socket;
        private readonly IMemoryPool _memoryPool;
        private bool _quitRequested = false;
        private sbyte _ticksPerSecond;
        private double _tickTime;
        private NetworkServerState _state;
        private NetworkRunningState _receivingRunningState;

        private readonly Action _handleArrivedDataDelegate;

        private readonly Action _onNewConnection;
        private readonly Action _onDataArrived;
        private readonly Action _onDisconnection;
        
        public NetworkServer()
        {
            var memoryAllocator = new SimpleManagedAllocator();
            _memoryPool = new SimpleMemoryPool(memoryAllocator);
            _receivingRunningState = NetworkRunningState.NotRunning;
            _handleArrivedDataDelegate = HandleArrivedData;
            _state = NetworkServerState.Initialized;
        }

        public void Start(int port, int protocolId, sbyte ticksPerSecond = 30)
        {
            _socket = new NetworkSocket(_memoryPool, protocolId);
            _socket.Bind(new IPEndPoint(IPAddress.Any, port));

            _ticksPerSecond = ticksPerSecond;
            _tickTime = 1000f / ticksPerSecond;
            _receivingRunningState = NetworkRunningState.Running;
            
            StartNetworkThread();
            _state = NetworkServerState.Listening;
        }

        private void StartNetworkThread()
        {
            Task.Factory.StartNew(() =>
            {
                var timer = new Stopwatch();
                var timespan = new TimeSpan();
                double current = timer.Elapsed.TotalMilliseconds;
                while (!_quitRequested)
                {
                    timer.Start();
                    while (_socket.HasAvailableData())
                    {
                        Task.Run(_handleArrivedDataDelegate);
                    }
                    timer.Stop();

                    double elapsed = timer.Elapsed.TotalMilliseconds - current;

                    if (elapsed < _tickTime)
                    {
                        _receivingRunningState = NetworkRunningState.Running;
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
                        _receivingRunningState = NetworkRunningState.Overloaded;
                    }
                }
                
                DisconnectAll();
                
                _socket.Close();
                _state = NetworkServerState.Closed;
            }, TaskCreationOptions.LongRunning);
        }

        public void Close()
        {
            _quitRequested = true;
            _receivingRunningState = NetworkRunningState.NotRunning;
        }

        private void HandleArrivedData()
        {
            if (_socket.Poll(out NetworkArrivedData data))
            {
                var address = data.EndPoint;
                var packet = data.Data;

                var reader = default(NetworkReader);
                reader.Initialize(ref packet);

                var header = NetworkHeader.ReadHeader(ref reader);
                
                
            }
        }
        
        private void DisconnectAll()
        {
            
        }
    }
}