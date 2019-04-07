using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using GameLoop.Networking.Buffers;
using GameLoop.Networking.Memory;
using Xunit;
using Xunit.Abstractions;

namespace GameLoop.Networking.Tests
{
    public class NetworkWriterAndReaderTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private Stopwatch _timer;
        private IMemoryPool _memoryPool;
        
        public NetworkWriterAndReaderTest(ITestOutputHelper testOutputHelper)
        {
            var memoryAllocator = new SimpleManagedAllocator();
            _memoryPool = new SimpleMemoryPool(memoryAllocator);
            _timer = new Stopwatch();
            _testOutputHelper = testOutputHelper;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StartMeasure()
        {
            _timer.Restart();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StopMeasure()
        {
            _timer.Stop();
            _testOutputHelper.WriteLine("Ticks spent: " + _timer.ElapsedTicks);
            _testOutputHelper.WriteLine("Milliseconds spent: " + _timer.Elapsed.TotalMilliseconds);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeTest(out NetworkReader reader, out NetworkWriter writer, int size)
        {
            reader = default(NetworkReader);
            writer = default(NetworkWriter);
            writer.Initialize(_memoryPool, size);
        }
        
        [Fact]
        public void WriteAndReadByte()
        {
            byte v = 63;
            InitializeTest(out var reader, out var writer, 1);
            
            writer.Write(v);

            var buffer = writer.GetBuffer();
            reader.Initialize(ref buffer);
            
            Assert.True(reader.ReadByte() == v);
        }
        
        [Fact]
        public void WriteAndReadShort()
        {
            short v = 63;
            InitializeTest(out var reader, out var writer, 2);
            
            writer.Write(v);

            var buffer = writer.GetBuffer();
            reader.Initialize(ref buffer);
            
            Assert.True(reader.ReadShort() == v);
        }
        
        [Fact]
        public void WriteAndReadInt()
        {
            int v = 63;
            InitializeTest(out var reader, out var writer, 4);
            
            writer.Write(v);

            var buffer = writer.GetBuffer();
            reader.Initialize(ref buffer);
            
            Assert.True(reader.ReadInt() == v);
        }
        
        [Fact]
        public void WriteAndReadLong()
        {
            long v = 63;
            InitializeTest(out var reader, out var writer, 8);
            
            writer.Write(v);

            var buffer = writer.GetBuffer();
            reader.Initialize(ref buffer);
            
            Assert.True(reader.ReadLong() == v);
        }
        
        [Fact]
        public void WriteAndReadFloat()
        {
            float v = 63.721f;
            InitializeTest(out var reader, out var writer, 4);
            
            writer.Write(v);

            var buffer = writer.GetBuffer();
            reader.Initialize(ref buffer);
            
            Assert.True(reader.ReadFloat() == v);
        }
        
        [Fact]
        public void WriteAndReadDouble()
        {
            double v = 63.721f;
            InitializeTest(out var reader, out var writer, 8);
            
            writer.Write(v);

            var buffer = writer.GetBuffer();
            reader.Initialize(ref buffer);
            
            Assert.True(reader.ReadDouble() == v);
        }
        
        [Fact]
        public void WriteAndReadBytes()
        {
            byte[] v = new byte[5] { 2, 39, 11, 94, 19};
            InitializeTest(out var reader, out var writer, 1);
            
            writer.Write(v.Length);
            writer.Write(v);

            var buffer = writer.GetBuffer();
            reader.Initialize(ref buffer);

            var length = reader.ReadInt();
            var read = reader.ReadBytes(length);

            for (int i = 0; i < length; i++)
            {
                Assert.True(read[i] == v[i]);
            }
        }
        
        [Fact]
        public void WriteAndReadBool()
        {
            bool v = true;
            InitializeTest(out var reader, out var writer, 1);
            
            writer.Write(v);

            var buffer = writer.GetBuffer();
            reader.Initialize(ref buffer);
            
            Assert.True(reader.ReadBool() == v);
        }
        
        [Fact]
        public void WriteAndReadBits()
        {
            int v = int.MaxValue >> 10;
            InitializeTest(out var reader, out var writer, 1);
            
            writer.Write(v, 22);

            var buffer = writer.GetBuffer();
            reader.Initialize(ref buffer);

            var read = reader.ReadInt(22);
            Assert.True(read == int.MaxValue >> 10);
            Assert.False(read == int.MaxValue >> 11);
            Assert.False(read == int.MaxValue >> 9);
        }
        
        [Fact]
        public void WriteAndReadWithResizing()
        {
            long v1 = 63, v2 = 980, v3 = 291102, v4 = 1919;
            InitializeTest(out var reader, out var writer, 1);
            
            writer.Write(v1);
            writer.Write(v2);
            writer.Write(v3);
            writer.Write(v4);

            var buffer = writer.GetBuffer();
            reader.Initialize(ref buffer);
            
            Assert.True(reader.ReadLong() == v1);
            Assert.True(reader.ReadLong() == v2);
            Assert.True(reader.ReadLong() == v3);
            Assert.True(reader.ReadLong() == v4);
        }
    }
}