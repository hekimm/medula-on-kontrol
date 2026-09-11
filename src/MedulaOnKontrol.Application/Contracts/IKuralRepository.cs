using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Contracts;
public interface IKuralRepository
{
    Task<IReadOnlyList<Kural>> ListAsync(CancellationToken cancellationToken);
    Task<Result<bool>> VersionAsync(ErisimKapsami scope, KuralVersionRequest request, CancellationToken cancellationToken);
}
