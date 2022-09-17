using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Cast.SharedModels
{
    public static class Helper
    {
        public static IPAddress GetLocalIPAddress()
            => Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(ip => ip.AddressFamily == AddressFamily.InterNetwork);

        public static string ComputeMD5(string message)
        {
            using MD5 md5 = MD5.Create();
            byte[] input = Encoding.ASCII.GetBytes(message);
            byte[] hash = md5.ComputeHash(input);

            StringBuilder sb = new();
            for (int i = 0; i < hash.Length; i++)
                sb.Append(hash[i].ToString("X2"));
            return sb.ToString();
        }
    }
}
