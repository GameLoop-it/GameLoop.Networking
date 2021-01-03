using System.Runtime.CompilerServices;

namespace GameLoop.Networking.Memory
{
    public sealed class SimpleManagedAllocator : IMemoryAllocator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] Allocate<T>(int size)
        {
            return new T[size];
        }
    }
}