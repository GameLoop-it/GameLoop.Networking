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
using System.Text;
using NanoSockets;

namespace GameLoop.Networking.Sockets
{
    [StructLayout(LayoutKind.Explicit, Size = 18)]
    public struct NetworkAddress : IEquatable<NetworkAddress>
    {
        private const string LocalhostAddress = "127.0.0.1";
        private const string AnyAddress       = "0.0.0.0";

        [FieldOffset(0)] 
        internal Address Address;

        public ushort Port => Address.Port;

        internal NetworkAddress(Address address)
        {
            Address = address;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkAddress Create(string ip, ushort port)
        {
            return new NetworkAddress(NanoSockets.Address.CreateFromIpPort(ip, port));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkAddress CreateLocalhost(ushort port)
        {
            return Create(LocalhostAddress, port);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkAddress CreateAny(ushort port)
        {
            return Create(AnyAddress, port);
        }
        
        public override string ToString()
        {
            var ip = new StringBuilder(64);
            NanoSockets.UDP.GetIP(ref Address, ip, 64);

            return $"[IP={ip} Port={Port}]";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return Address.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(NetworkAddress other)
        {
            return Address.Equals(other.Address);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return obj is NetworkAddress other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(NetworkAddress left, NetworkAddress right)
        {
            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(NetworkAddress left, NetworkAddress right)
        {
            return !left.Equals(right);
        }
    }
}