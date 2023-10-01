using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Kast.Provider.Supports
{
    public static class Utilities
    {
        public static DateTime? ToDateTime(int? year)
        {
            if (!year.HasValue)
                return null;

            if (DateTime.TryParseExact(year.Value.ToString(), "yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
                return dateTime;
            return null;
        }

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

        public static string StackMethodName(int previous = 1)
             => new StackTrace().GetFrame(previous)?.GetMethod()?.Name ?? "unknown";
    }
}
