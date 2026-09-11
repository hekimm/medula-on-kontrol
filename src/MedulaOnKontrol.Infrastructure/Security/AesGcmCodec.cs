using System.Security.Cryptography;
using System.Text;
using MedulaOnKontrol.Application;
using MedulaOnKontrol.Domain;

namespace MedulaOnKontrol.Infrastructure.Security;
public sealed class AesGcmCodec(byte[] key)
{
    public string Encrypt(string text)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plain = Encoding.UTF8.GetBytes(text);
        var encrypted = new byte[plain.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, plain, encrypted, tag);
        return "v1:" + Convert.ToBase64String(nonce.Concat(tag).Concat(encrypted).ToArray());
    }

    public string Decrypt(string text)
    {
        if (!text.StartsWith("v1:", StringComparison.Ordinal))
            throw new CryptographicException("Desteklenmeyen şifreli veri sürümü.");
        var bytes = Convert.FromBase64String(text[3..]);
        if (bytes.Length < 28)
            throw new CryptographicException("Geçersiz şifreli veri.");
        var plain = new byte[bytes.Length - 28];
        using var aes = new AesGcm(key, 16);
        aes.Decrypt(bytes.AsSpan(0, 12), bytes.AsSpan(28), bytes.AsSpan(12, 16), plain);
        return Encoding.UTF8.GetString(plain);
    }
}
