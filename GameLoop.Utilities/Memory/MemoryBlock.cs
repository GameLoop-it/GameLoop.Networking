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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using GameLoop.Utilities.Asserts;

namespace GameLoop.Utilities.Memory
{
    [StructLayout(LayoutKind.Explicit)]
    public ref struct MemoryBlock
    {
        public static MemoryBlock InvalidBlock => new MemoryBlock() {Buffer = IntPtr.Zero, Size = -1};
        
        [FieldOffset(0)]
        public int    Size;
        [FieldOffset(4)]
        public IntPtr Buffer;

        public byte this[int index]
        {
            get
            {
                Assert.AlwaysCheck(index < Size);
                unsafe
                {
                    return *((byte*)Buffer + index);
                }
            }
            set
            {
                Assert.AlwaysCheck(index < Size);
                unsafe
                {
                    *((byte*) Buffer + index) = value;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MemoryBlock Create(IntPtr buffer, int size)
        {
            return new MemoryBlock()
            {
                Buffer = buffer,
                Size   = size
            };
        }
        
        public void CopyFrom(MemoryBlock data, int offset, int length)
        {
            unsafe
            {
                System.Buffer.MemoryCopy(((byte*)data.Buffer + offset), (byte*)Buffer, Size, length);
            }
        }

        public void CopyFrom(byte[] data, int offset, int length)
        {
            unsafe
            {
                for (var i = 0; i < length - offset; i++)
                {
                    *((byte*) Buffer + i) = data[offset + i];
                }
            }
        }
        
        public void CopyFrom(byte[] data)
        {
            CopyFrom(data, 0, data.Length);
        }
    }
}