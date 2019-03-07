using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GameLoop.Networking.Server
{
    public class NetworkConnectionCollection
    {
        private NetworkConnection[] _connections;
        private ConcurrentDictionary<IPEndPoint, NetworkConnection> _connectionsMap;
        private readonly ConcurrentStack<int> _freeUniqueIdentifiers;
        private int _currentFreeUniqueId;
        
        private readonly object _poolExpansionLock = new object();
        
        public NetworkConnectionCollection(int initialSize)
        {
            _connections = new NetworkConnection[initialSize];
            _connectionsMap = new ConcurrentDictionary<IPEndPoint, NetworkConnection>();
            _freeUniqueIdentifiers = new ConcurrentStack<int>();
            _currentFreeUniqueId = 0;
        }

        public NetworkConnection AddConnection(IPEndPoint endpoint)
        {
            var connection = NetworkConnection.Create(endpoint);
            connection.UniqueId = GetNextUniqueId();
            
            if(connection.UniqueId >= _connections.Length)
                ExpandConnectionsPool();

            _connections[connection.UniqueId] = connection;
            _connectionsMap.TryAdd(endpoint, connection);
            return connection;
        }

        public void RemoveConnection(NetworkConnection connection)
        {
            _connections[connection.UniqueId] = null;
            _connectionsMap.TryRemove(connection.RemoteEndpoint, out var removedConnection);
            ReleaseUniqueId(connection);
        }

        public NetworkConnection[] GetConnections()
        {
            return _connections;
        }

        public bool GetConnection(IPEndPoint endpoint, out NetworkConnection connection)
        {
            if(_connectionsMap.ContainsKey(endpoint))
            {
                connection = _connectionsMap[endpoint];
                return true;
            }

            connection = null;
            return false;
        }
        
        public bool GetConnection(int connectionId, out NetworkConnection connection)
        {
            if(connectionId < _connections.Length)
            {
                connection = _connections[connectionId];
                return connection != null;
            }

            connection = null;
            return false;
        }

        private void ExpandConnectionsPool()
        {
            lock (_poolExpansionLock)
            {
                Array.Resize(ref _connections, _connections.Length * 2);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetNextUniqueId()
        {
            if (!_freeUniqueIdentifiers.IsEmpty)
            {
                if (_freeUniqueIdentifiers.TryPop(out int id))
                {
                    return id;
                }
            }
            return Interlocked.Increment(ref _currentFreeUniqueId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReleaseUniqueId(NetworkConnection connection)
        {
            _freeUniqueIdentifiers.Push(connection.UniqueId);
        }
    }
}