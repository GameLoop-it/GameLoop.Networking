using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using GameLoop.Utilities.Asserts;

namespace GameLoop.Utilities.Memory
{
    public class SimpleMemoryPool : IDisposable, IMemoryPool
    {
        [StructLayout(LayoutKind.Explicit)]
        private struct MemoryPoolBlock
        {
            [FieldOffset(0)] public IntPtr Next;
        }

        private int    _blockSize;
        private int    _blocksCount;
        private IntPtr _root;
        private IntPtr _nextFreeBlock;

        private int _currentLeakedBlocks;

        public bool HasLeakedBlocks   => _currentLeakedBlocks > 0;
        public int  LeakedBlocksCount => _currentLeakedBlocks;

        private SimpleMemoryPool()
        {
        }

        public static SimpleMemoryPool Create(int blockSize, int blocksAmount)
        {
            Assert.AlwaysCheck(blockSize    > 0);
            Assert.AlwaysCheck(blocksAmount > 0);

            var pool = new SimpleMemoryPool();

            pool._blockSize     = blockSize;
            pool._blocksCount   = blocksAmount;
            pool._root          = MemoryHelper.Alloc(blockSize * blocksAmount);
            pool._nextFreeBlock = IntPtr.Zero;

            pool.InitializeFreeBlocks();

            return pool;
        }

        private void InitializeFreeBlocks()
        {
            unsafe
            {
                for (var i = 0; i < _blocksCount; i++)
                {
                    var block = (MemoryPoolBlock*) IntPtr.Add(_root, i * _blockSize);
                    block->Next    = _nextFreeBlock;
                    _nextFreeBlock = (IntPtr) block;
                }
            }
        }

        private static void RaiseOutOfMemoryException()
        {
            throw new OutOfMemoryException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntPtr Allocate()
        {
            if (TryGetBlock(out var block)) return block;

            RaiseOutOfMemoryException();
            return default;
        }

        public bool TryGetBlock(out IntPtr blockPtr)
        {
            IntPtr nextFreeBlock = _nextFreeBlock;

            if (nextFreeBlock == IntPtr.Zero)
            {
                blockPtr = default;
                return false;
            }

            unsafe
            {
                var block = (MemoryPoolBlock*) nextFreeBlock;

                _nextFreeBlock = block->Next;
                block->Next    = default;

                blockPtr = nextFreeBlock;
                Interlocked.Increment(ref _currentLeakedBlocks);
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Free(IntPtr blockPtr)
        {
            unsafe
            {
                var block = (MemoryPoolBlock*) blockPtr;
                block->Next = _nextFreeBlock;

                _nextFreeBlock = blockPtr;
                Interlocked.Decrement(ref _currentLeakedBlocks);
            }
        }

        public void Dispose()
        {
            MemoryHelper.Free(_root);
        }
    }
}