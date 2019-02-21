namespace GameLoop.Networking.Memory
{
    public sealed class SimpleMemoryPool : IMemoryPool
    {
        private readonly IMemoryAllocator _allocator;
        
        public SimpleMemoryPool(IMemoryAllocator allocator)
        {
            _allocator = allocator;
        }
        
        public byte[] Rent(int minimumSize)
        {
            return _allocator.Allocate(minimumSize);
        }

        public void Release(byte[] chunk)
        {
            // This simple pool does not care of releasing your shit. ;)
        }
    }
}