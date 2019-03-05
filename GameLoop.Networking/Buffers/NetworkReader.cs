using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace GameLoop.Networking.Buffers
{
    public struct NetworkReader
    {
        private byte[] _buffer;

        private int _bitsPointer;
        private int _bytePointer;
        private byte _currentByte;

        /// <summary>
        /// Initializes the reader and sets the internal buffer for reading.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(ref byte[] buffer)
        {
        	if(buffer.Length <= 0) throw new Exception("Buffer length is not valid");
            _buffer = buffer;
            _currentByte = _buffer[_bytePointer];
        }

        public byte[] GetBuffer()
        {
            return _buffer;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte ReadBits(int bitsAmount)
        {
            if (_bitsPointer == 0 && bitsAmount == 8)
            {
                var tmp = _currentByte;
                _bytePointer += 1;
                _currentByte = _buffer[_bytePointer];
                return tmp;
            }

            int freeBitsMaskOffset = (8 - _bitsPointer - bitsAmount);
            freeBitsMaskOffset = (freeBitsMaskOffset >= 0) ? freeBitsMaskOffset : 0;
            // Remaining bits to read on the next byte in the buffer.
            int leftBitsToRead = bitsAmount - (8 - _bitsPointer);

            byte data = (byte)((_currentByte & ((0xFF << _bitsPointer) & (0xFF >> freeBitsMaskOffset))) >> _bitsPointer);

            if (leftBitsToRead > 0)
            {
                _bytePointer += 1;
                _currentByte = _buffer[_bytePointer];

                var alreadyReadBits = (bitsAmount - leftBitsToRead);
                data = (byte)(data | ((_currentByte << alreadyReadBits) & ((0xFF << alreadyReadBits) & (0xFF >> (8 - bitsAmount)))));
                _bitsPointer = leftBitsToRead;
            }
            else
            {
                _bitsPointer += bitsAmount;
            }

            return data;
        }
        
        /// <summary>
        /// Reads a byte from the buffer.
        /// </summary>
        /// <param name="bits">The number of bits to read.</param>
        public byte ReadByte(int bits = sizeof(byte) * 8)
        {
            return ReadBits(bits);
        }

        /// <summary>
        /// Reads a sbyte from the buffer.
        /// </summary>
        /// <param name="bits">The number of bits to read.</param>
        public sbyte ReadSByte(int bits = sizeof(byte) * 8)
        {
            return (sbyte)ReadBits(bits);
        }

        /// <summary>
        /// Reads a byte array from the buffer.
        /// </summary>
        /// <param name="length">The number of bytes to read.</param>
        public byte[] ReadBytes(int length)
        {
            var bytes = new byte[length];

            if (_bitsPointer == 0)
            {
                System.Buffer.BlockCopy(_buffer, _bytePointer, bytes, 0, length);
                return bytes;
            }

            for (int index = 0; index < length; index++)
            {
                bytes[index] = ReadBits(8);
            }
            return bytes;
        }

        /// <summary>
        /// Reads a bool from the buffer.
        /// </summary>
        public bool ReadBool()
        {
            return ReadBits(1) == 1;
        }

        /// <summary>
        /// Reads a short from the buffer.
        /// </summary>
        /// <param name="bits">The number of bits to read.</param>
        public short ReadShort(int bits = sizeof(short) * 8)
        {
            return (short)ReadUShort(bits);
        }

        /// <summary>
        /// Reads a ushort from the buffer.
        /// </summary>
        /// <param name="bits">The number of bits to read.</param>
        public ushort ReadUShort(int bits = sizeof(short) * 8)
        {
            if (bits <= 8)
            {
                return (ushort)ReadBits(bits);
            }
            else
            {
                return (ushort)(ReadBits(8) | (ReadBits(bits - 8) << 8));
            }
        }

        /// <summary>
        /// Reads a int from the buffer.
        /// </summary>
        /// <param name="bits">The number of bits to read.</param>
        public int ReadInt(int bits = sizeof(int) * 8)
        {
            return (int)ReadUInt(bits);
        }

        /// <summary>
        /// Reads a uint from the buffer.
        /// </summary>
        /// <param name="bits">The number of bits to read.</param>
        public uint ReadUInt(int bits = sizeof(int) * 8)
        {
            if (bits <= 8)
            {
                return (uint)ReadBits(bits);
            }
            else if (bits <= 16)
            {
                return (uint)(ReadBits(8) | (ReadBits(bits - 8) << 8));
            }
            else if (bits <= 24)
            {
                return (uint)(ReadBits(8) | (ReadBits(8) << 8) | (ReadBits(bits - 16) << 16));
            }
            else
            {
                return (uint)(ReadBits(8) | (ReadBits(8) << 8) | (ReadBits(8) << 16) | (ReadBits(bits - 24) << 24));
            }
        }

        /// <summary>
        /// Reads a long from the buffer.
        /// </summary>
        /// <param name="bits">The number of bits to read.</param>
        public long ReadLong(int bits = sizeof(long) * 8)
        {
            return (long)ReadULong(bits);
        }

        /// <summary>
        /// Reads a ulong from the buffer.
        /// </summary>
        /// <param name="bits">The number of bits to read.</param>
        public ulong ReadULong(int bits = sizeof(ulong) * 8)
        {
            if (bits <= 32)
            {
                return ReadUInt(bits);
            }
            else
            {
                ulong first = ReadUInt(32) & 0xFFFFFFFF;
                ulong second = ReadUInt(bits - 32);
                return first | (second << 32);
            }
        }

        /// <summary>
        /// Reads a double from the buffer.
        /// </summary>
        public double ReadDouble()
        {
            var byteConverter = default(ByteConverter);
            byteConverter.Byte0 = ReadBits(8);
            byteConverter.Byte1 = ReadBits(8);
            byteConverter.Byte2 = ReadBits(8);
            byteConverter.Byte3 = ReadBits(8);
            byteConverter.Byte4 = ReadBits(8);
            byteConverter.Byte5 = ReadBits(8);
            byteConverter.Byte6 = ReadBits(8);
            byteConverter.Byte7 = ReadBits(8);

            return byteConverter.Double;
        }

        /// <summary>
        /// Reads a float from the buffer.
        /// </summary>
        public float ReadFloat()
        {
            var byteConverter = default(ByteConverter);
            byteConverter.Byte0 = ReadBits(8);
            byteConverter.Byte1 = ReadBits(8);
            byteConverter.Byte2 = ReadBits(8);
            byteConverter.Byte3 = ReadBits(8);

            return byteConverter.Float;
        }

        /// <summary>
        /// Reads a string from the buffer.
        /// </summary>
        public string ReadString()
        {
            return Encoding.UTF8.GetString(ReadBytes(ReadBits(8)));
        }
    }
}