using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Contracts;
public interface ISifrelemeService
{
    ValueTask<string> EncryptAsync(string text, CancellationToken cancellationToken);
    ValueTask<string> DecryptAsync(string text, CancellationToken cancellationToken);
}
