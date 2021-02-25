using System;
using System.Collections.Generic;
using GameLoop.Utilities.Asserts;

namespace GameLoop.Utilities.Memory
{
    public class MemoryManager : IMemoryManager, IDisposable
    {
        private struct MemoryPoolEntry : IDisposable
        {
            public int               BlockSize;
            public int               BucketSize;
            public List<IMemoryPool> Pools;

            public bool HasLeakedBlocks
            {
                get
                {
                    for (var i = Pools.Count - 1; i >= 0; i--)
                    {
                        var pool = Pools[i];
                        if (pool.HasLeakedBlocks) return true;
                    }

                    return false;
                }
            }

            public int LeakedBlocksCount
            {
                get
                {
                    var accumulator = 0;
                    for (var i = Pools.Count - 1; i >= 0; i--)
                    {
                        var pool = Pools[i];
                        accumulator += pool.LeakedBlocksCount;
                    }

                    return accumulator;
                }
            }

            public MemoryPoolEntry(int blockSize, int bucketsSize)
            {
                BlockSize  = blockSize;
                BucketSize = bucketsSize;
                Pools      = new List<IMemoryPool>();
                ExpandPool();
            }

            private IMemoryPool ExpandPool()
            {
                var pool = MemoryPool.Create(BlockSize, BucketSize);
                Pools.Add(pool);
                return pool;
            }

            public IntPtr Allocate()
            {
                for (var i = Pools.Count - 1; i >= 0; i--)
                {
                    var pool = Pools[i];
                    if (!pool.IsEmpty)
                    {
                        if (pool.TryAllocate(out var block))
                            return block;
                    }
                }
                
                var newPool = ExpandPool();
                if (newPool.TryAllocate(out var newBlock))
                    return newBlock;

                throw new OutOfMemoryException();
            }

            public void Free(IntPtr block)
            {
                for (var i = Pools.Count - 1; i >= 0; i--)
                {
                    var pool = Pools[i];
                    if (pool.IsOwnerOf(block))
                    {
                        pool.Free(block);
                        return;
                    }
                }

                throw new ArgumentException("The passed block does not belong to any of the pools");
            }

            public void Dispose()
            {
                for (var i = Pools.Count - 1; i >= 0; i--)
                {
                    var pool = Pools[i];
                    pool.Dispose();
                }
            }
        }

        private readonly List<MemoryPoolEntry> _pools;

        public MemoryManager(int initialBucketsSize, int maxBlockSize)
        {
            _pools = new List<MemoryPoolEntry>();

            int currentBlockSize = MemoryPool.MinimumBlockSize;
            while (true)
            {
                if (currentBlockSize > 512)
                {
                    _pools.Add(new MemoryPoolEntry(maxBlockSize, initialBucketsSize));
                    break;
                }

                _pools.Add(new MemoryPoolEntry(currentBlockSize, initialBucketsSize));

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
                    memoryBlock.Size = size;

                    memoryBlock.Buffer = currentPoolEntry.Allocate();

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
                    currentPoolEntry.Free(block.Buffer);
                    return;
                }
            }

            Assert.AlwaysFail($"The memory block size of {block.Size} is not supported.");
        }

        public void Dispose()
        {
            foreach (var poolEntry in _pools)
            {
                poolEntry.Dispose();
            }
        }

        public bool HasLeakedBlocks
        {
            get
            {
                foreach (var entry in _pools)
                {
                    if (entry.HasLeakedBlocks)
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
                    count += entry.LeakedBlocksCount;
                }

                return count;
            }
        }
    }
}