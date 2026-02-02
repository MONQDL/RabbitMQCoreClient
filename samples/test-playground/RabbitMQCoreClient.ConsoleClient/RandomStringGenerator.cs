using System;
using System.Text;

namespace RabbitMQCoreClient.ConsoleClient;

public static class RandomStringGenerator
{
    static readonly Random _random = new Random();
    static readonly char[] _validChars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=[]{}|;:,.<>?~ "
            .ToCharArray();

    /// <summary>
    /// Generates a random string of the specified size in UTF-8 (in bytes).
    /// </summary>
    /// <param name="sizeInBytes">The desired string size in bytes.</param>
    /// <returns>A random string that takes up approximately sizeInBytes bytes in UTF-8.</returns>
    public static string GenerateRandomString(long sizeInBytes)
    {
        if (sizeInBytes <= 0)
            throw new ArgumentException("The size must be greater than 0.", nameof(sizeInBytes));

        var sb = new StringBuilder();
        long currentSize = 0;

        while (currentSize < sizeInBytes)
        {
            char c = _validChars[_random.Next(_validChars.Length)];
            sb.Append(c);
            currentSize += Encoding.UTF8.GetByteCount(new[] { c });
        }

        // We cut off the excess if we have gone beyond the limits
        while (Encoding.UTF8.GetByteCount(sb.ToString()) > sizeInBytes)
        {
            sb.Length--;
        }

        return sb.ToString();
    }
}
