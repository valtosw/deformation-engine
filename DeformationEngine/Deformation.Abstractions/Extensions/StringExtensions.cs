using System.Globalization;

namespace Deformation.Abstractions.Extensions
{
    public static class StringExtensions
    {
        public static string[] SplitByWhitespace(this string input)
        {
            return input.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        }

        public static float ToFloatInvariant(this string value)
        {
            return float.Parse(value, CultureInfo.InvariantCulture);
        }
    }
}
