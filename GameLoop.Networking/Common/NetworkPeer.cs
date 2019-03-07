using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GameLoop.Networking.Buffers;
using GameLoop.Networking.Common.Handlers;
using GameLoop.Networking.Memory;
using GameLoop.Networking.Server;
using GameLoop.Networking.Sockets;

namespace GameLoop.Networking.Common
{
    public sealed class NetworkPeer
    {
        private NetworkSocket _socket;
        private readonly IMemoryPool _memoryPool;
        
        private sbyte _ticksPerSecond;
        private double _tickTime;
        
        private readonly NetworkConnectionCollection _connections;
        
        private readonly ManualResetEventSlim _closingWaitHandle;
        private bool _quitRequested = false;

        private List<ConnectionHandler> _connectionHandlers;
        private List<DisconnectionHandler> _disconnectionHandlers;
        private List<MessageHandler> _messageHandlers;
        private List<MessageHandler> _concurrentMessageHandlers;
        private Task[] _awaitedTasks;
        
        public NetworkPeer(IMemoryPool memoryPool, int expectedConcurrentConnections)
        {
            _memoryPool = memoryPool;
            _closingWaitHandle = new ManualResetEventSlim();
            _connections = new NetworkConnectionCollection(expectedConcurrentConnections);
            
            _connectionHandlers = new List<ConnectionHandler>();
            _disconnectionHandlers = new List<DisconnectionHandler>();
            _messageHandlers = new List<MessageHandler>();
            _concurrentMessageHandlers = new List<MessageHandler>();
        }
        
        public void Start(int port, int protocolId, sbyte ticksPerSecond = 30)
        {
            _awaitedTasks = new Task[_concurrentMessageHandlers.Count];
            _socket = new NetworkSocket(_memoryPool, protocolId, HandleArrivedData);
            _socket.Bind(new IPEndPoint(IPAddress.Any, port));

            _ticksPerSecond = ticksPerSecond;
            _tickTime = 1000f / ticksPerSecond;
            
            StartNetworkSendingThread();
        }
        
        public void Close()
        {
            _socket.StopAcceptingData();
            DisconnectAll();
            
            _quitRequested = true;
            _closingWaitHandle.Wait();
            
            _socket.Close();
        }

        public void Send(NetworkConnection connection, ref NetworkMessage message)
        {
            
        }

        public void RegisterConnectionHandler<T>() where T : ConnectionHandler
        {
            var handler = Activator.CreateInstance<T>();
            _connectionHandlers.Add(handler);
        }
        
        public void RegisterDisconnectionHandler<T>() where T : DisconnectionHandler
        {
            var handler = Activator.CreateInstance<T>();
            _disconnectionHandlers.Add(handler);
        }
        
        public void RegisterMessageHandler<T>() where T : MessageHandler
        {
            var handler = Activator.CreateInstance<T>();
            
            if (handler.IsConcurrent) _concurrentMessageHandlers.Add(handler);
            else _messageHandlers.Add(handler);
        }
        
        private void DisconnectAll()
        {
            var connections = _connections.GetConnections();
            var message = NetworkMessage.CreateDisconnectionMessage(_memoryPool);
            for (int i = 0; i < connections.Length; i++)
            {
                var connection = connections[i];
                if (connection != null)
                {
                    Send(connection, ref message);
                }
            }
        }
        
        private void StartNetworkSendingThread()
        {
            Task.Factory.StartNew(() =>
            {
                var timer = new Stopwatch();
                while (!_quitRequested)
                {
                    timer.Restart();
                    
                    // TODO: do stuff
                    
                    timer.Stop();

                    double elapsed = timer.Elapsed.TotalMilliseconds;

                    if (elapsed < _tickTime)
                    {
                        Thread.Sleep((int)(_tickTime - elapsed));
                    }
                    else
                    {
                        // This network thread is overloaded.
                        // Probably a burst of data or just 
                        // too many connections/packets.
                        // We can think about a strategy to 
                        // cut down the tick rate until it can
                        // keep up.
                    }
                }

                _closingWaitHandle.Set();
            }, TaskCreationOptions.LongRunning);
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
                        var payloadLength = header.Length;

                        var payloadBuffer = _memoryPool.Rent(payloadLength);
                        reader.ReadBytes(payloadLength, ref payloadBuffer);
                        
                        if (header.IsEncrypted)
                        {
                            // Decrypt it.
                        }

                        if (header.IsCompressed)
                        {
                            // Decompress it.
                        }
                        
                        var payloadReader = default(NetworkReader);
                        payloadReader.Initialize(ref payloadBuffer);
                        
                        TriggerMessageHandling(connection, payloadReader);
                        
                        _memoryPool.Release(payloadBuffer);
                    }
                    else
                    {
                        // No connection found. How do I want to proceed?
                        // Drop the packet? Store it and evaluate it when possible?
                        // For now I simply drop it.
                        continue;
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
            
            _memoryPool.Release(packet);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TriggerConnection(NetworkConnection connection)
        {
            for (int i = 0; i < _connectionHandlers.Count; i++)
            {
                _connectionHandlers[i].Handle(connection);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TriggerDisconnection(NetworkConnection connection)
        {
            for (int i = 0; i < _disconnectionHandlers.Count; i++)
            {
                _disconnectionHandlers[i].Handle(connection);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TriggerMessageHandling(NetworkConnection connection, NetworkReader reader)
        {
            int awaitedTaskIndex = 0;

            for (int i = 0; i < _concurrentMessageHandlers.Count; i++)
            {
                var handler = _messageHandlers[i];
                _awaitedTasks[awaitedTaskIndex++] = Task.Run(() => { handler.Handle(connection, reader); });
            }
            
            for (int i = 0; i < _messageHandlers.Count; i++)
            {
                var handler = _messageHandlers[i];
                handler.Handle(connection, reader);
            }
            
            if(_awaitedTasks.Length > 0)
                Task.WaitAll(_awaitedTasks);
        }
    }
}