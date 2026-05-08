using System.Security.Cryptography;
using System.Text;
using UrlShortener.Application.Links.Interfaces;

namespace UrlShortener.Infrastructure.ShortCodes;

public class ShortCodeGenerator : IShortCodeGenerator
{
    private const string Alphabet =
        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public string Generate(int length = 7)
    {
        var buffer = new StringBuilder(length);
        // RandomNumberGenerator.GetInt32 is cryptographically secure and
        // distributes uniformly without modulo bias, which keeps the chance
        // of duplicates at the level math would predict (~62^7 keyspace).
        for (var i = 0; i < length; i++)
        {
            var index = RandomNumberGenerator.GetInt32(Alphabet.Length);
            buffer.Append(Alphabet[index]);
        }
        return buffer.ToString();
    }
}
