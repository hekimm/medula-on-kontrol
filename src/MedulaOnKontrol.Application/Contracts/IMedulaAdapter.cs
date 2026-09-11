using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Contracts;
public interface IMedulaAdapter
{
    Task<MedulaYaniti> SendAsync(Fatura fatura, string submissionKey, CancellationToken cancellationToken);
}
