using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Contracts;
public interface IEImzaDogrulayici
{
    Task<bool> VerifyAsync(Kullanici user, string password, CancellationToken cancellationToken);
}
