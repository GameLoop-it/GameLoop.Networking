using GameLoop.Networking.Settings;
using GameLoop.Utilities.Asserts;

namespace GameLoop.Networking.Buffers
{
    public class SimpleMemoryManager : IMemoryManager
    {
        private readonly IMemoryPool _2bytesBlock;
        private readonly IMemoryPool _4bytesBlock;
        private readonly IMemoryPool _8bytesBlock;
        private readonly IMemoryPool _16bytesBlock;
        private readonly IMemoryPool _32bytesBlock;
        private readonly IMemoryPool _64bytesBlock;
        private readonly IMemoryPool _128bytesBlock;
        private readonly IMemoryPool _256bytesBlock;
        private readonly IMemoryPool _512bytesBlock;
        private readonly IMemoryPool _1024bytesBlock;
        private readonly IMemoryPool _mtuBytesBlock;

        public SimpleMemoryManager()
        {
            _2bytesBlock   = new SimpleMemoryPool(2, 32);
            _4bytesBlock   = new SimpleMemoryPool(4, 32);
            _8bytesBlock   = new SimpleMemoryPool(8, 32);
            _16bytesBlock  = new SimpleMemoryPool(16, 32);
            _32bytesBlock  = new SimpleMemoryPool(32, 32);
            _64bytesBlock  = new SimpleMemoryPool(64, 32);
            _128bytesBlock = new SimpleMemoryPool(128, 32);
            _256bytesBlock = new SimpleMemoryPool(256, 32);
            _512bytesBlock = new SimpleMemoryPool(512, 32);
            _mtuBytesBlock = new SimpleMemoryPool(NetworkSettings.PacketMtu, 32);
        }

        public byte[] Allocate(int size)
        {
            if (size <= 2) return _2bytesBlock.Allocate();
            if (size <= 4) return _4bytesBlock.Allocate();
            if (size <= 8) return _8bytesBlock.Allocate();
            if (size <= 16) return _16bytesBlock.Allocate();
            if (size <= 32) return _32bytesBlock.Allocate();
            if (size <= 64) return _64bytesBlock.Allocate();
            if (size <= 128) return _128bytesBlock.Allocate();
            if (size <= 256) return _256bytesBlock.Allocate();
            if (size <= 512) return _512bytesBlock.Allocate();
            if (size <= NetworkSettings.PacketMtu) return _mtuBytesBlock.Allocate();

            Assert.AlwaysFail($"The allocated size {size} is not supported.");
            return null;
        }

        public void Free(byte[] block)
        {
            var size = block.Length;

            if (size <= 2)
            {
                _2bytesBlock.Free(block);
                return;
            }

            if (size <= 4)
            {
                _4bytesBlock.Free(block);
                return;
            }

            if (size <= 8)
            {
                _8bytesBlock.Free(block);
                return;
            }

            if (size <= 16)
            {
                _16bytesBlock.Free(block);
                return;
            }

            if (size <= 32)
            {
                _32bytesBlock.Free(block);
                return;
            }

            if (size <= 64)
            {
                _64bytesBlock.Free(block);
                return;
            }

            if (size <= 128)
            {
                _128bytesBlock.Free(block);
                return;
            }

            if (size <= 256)
            {
                _256bytesBlock.Free(block);
                return;
            }

            if (size <= 512)
            {
                _512bytesBlock.Free(block);
                return;
            }

            if (size <= NetworkSettings.PacketMtu)
            {
                _mtuBytesBlock.Free(block);
                return;
            }

            Assert.AlwaysFail($"The memory block size of {size} is not supported.");
        }
    }
}