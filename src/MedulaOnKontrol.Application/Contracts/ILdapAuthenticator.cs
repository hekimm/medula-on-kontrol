using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Contracts;
public interface ILdapAuthenticator
{
    Task<Result<Kullanici>> AuthenticateAsync(string username, string password, CancellationToken cancellationToken);
}
