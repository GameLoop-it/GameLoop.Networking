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
using System.Net.Sockets;

namespace GameLoop.Networking.Sockets
{
    internal static class NetworkSocketUtils
    {
        private const uint IocIn     = 0x80000000;
        private const uint IocVendor = 0x18000000;
        private const int  IocCode   = unchecked((int) (IocIn | IocVendor | 12));

        private static readonly byte[] IocValue = {Convert.ToByte(false)};

        public static void SetConnectionReset(Socket socket)
        {
            try
            {
                socket.IOControl(IocCode, IocValue, null);
            }
            catch
            {
            }
        }
    }
}