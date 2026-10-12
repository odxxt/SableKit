using System.Security.Cryptography;

namespace SableKit.Core;

public static class Passwords
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%&*+-=?";
    public static string Generate(int length)
    {
        if (length is < 8 or > 128) throw new ArgumentOutOfRangeException(nameof(length), "Choose 8–128 characters.");
        return string.Create(length, 0, (span, _) => { for (var i = 0; i < span.Length; i++) span[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)]; });
    }
}
