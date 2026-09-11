using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Contracts;
public interface IRedRepository
{
    Task<Result<bool>> SaveAsync(ErisimKapsami scope, RedRequest request, CancellationToken cancellationToken);
    Task<Result<bool>> ClassifyAsync(ErisimKapsami scope, RedSiniflandirmaRequest request, CancellationToken cancellationToken);
    Task<RedAnalizi> AnalyzeAsync(ErisimKapsami scope, int donem, CancellationToken cancellationToken);
}
