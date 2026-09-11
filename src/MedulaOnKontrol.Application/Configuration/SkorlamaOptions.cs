using MedulaOnKontrol.Domain;
using Microsoft.Extensions.Options;

namespace MedulaOnKontrol.Application.Configuration;
public sealed class SkorlamaOptions
{
    public bool NormalizeWeights { get; set; }
    public Dictionary<Severity, decimal> SeverityMultipliers { get; set; } = new()
    {
        [Severity.Blocking] = 1m,
        [Severity.High] = 0.7m,
        [Severity.Medium] = 0.4m,
        [Severity.Information] = 0.1m
    };
}
