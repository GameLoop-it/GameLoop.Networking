using System.Collections.Generic;
using GameLoop.Utilities.Asserts;
using GameLoop.Utilities.Memory;

namespace GameLoop.Networking.Transport.Memory
{
    public class SimpleMemoryManager : IMemoryManager
    {
        private struct MemoryPoolEntry
        {
            public int         BlockSize;
            public IMemoryPool Pool;

            public MemoryPoolEntry(int blockSize, IMemoryPool pool)
            {
                BlockSize = blockSize;
                Pool      = pool;
            }
        }

        private readonly List<MemoryPoolEntry> _pools;

        public SimpleMemoryManager(int initialBucketsSize, int maxBlockSize)
        {
            _pools = new List<MemoryPoolEntry>();

            int currentBlockSize = 2;
            while (true)
            {
                if (currentBlockSize > 512)
                {
                    _pools.Add(new MemoryPoolEntry(maxBlockSize, new SimpleMemoryPool(maxBlockSize, initialBucketsSize)));
                    break;
                }

                _pools.Add(new MemoryPoolEntry(currentBlockSize,
                                               new SimpleMemoryPool(currentBlockSize, initialBucketsSize)));

                currentBlockSize *= 2;
            }
        }

        public MemoryBlock Allocate(int size)
        {
            for (var i = 0; i < _pools.Count; i++)
            {
                var currentPoolEntry = _pools[i];

                if (size <= currentPoolEntry.BlockSize)
                {
                    var memoryBlock = new MemoryBlock();
                    memoryBlock.Size   = size;
                    memoryBlock.Buffer = currentPoolEntry.Pool.Allocate();

                    return memoryBlock;
                }
            }

            Assert.AlwaysFail($"The allocated size {size} is not supported.");
            return MemoryBlock.InvalidBlock;
        }

        public void Free(MemoryBlock block)
        {
            Assert.Check(block.Buffer != null);
            Assert.Check(block.Size   > 0);

            for (var i = 0; i < _pools.Count; i++)
            {
                var currentPoolEntry = _pools[i];

                if (block.Buffer.Length <= currentPoolEntry.BlockSize)
                {
                    currentPoolEntry.Pool.Free(block.Buffer);
                    return;
                }
            }

            Assert.AlwaysFail($"The memory block size of {block.Size} is not supported.");
        }
    }
}