using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Contracts;
public interface IDenetimIsiRepository
{
    Task<long> EnqueueAsync(ErisimKapsami scope, int donem, CancellationToken cancellationToken);
    Task<DenetimIsi?> ClaimAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<long>> GetCandidatesAsync(DenetimIsi operation, CancellationToken cancellationToken);
    Task ProgressAsync(long id, int total, int completedCount, int failedCount, string? error, bool isComplete, CancellationToken cancellationToken);
    Task<IReadOnlyList<DenetimIsi>> ListAsync(ErisimKapsami scope, CancellationToken cancellationToken);
}
