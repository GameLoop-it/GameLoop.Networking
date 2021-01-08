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

using System.Collections.Generic;
using GameLoop.Utilities.Logs;

namespace GameLoop.Networking.Memory
{
    public sealed class SimpleMemoryPool : IMemoryPool
    {
        private readonly Queue<byte[]> _availableBlocks;
        private readonly List<byte[]>  _unavailableBlocks;

        private readonly int _blockSize;

        public SimpleMemoryPool(int blockSize, int amount)
        {
            _availableBlocks   = new Queue<byte[]>(amount);
            _unavailableBlocks = new List<byte[]>(amount);

            _blockSize = blockSize;

            for (var i = 0; i < amount; i++)
            {
                _availableBlocks.Enqueue(new byte[blockSize]);
            }
        }

        public byte[] Allocate()
        {
            lock (_availableBlocks)
            {
                if (_availableBlocks.Count > 0)
                {
                    var block = _availableBlocks.Dequeue();
                    _unavailableBlocks.Add(block);

                    Logger.DebugInfo($"Retrieved memory block from {_blockSize}-pool.");
                    
                    return block;
                }
            }
            
            Logger.DebugInfo($"Allocated un-pooled memory block from {_blockSize}-pool.");

            return new byte[_blockSize];
        }

        public void Free(byte[] block)
        {
            lock (_availableBlocks)
            {
                for (var i = 0; i < _unavailableBlocks.Count; i++)
                {
                    var currentBlock = _unavailableBlocks[i];

                    if (currentBlock == block)
                    {
                        _unavailableBlocks.RemoveAt(i);
                        break;
                    }
                }

                _availableBlocks.Enqueue(block);
            }
            
            Logger.DebugInfo($"Released memory block to {_blockSize}-pool.");
        }

        public void Dispose()
        {
        }
    }
}