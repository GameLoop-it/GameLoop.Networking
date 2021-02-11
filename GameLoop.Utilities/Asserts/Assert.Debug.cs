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

namespace GameLoop.Utilities.Asserts
{
    public static partial class Assert
    {
        [Conditional("DEBUG")]
        [DebuggerHidden]
        public static void Fail()
        {
            throw new AssertFailedException();
        }

        [Conditional("DEBUG")]
        [DebuggerHidden]
        public static void Fail(string message)
        {
            throw new AssertFailedException(message);
        }

        [Conditional("DEBUG")]
        [DebuggerHidden]
        public static void Fail(string message, params object[] parameters)
        {
            throw new AssertFailedException(string.Format(message, parameters));
        }
        
        [Conditional("DEBUG")]
        [DebuggerHidden]
        public static void Check(bool condition)
        {
            if (condition == false) Fail();
        }
        
        [Conditional("DEBUG")]
        [DebuggerHidden]
        public static void Check(bool condition, string message)
        {
            if (condition == false) Fail(message);
        }
        
        [Conditional("DEBUG")]
        [DebuggerHidden]
        public static void Check(bool condition, string message, params object[] parameters)
        {
            if (condition == false) Fail(message, parameters);
        }
        
        [Conditional("DEBUG")]
        [DebuggerHidden]
        public static void NotNull(object obj)
        {
            if (obj == null) Fail();
        }
        
        [Conditional("DEBUG")]
        [DebuggerHidden]
        public static void NotNull(object obj, string message)
        {
            if (obj == null) Fail(message);
        }
    }
}