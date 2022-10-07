using System.Security.Cryptography;

namespace AuthServer.Extensions;

public static class StringExtensions
{
    public static string ReplaceLastOccurrence(this string input, string toFind, string replaceWith)
    {
        var place = input.LastIndexOf(toFind, StringComparison.Ordinal);
        return place == -1
            ? input
            : input.Remove(place, toFind.Length).Insert(place, replaceWith);
    }

    public static string Base64Encode(this string plainText)
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(plainTextBytes);
    }

    public static string Base64Decode(this string base64EncodedData)
    {
        var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
        return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }

    public static string GenerateCryptoSafeToken()
    {
        const int bits = 32;
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(bits));
    }
}