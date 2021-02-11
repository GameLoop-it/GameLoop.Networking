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

using GameLoop.Utilities.Asserts;

namespace GameLoop.Utilities.Memory
{
    public ref struct MemoryBlock
    {
        public static MemoryBlock InvalidBlock => new MemoryBlock() {Buffer = null, Size = -1};
        
        public byte[] Buffer;
        public int    Size;

        public byte this[int index]
        {
            get
            {
                Assert.AlwaysCheck(index < Size);
                return Buffer[index];
            }
            set
            {
                Assert.AlwaysCheck(index < Size);
                Buffer[index] = value;
            }
        }

        public static implicit operator MemoryBlock(byte[] buffer) =>
            new MemoryBlock() {Buffer = buffer, Size = buffer.Length};

        public void CopyFrom(byte[] data, int offset, int length)
        {
            System.Buffer.BlockCopy(data, 0, Buffer, offset, length);
        }
        
        public void CopyFrom(MemoryBlock data, int offset, int length)
        {
            System.Buffer.BlockCopy(data.Buffer, 0, Buffer, offset, length);
        }
    }
}