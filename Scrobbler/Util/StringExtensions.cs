using System.Security.Cryptography;
using System.Text;

namespace Scrobbler.Util;

public static class StringExtensions
{
    public static bool IsNonEmpty(this string? s) => !string.IsNullOrWhiteSpace(s);

    /// <summary>
    /// Hash a given string to MD5 and return its hex string representation
    /// </summary>
    public static string AsMd5HexString(this string s)
    {
        var inputBytes = Encoding.UTF8.GetBytes(s);
        var hashBytes = MD5.HashData(inputBytes);

        return Convert.ToHexString(hashBytes);
    }
}