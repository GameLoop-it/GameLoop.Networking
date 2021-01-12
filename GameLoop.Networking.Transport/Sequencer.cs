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

namespace GameLoop.Networking.Transport
{
    public struct Sequencer
    {
        private int   _shift;
        private int   _bytes;
        private ulong _mask;
        private ulong _sequence;

        public Sequencer(int bytes)
        {
            _bytes    = bytes;
            _sequence = 0;

            // Example:
            // bytes = 1
            // (8 - 1) * 8 = 56
            _shift = (sizeof(ulong) - bytes) * 8;

            // Example:
            // bytes = 1
            // 1 << 8 = 256
            // - 1    = 255
            //        = 1111 1111
            _mask = (1UL << (bytes * 8)) - 1UL;
        }

        public ulong NextAfter(ulong sequence)
        {
            // This is how it wraps. Faster than modulo operator.
            // Example:
            // _bytes       = 1
            // _mask        = 1111 1111
            // sequence     = 1111 1111        - it is at its maximum now
            // sequence + 1 = 0001 0000 0000   - so if I add 1 it has to wrap around to 0
            // & _mask = 0001 0000 0000 & 1111 1111 = 0000 0000   - it now starts at 0 again, it wrapped around
            return (sequence + 1UL) & _mask;
        }

        public ulong Next()
        {
            return _sequence = NextAfter(_sequence);
        }

        public long Distance(ulong from, ulong to)
        {
            // Example:
            // _bytes = 1
            // _shift = 56
            // from = 1
            // to = 2
            // from << 56 = 72.057.594.037.927.936 = 0000 0001 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000
            // to << 56 = 144.115.188.075.855.872  = 0000 0010 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000
            // from - to = -72.057.594.037.927.936 = 1111 1111 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000
            // from - to >> 56 = -1
            to   = to   << _shift;
            from = from << _shift;

            return ((long) (from - to)) >> _shift;
        }
    }
}