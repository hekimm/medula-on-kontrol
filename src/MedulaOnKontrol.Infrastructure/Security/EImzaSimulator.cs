using System.Security.Cryptography;
using System.Text;
using MedulaOnKontrol.Application;
using MedulaOnKontrol.Domain;

namespace MedulaOnKontrol.Infrastructure.Security;
public sealed class EImzaSimulator : IEImzaDogrulayici
{
    public Task<bool> VerifyAsync(Kullanici user, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(user.IsActive && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash));
    }
}
