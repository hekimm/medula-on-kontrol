using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Contracts;
public interface IDenetimRepository
{
    Task AppendAsync(DenetimIzi auditEntry, CancellationToken cancellationToken);
    Task<PagedResult<DenetimIzi>> ListAsync(ErisimKapsami scope, DenetimFilter filter, CancellationToken cancellationToken);
}
