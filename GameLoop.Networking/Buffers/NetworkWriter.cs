using System;
using System.Runtime.CompilerServices;
using System.Text;
using GameLoop.Networking.Memory;

namespace GameLoop.Networking.Buffers
{
    public struct NetworkWriter : IDisposable
    {
        private const int GrowingFactor = 2;
        
        private byte[] _buffer;
        private bool _isBufferInternallyManaged;

        private int _bitsPointer;
        private int _bytePointer;
        private byte _currentByte;

        private IMemoryPool _pool;

        public byte[] GetBuffer()
        {
            return _buffer;
        }

        public int GetSize()
        {
            return _bytePointer + 1;
        }

        /// <summary>
        /// Initializes the writer and sets the internal buffer for writing.
        /// </summary>
        public void Initialize(ref byte[] buffer)
        {
            if(buffer.Length <= 0) throw new Exception("Buffer length is not valid");
            _buffer = buffer;
            _currentByte = _buffer[_bytePointer];
            _isBufferInternallyManaged = false;
        }
        
        /// <summary>
        /// Initializes the writer and sets the internal buffer for writing.
        /// </summary>
        public void Initialize(IMemoryPool pool, ref byte[] buffer)
        {
            _pool = pool;
            var newBuffer = pool.Rent(buffer.Length);
            Buffer.BlockCopy(buffer, 0, newBuffer, 0, buffer.Length);
            _buffer = newBuffer;
        }
        
        /// <summary>
        /// Initializes the writer.
        /// </summary>
        public void Initialize(IMemoryPool pool, int size)
        {
            _pool = pool;
            _isBufferInternallyManaged = true;
            _buffer = pool.Rent(size);
        }
        
