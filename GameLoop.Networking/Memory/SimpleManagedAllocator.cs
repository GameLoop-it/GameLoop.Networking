using System.Runtime.CompilerServices;

namespace GameLoop.Networking.Memory
{
    public sealed class SimpleManagedAllocator : IMemoryAllocator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] Allocate(int size)
        {
            return new byte[size];
        }
    }
}