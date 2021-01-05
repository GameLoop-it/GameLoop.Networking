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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using GameLoop.Networking.Memory;

namespace GameLoop.Networking.Buffers
{
    public class FastNetworkWriter : IDisposable
    {
        private const int GrowingFactor = 2;
        private const int ChunkSizeInBits = sizeof(ulong) << 3;
        private const int ChunkSizeInByte = sizeof(ulong);

        private ulong[] _bufferPointer;
        private bool _isBufferInternallyManaged;

        private ulong _currentChunk;
        private int _position;
        private int _bitsPointer;

        private IMemoryPool _memoryPool;

        public void Initialize(IMemoryPool pool, int sizeInByte)
        {
            _memoryPool = pool;
            _isBufferInternallyManaged = true;
            _position = 0;
            _bitsPointer = 0;
            int size = (sizeInByte >> 3) + ((sizeInByte % ChunkSizeInByte == 0) ? 0 : 1);
            _bufferPointer = pool.Rent<ulong>(size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBits(ulong data, int bitsAmount)
        {
            Debug.Assert(bitsAmount > 0 && bitsAmount <= ChunkSizeInBits);

            if (_bitsPointer == 0 && bitsAmount == ChunkSizeInBits)
            {
                _bufferPointer[_position] = data;
                _position++;
                return;
            }

            ulong mask = 0xFFFFFFFFFFFFFFFF << _bitsPointer;
            ulong bufferMask = 0xFFFFFFFFFFFFFFFF >> ChunkSizeInBits - (_bitsPointer);

            _currentChunk = ((data << _bitsPointer) & mask) | (_currentChunk & bufferMask);

            int leftBitsToWrite = bitsAmount - (ChunkSizeInBits - _bitsPointer);
            if (leftBitsToWrite > 0)
            {
                _bufferPointer[_position] = _currentChunk;
                _position++;
                _currentChunk = data >> (_bitsPointer + 1);
                _bitsPointer = leftBitsToWrite;
            }
            else
            {
                _bitsPointer += bitsAmount;
                if(_bitsPointer >= ChunkSizeInBits)
                    Flush();
            }
        }

        public void Flush()
        {
            _bufferPointer[_position] = _currentChunk;
            _position++;
            _bitsPointer = 0;
        }

        public void Dispose()
        {
            _memoryPool.Release(_bufferPointer);
        }
    }
}
