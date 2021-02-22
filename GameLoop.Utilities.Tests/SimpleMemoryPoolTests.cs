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
using GameLoop.Utilities.Memory;
using NUnit.Framework;

namespace GameLoop.Utilities.Tests
{
    public class SimpleMemoryPoolTests
    {
        private const int BlockSize = 16;

        private SimpleMemoryPool _memoryPool;

        [SetUp]
        public void Setup()
        {
            _memoryPool = SimpleMemoryPool.Create(BlockSize, 1);
        }

        [TearDown]
        public void Teardown()
        {
            _memoryPool.Dispose();
        }

        [Test]
        public void Allocate_Block()
        {
            var block = _memoryPool.Allocate();
            
            Assert.AreNotEqual(IntPtr.Zero, block);
            Assert.True(_memoryPool.HasLeakedBlocks);
            Assert.AreEqual(1, _memoryPool.LeakedBlocksCount);
        }

        [Test]
        public void Free_Block()
        {
            var block = _memoryPool.Allocate();
            _memoryPool.Free(block);
            
            Assert.False(_memoryPool.HasLeakedBlocks);
            Assert.AreEqual(0, _memoryPool.LeakedBlocksCount);
        }
    }
}