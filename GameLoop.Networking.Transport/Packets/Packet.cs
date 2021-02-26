/*
The MIT License (MIT)

Copyright (c) 2020 Emanuele Manzione, Fredrik Holmstrom

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

namespace GameLoop.Networking.Transport.Packets
{
    public struct Packet
    {
        public IntPtr Data;
        public int    Offset;
        public int    Length;

        public Span<byte> Span
        {
            get
            {
                unsafe
                {
                    return new Span<byte>(((byte*) Data), Length);
                }
            }
        }
        
        public Span<byte> SpanFromOffset
        {
            get
            {
                unsafe
                {
                    return new Span<byte>(((byte*) Data + Offset), Length - Offset);
                }
            }
        }

        public Packet(MemoryBlock block)
        {
            Data   = block.Buffer;
            Offset = 0;
            Length = block.Size;
        }

        public static Packet Create(IntPtr data, int length)
        {
            return new Packet()
            {
                Data   = data,
                Offset = 0,
                Length = length
            };
        }
    }
}