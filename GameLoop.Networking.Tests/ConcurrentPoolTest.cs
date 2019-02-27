using System.Net;
using GameLoop.Networking.Memory;
using GameLoop.Networking.Sockets;
using Xunit;
using Xunit.Abstractions;

namespace GameLoop.Networking.Tests
{
    public class ConcurrentPoolTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ConcurrentPoolTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void ExchangeData()
        {
            
        }
    }
}