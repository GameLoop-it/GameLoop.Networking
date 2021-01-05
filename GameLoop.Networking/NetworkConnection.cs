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
using System.Net;
using GameLoop.Utilities.Asserts;
using GameLoop.Utilities.Logs;

namespace GameLoop.Networking
{
    public class NetworkConnection
    {
        public IPEndPoint      RemoteEndpoint;
        public ConnectionState ConnectionState;

        public int    ConnectionAttempts;
        public double LastConnectionAttemptTime;

        public NetworkConnection(IPEndPoint remoteEndpoint)
        {
            RemoteEndpoint  = remoteEndpoint;
            ConnectionState = ConnectionState.Created;
        }

        public void ChangeState(ConnectionState connectionState)
        {
            switch (connectionState)
            {
                case ConnectionState.Connected:
                    Assert.Check(ConnectionState == ConnectionState.Created || ConnectionState == ConnectionState.Connecting);
                    break;
                case ConnectionState.Connecting:
                    Assert.Check(ConnectionState == ConnectionState.Created);
                    break;
            }
            
            Logger.DebugInfo($"{RemoteEndpoint} changed state from {ConnectionState} to {connectionState}");
            ConnectionState = connectionState;
        }

        public override string ToString()
        {
            return $"[Connection RemoteEndpoint={RemoteEndpoint}]";
        }
    }
}