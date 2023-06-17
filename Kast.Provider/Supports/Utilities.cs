using System.Net;
using System.Net.Sockets;

namespace Kast.Provider.Supports
{
    public static class Utilities
    {
        public static bool InsensitiveCompare(string? a, string? b)
            => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        public static IPAddress GetLocalIPAddress()
            => Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(ip => ip.AddressFamily == AddressFamily.InterNetwork);

        public static string Capitalize(this string input) =>
            input switch
            {
                null => throw new ArgumentNullException(nameof(input)),
                "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
                _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
            };
    }
}
