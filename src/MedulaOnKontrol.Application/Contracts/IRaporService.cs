using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Contracts;
public interface IRaporService
{
    ValueTask<Result<byte[]>> CreatePdfAsync(RaporVerisi data, int donem, CancellationToken cancellationToken);
    ValueTask<Result<byte[]>> CreateExcelAsync(RaporVerisi data, int donem, CancellationToken cancellationToken);
}