        public byte[] ToByteArray()
        {
            var bytes = new byte[_bytePointer + 1];
            System.Buffer.BlockCopy(_buffer, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public void ToByteArray(ref byte[] buffer)
        {
            if(buffer.Length < _bytePointer + 1)
                throw new Exception("The buffer is not large enough.");
            
            Buffer.BlockCopy(_buffer, 0, buffer, 0, _bytePointer + 1);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureBufferSpace(int additionalSpaceInBits)
        {
            var length = _buffer.Length;
            if ((_bytePointer << 3) + _bitsPointer + additionalSpaceInBits > length << 3)
            {
                if(!_isBufferInternallyManaged)
                    throw new Exception("The buffer is not managed by this writer and cannot be resized.");
                var tmpBuffer = _buffer;
                var newBuffer = _pool.Rent((length + (additionalSpaceInBits >> 8) + 1) * GrowingFactor);
                System.Buffer.BlockCopy(tmpBuffer, 0, newBuffer, 0, tmpBuffer.Length);
                _pool.Release(_buffer);
                _buffer = newBuffer;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteBits(byte data, int bitsAmount)
        {
            if (bitsAmount <= 0) return;
            if (bitsAmount > 8) bitsAmount = 8;

            if (_bitsPointer == 0 && bitsAmount == 8)
            {
                _currentByte = data;
                _buffer[_bytePointer] = data;
                _bytePointer += 1;
                return;
            }
            
            // Left bits in the current byte in the buffer.
            //int leftBits = 8 - _bitsPointer - bitsAmount;
            // If it is < 0, we have to move to the next byte.
            //leftBits = (leftBits < 0) ? 0 : leftBits;
            // Remaining bits to write to the next byte.
            int leftBitsToWrite = bitsAmount - (8 - _bitsPointer);

            // This is the mask to preserve old written bits (0xFF << _bitsPointer)
            // and to allow writing of new ones.
            var mask = (0xFF << _bitsPointer); // & (0xFF >> leftBits); // Do we really need to mask out bits > _bitsPointer + leftBits?

            // Write to the current byte.
            _currentByte = (byte)((_currentByte & (0xFF >> (8 - _bitsPointer))) | ((data << _bitsPointer) & mask));
            // Write the current byte to the buffer.
            _buffer[_bytePointer] = _currentByte;

            // If we have left bits to write, we have to advance the buffer pointer.
            if (leftBitsToWrite > 0)
            {
                _bytePointer += 1;
                _currentByte = 0x00;

                _currentByte = (byte)((_currentByte) | ((data >> (bitsAmount - leftBitsToWrite)) & (0xFF >> 8 - leftBitsToWrite)));
                _buffer[_bytePointer] = _currentByte;

                _bitsPointer = leftBitsToWrite;
            }
            // Else we can just increment our bits pointer.
            else
            {
                _bitsPointer += bitsAmount;
                if (_bitsPointer >= 8)
                {
                    _bitsPointer = 0;
                    _bytePointer += 1;
                    _currentByte = 0x00;
                }
            }
        }
        
        /// <summary>
        /// Writes a byte on the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="bits">The number of bits to write.</param>
        public void Write(byte value, int bits = sizeof(byte) * 8)
        {
            EnsureBufferSpace(bits);
            WriteBits(value, bits);
        }

        /// <summary>
        /// Writes a sbyte on the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="bits">The number of bits to write.</param>
        public void Write(sbyte value, int bits = sizeof(byte) * 8)
        {
            Write((byte)value, bits);
        }

        /// <summary>
        /// Writes a byte array on the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(byte[] value)
        {
            EnsureBufferSpace(value.Length * 8);

            if (_bitsPointer == 0)
            {
                System.Buffer.BlockCopy(value, 0, _buffer, _bytePointer, value.Length);
                _bytePointer += value.Length;
                _currentByte = _buffer[_bytePointer];
                return;
            }

            for (var index = 0; index < value.Length; index++)
                Write(value[index]);
        }

        /// <summary>
        /// Writes the NetworkWriter content on the buffer.
        /// </summary>
        /// <param name="buffer">The content to write.</param>
        public void Write(NetworkWriter buffer)
        {
            Write(buffer._buffer);
        }
        
        /// <summary>
        /// Writes the NetworkReader content on the buffer.
        /// </summary>
        /// <param name="buffer">The content to write.</param>
        public void Write(NetworkReader buffer)
        {
            Write(buffer.GetBuffer());
        }

        /// <summary>
        /// Writes a bool on the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(bool value)
        {
            EnsureBufferSpace(1);
            WriteBits((value) ? (byte)1 : (byte)0, 1);
        }

        /// <summary>
        /// Writes a short on the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="bits">The number of bits to write.</param>
        public void Write(short value, int bits = sizeof(short) * 8)
        {
            Write((ushort)value, bits);
        }

        /// <summary>
        /// Writes a ushort on the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="bits">The number of bits to write.</param>
        public void Write(ushort value, int bits = sizeof(short) * 8)
        {
            EnsureBufferSpace(bits);
            if (bits <= 8)
            {
                WriteBits((byte)value, bits);
            }
            else
            {
                WriteBits((byte)value, 8);
                WriteBits((byte)(value >> 8), bits - 8);
            }
        }

        /// <summary>
        /// Writes a int on the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="bits">The number of bits to write.</param>
        public void Write(int value, int bits = sizeof(int) * 8)
        {
            Write((uint)value, bits);
        }

        /// <summary>
        /// Writes a uint on the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="bits">The number of bits to write.</param>
        public void Write(uint value, int bits = sizeof(uint) * 8)
        {
            EnsureBufferSpace(bits);
            if (bits <= 8)
            {
                WriteBits((byte)value, bits);
            }
            else if (bits <= 16)
            {
                WriteBits((byte)value, 8);
                WriteBits((byte)(value >> 8), bits - 8);
            }
            else if (bits <= 24)
            {
                WriteBits((byte)value, 8);
                WriteBits((byte)(value >> 8), 8);
                WriteBits((byte)(value >> 16), bits - 16);
            }
            else
            {
                WriteBits((byte)value, 8);
                WriteBits((byte)(value >> 8), 8);
                WriteBits((byte)(value >> 16), 8);
                WriteBits((byte)(value >> 24), bits - 24);
            }
        }

        /// <summary>
        /// Writes a long on the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="bits">The number of bits to write.</param>
        public void Write(long value, int bits = sizeof(long) * 8)
        {
            Write((ulong)value, bits);
        }

        /// <summary>
        /// Writes a ulong on the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="bits">The number of bits to write.</param>
        public void Write(ulong value, int bits = sizeof(ulong) * 8)
        {
            EnsureBufferSpace(bits);

            if (bits <= 32)
            {
                Write((uint)value, bits);
            }
            else
            {
                Write((uint)value, 32);
                Write((uint)(value >> 32), bits - 32);
            }
        }

        /// <summary>
        /// Writes a double on the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(double value)
        {
            EnsureBufferSpace(sizeof(double) * 8);

            var byteConverter = default(ByteConverter);
            byteConverter.Double = value;

            WriteBits(byteConverter.Byte0, 8);
            WriteBits(byteConverter.Byte1, 8);
            WriteBits(byteConverter.Byte2, 8);
            WriteBits(byteConverter.Byte3, 8);
            WriteBits(byteConverter.Byte4, 8);
            WriteBits(byteConverter.Byte5, 8);
            WriteBits(byteConverter.Byte6, 8);
            WriteBits(byteConverter.Byte7, 8);
        }

        /// <summary>
        /// Writes a float on the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(float value)
        {
            EnsureBufferSpace(sizeof(float) * 8);

            var byteConverter = default(ByteConverter);
            byteConverter.Float = value;

            WriteBits(byteConverter.Byte0, 8);
            WriteBits(byteConverter.Byte1, 8);
            WriteBits(byteConverter.Byte2, 8);
            WriteBits(byteConverter.Byte3, 8);
        }

        /// <summary>
        /// Writes a string on the buffer. The max allowed size
        /// of the string is 256 (the length is sent as byte).
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(string value)
        {
            EnsureBufferSpace(sizeof(char) * 8 * (value.Length + 1));

            WriteBits((byte)value.Length, 8);
            Write(Encoding.UTF8.GetBytes(value));
        }

        public void Dispose()
        {
            if (_isBufferInternallyManaged)
            {
                _pool.Release(_buffer);
            }
        }
    }
}