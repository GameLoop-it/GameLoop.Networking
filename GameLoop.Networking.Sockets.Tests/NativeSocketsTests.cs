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
using GameLoop.Utilities.Asserts;
using GameLoop.Utilities.Logs;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameLoop.Utilities.Memory;
using GameLoop.Utilities.Timers;

namespace GameLoop.Networking.Sockets.Tests
{
    public class NativeSocketTests
    {
        private const ushort FixedPort = 29000;
        private const int    MemoryPoolBlockSize = 16;

        private List<NativeSocket> _sockets;
        private IMemoryPool        _memoryPool;

        [SetUp]
        public void Setup()
        {
            Logger.InitializeForConsole();
            _sockets = new List<NativeSocket>();
            _memoryPool = MemoryPool.Create(MemoryPoolBlockSize, 2);
        }

        [TearDown]
        public void Teardown()
        {
            foreach (var nativeSocket in _sockets)
            {
                nativeSocket.Close();
            }
            
            _memoryPool.Dispose();
        }

        [Test]
        public void Is_Bound()
        {
            var socket = BindSocket(FixedPort, out _);

            Assert.True(socket.IsCreated);
        }

        [Test]
        public void Cannot_Be_Bound_On_Already_Used_Port()
        {
            var socket = BindSocket(FixedPort, out _);

            Assert.Throws<AssertFailedException>(() => BindSocket(FixedPort, out _));
        }

        [Test]
        public void Send_And_Receive_Data()
        {
            var socket1 = BindSocket(0, out var address1);
            var socket2 = BindSocket(FixedPort, out var address2);

            var dataToSend      = new byte[] {5, 6, 2, 7, 7, 5, 44, 12, 0, 4};
            _memoryPool.TryAllocate(out var sendBlock);
            var dataToSendBlock = MemoryBlock.Create(sendBlock, dataToSend.Length);
            dataToSendBlock.CopyFrom(dataToSend);
            var dataToSendPtr    = dataToSendBlock.Buffer;
            
            _memoryPool.TryAllocate(out var receiveBlock);
            var receivedData       = MemoryBlock.Create(receiveBlock, MemoryPoolBlockSize);
            var receivedDataPtr    = receivedData.Buffer;
            var receivedDataLength = receivedData.Size;
            var hasReceived        = false;

            NetworkAddress receivedAddress = default;
            int            receivedBytes   = 0;
            
            Assert.AreEqual(dataToSend.Length, socket1.SendTo(address2, dataToSendPtr, dataToSendBlock.Size));
            
            var result = RunAsyncWithTimeout(
                () => hasReceived = socket2.Receive(out receivedAddress, receivedDataPtr, receivedDataLength, out receivedBytes),
                () => hasReceived == true
            );

            Assert.True(result.Success);
            Assert.AreEqual(dataToSend.Length, receivedBytes);
            Assert.AreEqual(address1, receivedAddress);

            for (var i = 0; i < dataToSend.Length; i++)
            {
                Assert.AreEqual(dataToSendBlock[i], receivedData[i]);
            }
        }

        [Test]
        public unsafe void Send_And_Receive_Unsafe_Data()
        {
            var socket1 = BindSocket(0, out var address1);
            var socket2 = BindSocket(FixedPort, out var address2);

            var dataToSend = new byte[] {5, 6, 2, 7, 7, 5, 44, 12, 0, 4};

            var receivedData = new byte[15];
            var hasReceived  = false;

            NetworkAddress receivedAddress = default;
            int            receivedBytes   = 0;

            fixed (byte* dataToSendPtr = dataToSend)
            {
                Assert.AreEqual(dataToSend.Length, socket1.SendTo(address2, dataToSendPtr, dataToSend.Length));
            }

            Task.Run(() =>
            {
                fixed (byte* receivedDataPtr = receivedData)
                {
                    var timer = new Timer();
                    timer.Start();
                    while (true)
                    {
                        hasReceived = socket2.Receive(out receivedAddress, receivedDataPtr, receivedData.Length,
                                                      out receivedBytes);

                        if (hasReceived) return;
                        if (timer.GetElapsedSeconds() >= .5f) return;
                    }
                }
            });
            
            var result = RunAsyncWithTimeout(() => hasReceived == true);
            
            Assert.True(result.Success);
            Assert.AreEqual(dataToSend.Length, receivedBytes);
            Assert.AreEqual(address1, receivedAddress);

            for (var i = 0; i < dataToSend.Length; i++)
            {
                Assert.AreEqual(dataToSend[i], receivedData[i]);
            }
        }

        [Test]
        public unsafe void Send_And_Receive_IntPtr_Data()
        {
            var socket1 = BindSocket(0, out var address1);
            var socket2 = BindSocket(FixedPort, out var address2);

            var dataToSend = new byte[] {5, 6, 2, 7, 7, 5, 44, 12, 0, 4};

            var receivedData = new byte[15];
            var hasReceived  = false;

            NetworkAddress receivedAddress = default;
            int            receivedBytes   = 0;

            fixed (byte* dataToSendPtr = dataToSend)
            {
                var dataPtr = (IntPtr) dataToSendPtr;
                Assert.AreEqual(dataToSend.Length, socket1.SendTo(address2, dataPtr, dataToSend.Length));
            }

            Task.Run(() =>
            {
                fixed (byte* receivedDataPtr = receivedData)
                {
                    var dataPtr = (IntPtr) receivedDataPtr;
                    var timer   = new Timer();
                    timer.Start();
                    while (true)
                    {
                        hasReceived = socket2.Receive(out receivedAddress, dataPtr, receivedData.Length,
                                                      out receivedBytes);

                        if (hasReceived) return;
                        if (timer.GetElapsedSeconds() >= .5f) return;
                    }
                }
            });
            
            var result = RunAsyncWithTimeout(() => hasReceived == true);
            
            Assert.True(result.Success);
            Assert.AreEqual(dataToSend.Length, receivedBytes);
            Assert.AreEqual(address1, receivedAddress);

            for (var i = 0; i < dataToSend.Length; i++)
            {
                Assert.AreEqual(dataToSend[i], receivedData[i]);
            }
        }

        private NativeSocket BindSocket(ushort port, out NetworkAddress address)
        {
            var socket = new NativeSocket();
            address = NetworkAddress.CreateLocalhost(port);
            socket.Bind(ref address);

            _sockets.Add(socket);

            return socket;
        }

        private struct JobResult
        {
            public bool   Success;
            public double Time;

            public JobResult(bool success, double time)
            {
                Success = success;
                Time    = time;
            }
        }

        private JobResult RunAsyncWithTimeout(Func<bool> exitCondition, double timeoutSeconds = .5f)
        {
            return RunAsyncWithTimeout(null, exitCondition, timeoutSeconds);
        }
        
        private JobResult RunAsyncWithTimeout(Action doSomething, Func<bool> exitCondition, double timeoutSeconds = .5f)
        {
            var task = Task.Run(() =>
            {
                var timer = new GameLoop.Utilities.Timers.Timer();
                timer.Start();

                while (timer.GetElapsedSeconds() <= timeoutSeconds)
                {
                    doSomething?.Invoke();
                    if (exitCondition.Invoke())
                    {
                        return new JobResult(true, timer.GetElapsedMilliseconds());
                    }
                }

                return new JobResult(false, timer.GetElapsedMilliseconds());
            });

            Task.WaitAll(task);

            return task.Result;
        }
    }
}