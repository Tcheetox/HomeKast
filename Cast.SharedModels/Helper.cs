using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Cast.SharedModels
{
    public static class Helper
    {
        public const string STATIC_FILES_DIRECTORY = "HomeKast";

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

        public static (int Red, int Green, int Blue) HexToRgb(string hex)
        {
            if (hex.StartsWith('#') && hex.Length == 4)
                hex = hex.PadRight(7, hex[^1]);

            var match = Regex.Match(hex, "#?([a-f\\d]{2})([a-f\\d]{2})([a-f\\d]{2})", RegexOptions.IgnoreCase);
            return match.Success ?
                (Convert.ToByte(match.Groups[1].Value, 16), Convert.ToByte(match.Groups[2].Value, 16), Convert.ToByte(match.Groups[3].Value, 16))
                : (0, 0, 0);
        }

        public static string Capitalize(this string input) =>
            input switch
            {
                null => throw new ArgumentNullException(nameof(input)),
                "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
                _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
            };
    }
}
