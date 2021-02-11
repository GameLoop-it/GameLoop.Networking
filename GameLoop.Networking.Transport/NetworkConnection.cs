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
using System.Net;
using GameLoop.Networking.Sockets;
using GameLoop.Networking.Transport.Statistics;
using GameLoop.Utilities.Asserts;
using GameLoop.Utilities.Collections;
using GameLoop.Utilities.Logs;

namespace GameLoop.Networking.Transport
{
    public struct SendEnvelope
    {
        public ulong  Sequence;
        public double Time;
        public object UserData;
    }

    public class NetworkConnection
    {
        public NetworkAddress  RemoteAddress;
        public ConnectionState ConnectionState { get; private set; }

        public int    ConnectionAttempts;
        public double LastConnectionAttemptTime;
        public double LastSentPacketTime;
        public double LastReceivedPacketTime;
        public double DisconnectionTime;

        public double RoundTripTime;

        public NetworkSequencer SendNetworkSequencer;
        public ulong            LastReceivedSequenceNumber;
        public ulong            ReceivedHistoryMask;

        public RingBuffer<SendEnvelope> SendWindow;

        // Example for ReceivedHistoryMask (on a smaller mask):
        // Initial state
        // LastReceivedSequenceNumber = 0
        // ReceivedHistoryMask = 00000000 00000000

        // First packet comes in
        // LastReceivedSequenceNumber = 1
        // ReceivedHistoryMask = 00000000 00000000

        // Second packet comes in
        // LastReceivedSequenceNumber = 2
        // ReceivedHistoryMask = 00000000 00000001

        // Third packet comes in
        // LastReceivedSequenceNumber = 3
        // ReceivedHistoryMask = 00000000 00000011

        // Fourth packet has been lost

        // Fifth packet comes in
        // LastReceivedSequenceNumber = 5
        // ReceivedHistoryMask = 00000000 00001101

        public readonly NetworkStatistics Statistics;

        public NetworkConnection(NetworkContext context, NetworkAddress remoteAddress)
        {
            RemoteAddress       = remoteAddress;
            ConnectionState      = ConnectionState.Created;
            Statistics           = NetworkStatistics.Create();
            SendNetworkSequencer = new NetworkSequencer(context.Settings.SequenceNumberBytes);
            SendWindow           = new RingBuffer<SendEnvelope>(context.Settings.SendWindowSize);
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
            return
                $"[Connection={RemoteAddress} Recv={Statistics.BytesReceived} Sent={Statistics.BytesSent} RTT={Math.Round(RoundTripTime * 1000, 2)}ms]";
        }
    }
}