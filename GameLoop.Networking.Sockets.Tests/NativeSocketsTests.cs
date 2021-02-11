using System;
using GameLoop.Utilities.Asserts;
using GameLoop.Utilities.Logs;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameLoop.Networking.Sockets.Tests
{
    public class NativeSocketTests
    {
        private const ushort FixedPort = 29000;

        private List<NativeSocket> _sockets;

        [SetUp]
        public void Setup()
        {
            Logger.InitializeForConsole();
            _sockets = new List<NativeSocket>();
        }

        [TearDown]
        public void Teardown()
        {
            foreach (var nativeSocket in _sockets)
            {
                nativeSocket.Close();
            }
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

            var dataToSend1 = new byte[] {5, 6, 2, 7, 7, 5, 44, 12, 0, 4};

            var receivedData = new byte[15];
            var hasReceived  = false;

            NetworkAddress receivedAddress = default;
            int            receivedBytes   = 0;

            socket1.Connect(ref address2);
            Assert.AreEqual(dataToSend1.Length, socket1.SendTo(address2, dataToSend1));

            var result = RunAsyncWithTimeout(
                () => hasReceived = socket2.Receive(out receivedAddress, receivedData, out receivedBytes),
                () => hasReceived == true
            );

            Assert.True(result.Success);
            Assert.AreEqual(dataToSend1.Length, receivedBytes);
            Assert.AreEqual(address1, receivedAddress);

            for (var i = 0; i < dataToSend1.Length; i++)
            {
                Assert.AreEqual(dataToSend1[i], receivedData[i]);
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

        private JobResult RunAsyncWithTimeout(Action doSomething, Func<bool> exitCondition, double timeoutSeconds = .5f)
        {
            var task = Task.Run(() =>
            {
                var timer = new GameLoop.Utilities.Timers.Timer();
                timer.Start();

                while (timer.GetElapsedSeconds() <= timeoutSeconds)
                {
                    doSomething.Invoke();
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