using System;
using System.Net;
using GameLoop.Networking.Memory;
using GameLoop.Networking.Sockets;
using Xunit;
using Xunit.Abstractions;

namespace GameLoop.Networking.Tests
{
    public class NetworkSocketTest
    {
        private const int ProtocolId = Int32.MaxValue;
        private readonly ITestOutputHelper _testOutputHelper;

        public NetworkSocketTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void ExchangeData()
        {
            var memoryAllocator = new SimpleManagedAllocator();
            var memoryPool = new SimpleMemoryPool(memoryAllocator);
            var peer1 = new NetworkSocket(memoryPool, ProtocolId);
            var peer2 = new NetworkSocket(memoryPool, ProtocolId);

            var bindingAddress1 = new IPEndPoint(IPAddress.Any, 50000);
            var bindingAddress2 = new IPEndPoint(IPAddress.Any, 50001);
            
            var sendingAddress1 = new IPEndPoint(IPAddress.Loopback, 50001);
            var sendingAddress2 = new IPEndPoint(IPAddress.Loopback, 50000);

            var dataToSend1 = new byte[] {10, 9, 8, 7, 6, 5};
            var dataToSend2 = new byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8};

            byte[] arrivedData1 = default(byte[]);
            EndPoint arrivedEndpoint1 = default(EndPoint);
            bool canProceed1 = false;
            
            byte[] arrivedData2 = default(byte[]);
            EndPoint arrivedEndpoint2 = default(EndPoint);
            bool canProceed2 = false;
            
            peer1.Bind(bindingAddress1);
            peer2.Bind(bindingAddress2);
            
            peer1.SendTo(sendingAddress1, dataToSend1);
            peer2.SendTo(sendingAddress2, dataToSend2);

            while (!canProceed1 || !canProceed2)
            {
                if (peer1.Poll(out NetworkArrivedData data1))
                {
                    arrivedData1 = data1.Data;
                    arrivedEndpoint1 = data1.EndPoint;
                    canProceed1 = true;
                }
                
                if (peer2.Poll(out NetworkArrivedData data2))
                {
                    arrivedData2 = data2.Data;
                    arrivedEndpoint2 = data2.EndPoint;
                    canProceed2 = true;
                }
            }
            
            Assert.True(arrivedData1.Length == dataToSend2.Length);
            Assert.True(arrivedData2.Length == dataToSend1.Length);

            for (int i = 0; i < arrivedData1.Length; i++)
            {
                Assert.Equal(arrivedData1[i], dataToSend2[i]);
                
                //_testOutputHelper.WriteLine(arrivedData1[i].ToString() + " == " + dataToSend2[i].ToString());
            }
            
            for (int i = 0; i < arrivedData2.Length; i++)
            {
                Assert.Equal(arrivedData2[i], dataToSend1[i]);
                
                //_testOutputHelper.WriteLine(arrivedData2[i].ToString() + " == " + dataToSend1[i].ToString());
            }
            
            Assert.Equal(arrivedEndpoint1, sendingAddress1);
            Assert.Equal(arrivedEndpoint2, sendingAddress2);
            
            Assert.True(peer1.Statistics.BytesSent == (ulong)dataToSend1.Length);
            Assert.True(peer2.Statistics.BytesSent == (ulong)dataToSend2.Length);
            Assert.True(peer1.Statistics.BytesReceived == (ulong)dataToSend2.Length);
            Assert.True(peer2.Statistics.BytesReceived == (ulong)dataToSend1.Length);
        }
        
        // TODO: a test for received payloads > PacketMtu
    }
}