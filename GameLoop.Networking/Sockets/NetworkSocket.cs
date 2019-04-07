using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using GameLoop.Networking.Memory;
using GameLoop.Networking.Statistics;

namespace GameLoop.Networking.Sockets
{
    public sealed class NetworkSocket : INetworkSocket
    {
        // Max Transmission Unit.
        // Minimum MTU amount that an host can set is 576 bytes.
        // We are sure that this amount will not be fragmented on any device.
        private const int PacketMtu = 500;
        private const int ReceiverBufferSize = PacketMtu;

        public NetworkSocketStatistics Statistics;

        private Socket _socket;
        private EndPoint _listeningEndPoint;
        private EndPoint _readingEndPoint;
        
        private readonly AsyncCallback _listenCallback;

        private readonly IMemoryPool _memoryPool;
        private readonly byte[] _dataBuffer;

        private readonly ConcurrentQueue<NetworkArrivedData> _arrivedDataQueue;
        
        public NetworkSocket(IMemoryPool pool)
        {
            _listenCallback = ListenCallback;
            _memoryPool = pool;
            _dataBuffer = pool.Rent(ReceiverBufferSize);
            _arrivedDataQueue = new ConcurrentQueue<NetworkArrivedData>();
            Statistics = NetworkSocketStatistics.Create();
        }

        public void Bind(IPEndPoint endpoint)
        {
            InitializeSocket(endpoint.AddressFamily);
            _socket.Bind(endpoint);
            _listeningEndPoint = endpoint;
            _readingEndPoint = new IPEndPoint((_listeningEndPoint.AddressFamily == AddressFamily.InterNetworkV6) ? IPAddress.IPv6Any : IPAddress.Any, 0);
            
            Listen();
        }
        
        private void InitializeSocket(AddressFamily addressFamily)
        {
            switch (addressFamily)
            {
                case AddressFamily.InterNetwork:
                    _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    break;
                case AddressFamily.InterNetworkV6:
                    if (!Socket.OSSupportsIPv6) throw new Exception("IPv6 is not supported on this OS.");
                    _socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                    _socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
                    break;
                default:
                    throw new Exception("The address family isn't supported.");
            }
            _socket.Blocking = false;
            _socket.DontFragment = true;
            _socket.ReceiveBufferSize = ReceiverBufferSize;
            _socket.SendBufferSize = ReceiverBufferSize;
        }

        private void Listen()
        {
            try
            {
                _socket.BeginReceiveFrom(_dataBuffer, 0, ReceiverBufferSize, SocketFlags.None, ref _listeningEndPoint, _listenCallback, _dataBuffer);
            }
            catch (ObjectDisposedException)
            {
                // The current socket has been disposed.
                // Listening is not needed anymore.
            }
            catch (SocketException)
            {
                // Something failed with the current read.
                // Restart the listening routine.
                Listen();
            }
        }

        private void ListenCallback(IAsyncResult result)
        {
            int receivedBytes;
            EndPoint remoteEndpoint = _readingEndPoint;
            try
            {
                receivedBytes = _socket.EndReceiveFrom(result, ref remoteEndpoint);
            }
            catch (ObjectDisposedException)
            {
                // The current socket has been disposed.
                // The listening is not needed anymore.
                return;
            }
            catch (SocketException)
            {
                // Something failed with the current read.
                // Restart the listening routine.
                Listen();
                return;
            }
            
            // If we are here but we received 0 bytes, the socket has been dropped.
            // Also, if we received more data than our PacketMtu, we drop the received data.
            if (receivedBytes == 0 || receivedBytes > PacketMtu) return;
            
            // Copy received data in a local buffer.
            var buffer = _memoryPool.Rent(receivedBytes);
            Buffer.BlockCopy((byte[])result.AsyncState, 0, buffer, 0, receivedBytes);
            
            // Start listening again, while we handle the current received data.
            Listen();

            Statistics.BytesReceived += (ulong)receivedBytes;
            _arrivedDataQueue.Enqueue(new NetworkArrivedData() { EndPoint = remoteEndpoint, Data = buffer });
        }

        public void Close()
        {
            _socket.Close(1);
        }

        public void SendTo(IPEndPoint endPoint, byte[] data)
        {
            _socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, endPoint, null, null);
            Statistics.BytesSent += (ulong)data.Length;
        }

        public bool Poll(out NetworkArrivedData data)
        {
            return _arrivedDataQueue.TryDequeue(out data);
        }
        
        public bool HasAvailableData()
        {
            return !_arrivedDataQueue.IsEmpty;
        }
    }
}