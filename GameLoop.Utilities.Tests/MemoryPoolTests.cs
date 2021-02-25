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
using System.Collections.Generic;
using GameLoop.Utilities.Memory;
using NUnit.Framework;

namespace GameLoop.Utilities.Tests
{
    public class MemoryPoolTests
    {
        private const int BlockSize    = 16;
        private const int BlocksAmount = 16;

        private IMemoryPool _memoryPool;

        [SetUp]
        public void Setup()
        {
            _memoryPool = MemoryPool.Create(BlockSize, BlocksAmount);
        }

        [TearDown]
        public void Teardown()
        {
            _memoryPool.Dispose();
        }

        [Test]
        public void Allocate_Block()
        {
            Assert.True(_memoryPool.TryAllocate(out var block));

            Assert.AreNotEqual(IntPtr.Zero, block);
            Assert.True(_memoryPool.HasLeakedBlocks);
            Assert.AreEqual(1, _memoryPool.LeakedBlocksCount);
        }
        
        [Test]
        public void Allocate_More_Blocks()
        {
            for (var i = 0; i < BlocksAmount; i++)
            {
                Assert.True(_memoryPool.TryAllocate(out var block));

                Assert.AreNotEqual(IntPtr.Zero, block);
                Assert.True(_memoryPool.HasLeakedBlocks);
                Assert.AreEqual(i + 1, _memoryPool.LeakedBlocksCount);
            }
        }
        
        [Test]
        public void Allocate_Too_Many_Blocks()
        {
            for (var i = 0; i < BlocksAmount; i++)
            {
                Assert.True(_memoryPool.TryAllocate(out var block));

                Assert.AreNotEqual(IntPtr.Zero, block);
                Assert.True(_memoryPool.HasLeakedBlocks);
                Assert.AreEqual(i + 1, _memoryPool.LeakedBlocksCount);
            }

            Assert.True(_memoryPool.IsEmpty);
            Assert.False(_memoryPool.TryAllocate(out _));
        }

        [Test]
        public void Free_Block()
        {
            Assert.True(_memoryPool.TryAllocate(out var block));
            _memoryPool.Free(block);

            Assert.False(_memoryPool.HasLeakedBlocks);
            Assert.AreEqual(0, _memoryPool.LeakedBlocksCount);
        }
        
        [Test]
        public void Free_More_Blocks()
        {
            var blocks = new List<IntPtr>();
            
            for (var i = 0; i < BlocksAmount; i++)
            {
                Assert.True(_memoryPool.TryAllocate(out var block));
                blocks.Add(block);

                Assert.AreNotEqual(IntPtr.Zero, block);
                Assert.True(_memoryPool.HasLeakedBlocks);
                Assert.AreEqual(i + 1, _memoryPool.LeakedBlocksCount);
            }
            
            for (var i = 0; i < BlocksAmount; i++)
            {
                var block = blocks[i];
                
                _memoryPool.Free(block);

                Assert.AreEqual(blocks.Count - (i + 1), _memoryPool.LeakedBlocksCount);
            }
            
            Assert.False(_memoryPool.HasLeakedBlocks);
        }

        [Test]
        public void Is_Owner_Of()
        {
            Assert.True(_memoryPool.TryAllocate(out var block));
            Assert.True(_memoryPool.IsOwnerOf(block));
        }
        
        [Test]
        public void Is_Not_Owner_Of()
        {
            Assert.True(_memoryPool.TryAllocate(out var block));

            block += _memoryPool.AvailableMemoryInBytes + 1;
            
            Assert.False(_memoryPool.IsOwnerOf(IntPtr.MaxValue));
            Assert.False(_memoryPool.IsOwnerOf(IntPtr.MinValue));
            Assert.False(_memoryPool.IsOwnerOf(block));
        }
    }
}