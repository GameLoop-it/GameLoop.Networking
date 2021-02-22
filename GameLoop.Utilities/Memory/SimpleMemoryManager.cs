using System;
using System.Collections.Generic;
using GameLoop.Utilities.Asserts;

namespace GameLoop.Utilities.Memory
{
    public class SimpleMemoryManager : IMemoryManager, IDisposable
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
                    _pools.Add(new MemoryPoolEntry(maxBlockSize, SimpleMemoryPool.Create(maxBlockSize, initialBucketsSize)));
                    break;
                }

                _pools.Add(new MemoryPoolEntry(currentBlockSize, SimpleMemoryPool.Create(currentBlockSize, initialBucketsSize)));

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
            Assert.Check(block.Buffer != IntPtr.Zero);
            Assert.Check(block.Size   > 0);

            for (var i = 0; i < _pools.Count; i++)
            {
                var currentPoolEntry = _pools[i];

                if (block.Size <= currentPoolEntry.BlockSize)
                {
                    currentPoolEntry.Pool.Free(block.Buffer);
                    return;
                }
            }

            Assert.AlwaysFail($"The memory block size of {block.Size} is not supported.");
        }

        public void Dispose()
        {
            foreach (var poolEntry in _pools)
            {
                poolEntry.Pool.Dispose();
            }
        }

        public bool HasLeakedBlocks
        {
            get
            {
                foreach (var entry in _pools)
                {
                    if (entry.Pool.HasLeakedBlocks)
                        return true;
                }

                return false;
            }
        }

        public int LeakedBlocksCount
        {
            get
            {
                var count = 0;
                foreach (var entry in _pools)
                {
                    count += entry.Pool.LeakedBlocksCount;
                }

                return count;
            }
        }
    }
}