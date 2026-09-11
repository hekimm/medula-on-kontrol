namespace MedulaOnKontrol.Infrastructure.Security;

public sealed class SifrelemeService(AesGcmCodec codec) : ISifrelemeService
{
    public SifrelemeService(byte[] key) : this(new AesGcmCodec(key)) { }

    public ValueTask<string> EncryptAsync(string text, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(codec.Encrypt(text));
    }

    public ValueTask<string> DecryptAsync(string text, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(codec.Decrypt(text));
    }
}
