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
using GameLoop.Utilities.Exceptions;

namespace GameLoop.Networking.Collections
{
    public class RingBuffer<TItem>
    {
        public int  Count  => _count;
        public bool IsFull => _count == _buffer.Length;

        private TItem[] _buffer;
        private int     _count;
        private int     _head;
        private int     _tail;

        public RingBuffer(int capacity)
        {
            _buffer = new TItem[capacity];
        }

        public void Push(TItem item)
        {
            if (IsFull)
                throw new InvalidOperationException();

            _buffer[_head] =  item;
            _head          =  (_head + 1) % _buffer.Length;
            _count         += 1;
        }

        public TItem Pop()
        {
            var item = _buffer[_tail];

            _buffer[_tail] =  default;
            _tail          =  (_tail + 1) % _buffer.Length;
            _count         -= 1;

            return item;
        }

        public void Clear()
        {
            _count = 0;
            _head  = 0;
            _tail  = 0;

            Array.Clear(_buffer, 0, _buffer.Length);
        }

        public TItem this[int index]
        {
            get => _buffer[((_head + index) % _buffer.Length)];
            set => _buffer[((_head + index) % _buffer.Length)] = value;
        }
    }
}