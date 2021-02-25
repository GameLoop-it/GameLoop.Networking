using System;
using GameLoop.Utilities.Memory;
using NUnit.Framework;

namespace GameLoop.Utilities.Tests
{
    public class MemoryManagerTests
    {
        private const int BlocksAmount = 16;
        private const int PacketMtu    = 1280 - (8 + 20);

        private MemoryManager _memoryManager;

        private Random _random;

        [SetUp]
        public void Setup()
        {
            _memoryManager = new MemoryManager(BlocksAmount, PacketMtu);
            _random        = new Random();
        }

        [TearDown]
        public void Teardown()
        {
            _memoryManager.Dispose();
        }

        private int GetRandomAllocationSize()
        {
            return _random.Next(1, PacketMtu);
        }

        [Test]
        public void Allocate_Block()
        {
            var block = _memoryManager.Allocate(GetRandomAllocationSize());

            Assert.True(_memoryManager.HasLeakedBlocks);
            Assert.AreEqual(1, _memoryManager.LeakedBlocksCount);
        }

        [Test]
        public void Allocate_More_Blocks()
        {
            for (var i = 0; i < BlocksAmount; i++)
            {
                var block = _memoryManager.Allocate(GetRandomAllocationSize());

                Assert.True(_memoryManager.HasLeakedBlocks);
                Assert.AreEqual(i + 1, _memoryManager.LeakedBlocksCount);
            }
        }
        
        [Test]
        public void Allocate_Too_Many_Blocks()
        {
            for (var i = 0; i < BlocksAmount; i++)
            {
                var block = _memoryManager.Allocate(3);
                
                Assert.True(_memoryManager.HasLeakedBlocks);
                Assert.AreEqual(i + 1, _memoryManager.LeakedBlocksCount);
            }

            var additionalBlock = _memoryManager.Allocate(3);
            Assert.True(_memoryManager.HasLeakedBlocks);
            Assert.AreEqual(BlocksAmount + 1, _memoryManager.LeakedBlocksCount);
        }

        [Test]
        public void Free_Block()
        {
            var block = _memoryManager.Allocate(GetRandomAllocationSize());
            _memoryManager.Free(block);

            Assert.False(_memoryManager.HasLeakedBlocks);
            Assert.AreEqual(0, _memoryManager.LeakedBlocksCount);
        }

        [Test]
        public unsafe void Free_More_Blocks()
        {
            var blocks = stackalloc MemoryBlock[BlocksAmount];

            for (var i = 0; i < BlocksAmount; i++)
            {
                var block = _memoryManager.Allocate(GetRandomAllocationSize());
                blocks[i] = block;

                Assert.True(_memoryManager.HasLeakedBlocks);
                Assert.AreEqual(i + 1, _memoryManager.LeakedBlocksCount);
            }

            for (var i = 0; i < BlocksAmount; i++)
            {
                var block = blocks[i];

                _memoryManager.Free(block);

                Assert.AreEqual(BlocksAmount - (i + 1), _memoryManager.LeakedBlocksCount);
            }

            Assert.False(_memoryManager.HasLeakedBlocks);
        }
    }
}