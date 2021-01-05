﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using GameLoop.Networking.Settings;
using GameLoop.Utilities.Logs;

namespace GameLoop.Networking.Sockets
{
    public sealed class NetworkSocket : INetworkSocket
    {
        private const int ReceiverBufferSize = NetworkSettings.PacketMtu;

        private Socket   _socket;
        private EndPoint _listeningEndPoint;
        private EndPoint _receivingFromEndPoint;
        private byte[]   _receiveBuffer;

        public NetworkSocket()
        {
            _receiveBuffer = new byte[ReceiverBufferSize];
        }

        public void Bind(IPEndPoint endpoint)
        {
            _socket = InitializeSocket(endpoint.AddressFamily);
            _socket.Bind(endpoint);
            NetworkSocketUtils.SetConnectionReset(_socket);

            _listeningEndPoint = endpoint;
            _receivingFromEndPoint = new IPEndPoint(
                (_listeningEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
                    ? IPAddress.IPv6Any
                    : IPAddress.Any,
                0);
        }

        private static Socket InitializeSocket(AddressFamily addressFamily)
        {
            Socket socket;

            switch (addressFamily)
            {
                case AddressFamily.InterNetwork:
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    break;
                case AddressFamily.InterNetworkV6:
                    if (!Socket.OSSupportsIPv6) throw new Exception("IPv6 is not supported on this OS.");
                    socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                    socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName) 27, false);
                    break;
                default:
                    throw new Exception("The address family isn't supported.");
            }

            socket.Blocking          = false;
            socket.DontFragment      = true;
            socket.ReceiveBufferSize = ReceiverBufferSize;
            socket.SendBufferSize    = ReceiverBufferSize;

            return socket;
        }

        public bool Receive(out IPEndPoint endPoint, out byte[] buffer, out int receivedBytes)
        {
            endPoint = null;
            buffer   = null;

            if (_socket.Poll(0, SelectMode.SelectRead) == false)
            {
                receivedBytes = 0;
                return false;
            }

            receivedBytes = _socket.ReceiveFrom(GetReceiveBuffer(), SocketFlags.None, ref _receivingFromEndPoint);

            if (receivedBytes > 0)
            {
                Logger.DebugInfo($"Received {receivedBytes} bytes from {_receivingFromEndPoint}");
                endPoint = (IPEndPoint) _receivingFromEndPoint;
                buffer   = _receiveBuffer;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte[] GetReceiveBuffer()
        {
            return _receiveBuffer;
        }

        public void Close()
        {
            _socket.Close(1);
        }

        public void SendTo(IPEndPoint endPoint, byte[] data)
        {
            SendTo(endPoint, data, 0, data.Length);
        }

        public void SendTo(IPEndPoint endPoint, byte[] data, int offset, int length)
        {
            _socket.SendTo(data, offset, length, SocketFlags.None, endPoint);
        }
    }
}