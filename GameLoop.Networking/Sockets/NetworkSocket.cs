using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GameLoop.Networking.Buffers;
using GameLoop.Networking.Memory;
using GameLoop.Networking.Settings;
using GameLoop.Networking.Statistics;

namespace GameLoop.Networking.Sockets
{
    public sealed class NetworkSocket
    {
        private const int ReceiverBufferSize = NetworkSettings.PacketMtu;

        public NetworkSocketStatistics Statistics;

        private Socket _socket;
        private EndPoint _listeningEndPoint;
        private EndPoint _readingEndPoint;
        
        private readonly AsyncCallback _listenCallback;

        private readonly IMemoryPool _memoryPool;
        private readonly byte[] _dataBuffer;
        private readonly int _protocolId;

        private bool _canAcceptData = true;

        private readonly Action<NetworkArrivedData> _onArrivedData;
        
        public NetworkSocket(IMemoryPool pool, int protocolId, Action<NetworkArrivedData> arrivedDataCallback)
        {
            _listenCallback = ListenCallback;
            _memoryPool = pool;
            _dataBuffer = pool.Rent(ReceiverBufferSize);
            _protocolId = protocolId;
            _onArrivedData = arrivedDataCallback;
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
            if (!_canAcceptData) return;
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
            if (receivedBytes == 0) return;
            
            // Also, if we received more data than our PacketMtu, we drop the received data.
            if (receivedBytes > NetworkSettings.PacketMtu)
            {
                Statistics.MalformedReceivedPayloads++;
                Listen();
                return;
            }

            var arrivedBytes = (byte[])result.AsyncState;
            
            // Check if the arrived protocol is the same
            var reader = default(NetworkReader);
            reader.Initialize(ref arrivedBytes);
            var arrivedProtocol = reader.ReadInt();

            // If not, drop the payload
            if (arrivedProtocol != _protocolId)
            {
                Statistics.MalformedReceivedPayloads++;
                Listen();
                return;
            }
            
            // Copy received data in a local buffer.
            var buffer = _memoryPool.Rent(receivedBytes - sizeof(int));
            Buffer.BlockCopy(arrivedBytes, sizeof(int), buffer, 0, receivedBytes - sizeof(int));
            
            // Start listening again, while we handle the current received data.
            Listen();

            Statistics.BytesReceived += (ulong)receivedBytes;
            
            Task.Run(() => { _onArrivedData.Invoke(new NetworkArrivedData() { EndPoint = (IPEndPoint)remoteEndpoint, Data = buffer }); });
        }

        public void StopAcceptingData()
        {
            _canAcceptData = false;
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
/*
        public bool Poll(out NetworkArrivedData data)
        {
            return _arrivedDataQueue.TryDequeue(out data);
        }

        public bool HasAvailableData()
        {
            return !_arrivedDataQueue.IsEmpty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WaitForAvailableData()
        {
            _receiveWaitHandle.WaitOne();
        }*/
    }
}