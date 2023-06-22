namespace Kast.Api.Extensions
{
    internal static class StringExtension
    {
        public static string LowerFirstLetter(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            char firstChar = char.ToLower(input[0]);
            string restOfString = input.Length > 1 ? input[1..] : string.Empty;

            return firstChar + restOfString;
        }
    }
}
