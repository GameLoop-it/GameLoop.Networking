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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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