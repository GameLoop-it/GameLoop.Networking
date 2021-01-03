using System.Runtime.CompilerServices;

namespace GameLoop.Networking.Memory
{
    public sealed class SimpleMemoryPool : IMemoryPool
    {
        private readonly IMemoryAllocator _allocator;
        
        public SimpleMemoryPool(IMemoryAllocator allocator)
        {
            _allocator = allocator;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] Rent<T>(int minimumSize)
        {
            return _allocator.Allocate<T>(minimumSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release<T>(T[] chunk)
        {
            // This simple pool does not care of releasing your shit. ;)
        }
    }
}