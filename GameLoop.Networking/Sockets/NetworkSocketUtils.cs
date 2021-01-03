using System;
using System.Net.Sockets;

namespace GameLoop.Networking.Sockets
{
    public static class NetworkSocketUtils
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