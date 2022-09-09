using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Cast.SharedModels
{
    public static class Helper
    {
        public static IPAddress GetIPAddress()
            => Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(ip => ip.AddressFamily == AddressFamily.InterNetwork);
    }
}
