using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Contracts;
public interface IGonderimRepository
{
    Task<Result<string>> PrepareAsync(ErisimKapsami scope, long id, CancellationToken cancellationToken);
    Task<Result<bool>> CompleteAsync(ErisimKapsami scope, long id, string submissionKey, MedulaYaniti result, CancellationToken cancellationToken);
}
