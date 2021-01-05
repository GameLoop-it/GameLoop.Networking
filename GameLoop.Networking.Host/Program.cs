using System.Collections.Generic;
using System.Net;
using System.Threading;
using GameLoop.Utilities.Logs;

namespace GameLoop.Networking.Host
{
    public class TestPeer
    {
        public const int ServerPort = 25005;

        public  NetworkPeer Peer;
        private bool        _isServer;

        public static IPEndPoint ServerEndpoint => new IPEndPoint(IPAddress.Loopback, ServerPort);

        public TestPeer(bool isServer)
        {
            _isServer = isServer;
            Peer      = new NetworkPeer(GetNetworkContext(isServer));
            
            if (!isServer)
                Peer.Connect(ServerEndpoint);
        }

        private static NetworkContext GetNetworkContext(bool isServer)
        {
            var context = new NetworkContext();
            GetNetworkSettings(isServer, context);

            return context;
        }
        
        private static void GetNetworkSettings(bool isServer, NetworkContext context)
        {
            if (isServer)
                context.Settings.BindingEndpoint = ServerEndpoint;
            else
                context.Settings.BindingEndpoint = new IPEndPoint(IPAddress.Any, 0);
        }

        public void Update()
        {
            Peer.Update();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Logger.InitializeForConsole();

            var peers = new List<TestPeer>();
            peers.Add(new TestPeer(true));
            peers.Add(new TestPeer(false));
            
            foreach (var peer in peers)
            {
                peer.Peer.Start();
            }

            while (true)
            {
                foreach (var peer in peers)
                {
                    peer.Update();
                }
                
                Thread.Sleep(15);
            }
        }
    }
}