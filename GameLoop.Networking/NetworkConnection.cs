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
using GameLoop.Networking.Collections;
using GameLoop.Networking.Statistics;
using GameLoop.Utilities.Asserts;
using GameLoop.Utilities.Logs;

namespace GameLoop.Networking
{
    public struct SendEnvelope
    {
        public ulong  Sequence;
        public double Time;
        public object UserData;
    }
    
    public class NetworkConnection
    {
        public IPEndPoint      RemoteEndpoint;
        public ConnectionState ConnectionState { get; private set; }

        public int    ConnectionAttempts;
        public double LastConnectionAttemptTime;
        public double LastSentPacketTime;
        public double LastReceivedPacketTime;
        public double DisconnectionTime;

        public Sequencer SendSequencer;
        public ulong     LastReceivedSequenceNumber;
        public ulong     ReceivedHistoryMask;

        public RingBuffer<SendEnvelope> SendWindow;
        
        // Initial state
        // LastReceivedSequenceNumber = 0
        // ReceivedHistoryMask = 0000 0000 0000 0000
        
        // First packet comes in
        // LastReceivedSequenceNumber = 1
        // ReceivedHistoryMask = 0000 0000 0000 0000
        
        // Second packet comes in
        // LastReceivedSequenceNumber = 2
        // ReceivedHistoryMask = 1000 0000 0000 0000
        
        // Third packet comes in
        // LastReceivedSequenceNumber = 3
        // ReceivedHistoryMask = 1100 0000 0000 0000
        
        // Fourth packet has been lost
        
        // Fifth packet comes in
        // LastReceivedSequenceNumber = 5
        // ReceivedHistoryMask = 1011 0000 0000 0000

        public readonly NetworkStatistics Statistics;

        public NetworkConnection(NetworkContext context, IPEndPoint remoteEndpoint)
        {
            RemoteEndpoint  = remoteEndpoint;
            ConnectionState = ConnectionState.Created;
            Statistics      = NetworkStatistics.Create();
            SendSequencer   = new Sequencer(context.Settings.SequenceNumberBytes);
            SendWindow      = new RingBuffer<SendEnvelope>(context.Settings.SendWindowSize);
        }

        public void ChangeState(ConnectionState connectionState)
        {
            switch (connectionState)
            {
                case ConnectionState.Connected:
                    Assert.Check(ConnectionState == ConnectionState.Created ||
                                 ConnectionState == ConnectionState.Connecting);
                    break;
                case ConnectionState.Connecting:
                    Assert.Check(ConnectionState == ConnectionState.Created);
                    break;
                case ConnectionState.Disconnected:
                    Assert.Check(ConnectionState == ConnectionState.Connected);
                    break;
            }

            Logger.DebugInfo($"{this} changed state from {ConnectionState} to {connectionState}");
            ConnectionState = connectionState;
        }

        public override string ToString()
        {
            return $"[Connection={RemoteEndpoint} Recv={Statistics.BytesReceived} Sent={Statistics.BytesSent}]";
        }
    }
}