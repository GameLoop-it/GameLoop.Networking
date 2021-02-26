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
using System.Threading;
using GameLoop.Utilities.Asserts;

namespace GameLoop.Utilities.Memory
{
    public class MemoryPool : IDisposable, IMemoryPool
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

        public bool HasLeakedBlocks        => _currentLeakedBlocks > 0;
        public int  LeakedBlocksCount      => _currentLeakedBlocks;
        public bool IsEmpty                => _currentLeakedBlocks >= _blocksCount;
        public int  AvailableMemoryInBytes => _blocksCount * _blockSize;

        private MemoryPool()
        {
        }

        public static int MinimumBlockSize
        {
            get
            {
                unsafe
                {
                    return sizeof(MemoryPoolBlock);
                }
            }
        }
        
        public static MemoryPool Create(int blockSize, int blocksAmount)
        {
            Assert.AlwaysCheck(blockSize    > 0);
            Assert.AlwaysCheck(blocksAmount > 0);
            unsafe
            {
                Assert.AlwaysCheck(blockSize >= MinimumBlockSize);
            }

            var pool = new MemoryPool();

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

        public bool TryAllocate(out IntPtr blockPtr)
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

        public bool IsOwnerOf(IntPtr blockPtr)
        {
            unsafe
            {
                return ((byte*) blockPtr >= (byte*) _root) && ((byte*)blockPtr <= (byte*)_root + AvailableMemoryInBytes);
            }
        }

        public void Dispose()
        {
            MemoryHelper.Free(_root);
        }
    }
}