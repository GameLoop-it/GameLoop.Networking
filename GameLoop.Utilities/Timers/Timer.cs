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
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace GameLoop.Utilities.Timers
{
    public struct Timer
    {
        private long _start;
        private long _elapsed;
        private byte _isRunning;

        public static Timer StartNew()
        {
            var timer = default(Timer);
            timer.Start();
            return timer;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsRunning() => _isRunning == 1;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetElapsedTicks() => IsRunning() ? _elapsed + GetDelta() : _elapsed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetElapsedSeconds() => GetElapsedTicks() / GetFrequency();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetElapsedMilliseconds() => GetElapsedSeconds() * 1000.0;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long GetTimestamp() => Stopwatch.GetTimestamp();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double GetFrequency() => Stopwatch.Frequency;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long GetDelta() => GetTimestamp() - _start;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start()
        {
            if (_isRunning == 1) return;

            _start     = GetTimestamp();
            _isRunning = 1;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Stop()
        {
            var delta = GetDelta();
            
            if (_isRunning == 0) return;

            _elapsed   += delta;
            _isRunning =  0;

            if (_elapsed < 0) 
                _elapsed = 0;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            _start     = 0;
            _elapsed   = 0;
            _isRunning = 0;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Restart()
        {
            _start     = GetTimestamp();
            _elapsed   = 0;
            _isRunning = 1;
        }
    }
}