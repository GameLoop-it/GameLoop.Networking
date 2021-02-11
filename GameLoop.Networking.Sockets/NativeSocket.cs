/*
The MIT License (MIT)

Copyright (c) 2020 Emanuele Manzione

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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using GameLoop.Utilities.Asserts;
using NanoSockets;
using Socket = NanoSockets.Socket;
#if LOGS_SOCKET
using GameLoop.Utilities.Logs;
#endif

namespace GameLoop.Networking.Sockets
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct NativeSocket : IDisposable
    {
        [FieldOffset(0)]
        private Socket _socket;

        public bool IsCreated => _socket.IsCreated;

        public void Bind(ref NetworkAddress address)
        {
            if (UDP.Initialize() == Status.Error)
            {
                Assert.AlwaysFail("Cannot initialize NanoSockets");
            }

            _socket = UDP.Create(256 * 1024, 256 * 1024);

            if (!_socket.IsCreated)
            {
                Assert.AlwaysFail("Cannot create socket");
            }
            
            if (UDP.SetNonBlocking(_socket) != Status.OK)
            {
                Assert.AlwaysFail("Cannot set NonBlocking Mode");
            }
            
            if (UDP.SetDontFragment(_socket) != Status.OK)
            {
                Assert.AlwaysFail("Cannot set DontFragment");
            }

            if (UDP.Bind(_socket, ref address.Address) != 0)
            {
                Assert.AlwaysFail($"Cannot bind the socket to {address}");
            }

            UDP.GetAddress(_socket, ref address.Address);
            
#if LOGS_SOCKET
            Logger.Debug($"Socket successfully bound to {address}");
#endif
        }

        public void Dispose()
        {
            Close();
        }

        public void Close()
        {
            UDP.Destroy(ref _socket);
            UDP.Deinitialize();
        }

        public void Connect(ref NetworkAddress connectTo)
        {
            if (UDP.Connect(_socket, ref connectTo.Address) != 0)
            {
                Assert.AlwaysFail($"Cannot connect to {connectTo}");
            }
            
#if LOGS_SOCKET
            Logger.Debug($"Socket successfully connected to {connectTo}");
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int SendTo(NetworkAddress address, byte[] data)
        {
            return SendTo(address, data, data.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int SendTo(NetworkAddress address, byte[] data, int length)
        {
            return UDP.Send(_socket, ref address.Address, data, length);
        }

        public bool Receive(out NetworkAddress remoteAddress, byte[] buffer, out int receivedBytes)
        {
            Assert.NotNull(buffer);
            remoteAddress = default;

            if (!Poll(0))
            {
                receivedBytes = 0;
                return false;
            }

            receivedBytes = UDP.Receive(_socket, ref remoteAddress.Address, buffer, buffer.Length);

            if (receivedBytes > 0)
            {
#if LOGS_SOCKET
                Logger.Debug($"Received [{receivedBytes}] bytes from {remoteAddress}");
#endif

                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Poll(int timeout)
        {
            return UDP.Poll(_socket, timeout) > 0;
        }
    }
}