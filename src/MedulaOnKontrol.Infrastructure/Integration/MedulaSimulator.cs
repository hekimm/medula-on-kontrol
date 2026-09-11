using System.Security.Cryptography;
using System.Text;
using Dapper;
using MedulaOnKontrol.Application;
using MedulaOnKontrol.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using static MedulaOnKontrol.Infrastructure.Persistence.OracleCommands;

namespace MedulaOnKontrol.Infrastructure.Integration;
public sealed class MedulaSimulator : IMedulaAdapter
{
    private readonly ResiliencePipeline<MedulaYaniti> _pipeline = new ResiliencePipelineBuilder<MedulaYaniti>().AddRetry(new RetryStrategyOptions<MedulaYaniti> { MaxRetryAttempts = 3, Delay = TimeSpan.FromMilliseconds(100), BackoffType = DelayBackoffType.Exponential, ShouldHandle = new PredicateBuilder<MedulaYaniti>().Handle<TimeoutException>() }).AddCircuitBreaker(new CircuitBreakerStrategyOptions<MedulaYaniti> { FailureRatio = 0.5, MinimumThroughput = 5, SamplingDuration = TimeSpan.FromSeconds(30), BreakDuration = TimeSpan.FromSeconds(10), ShouldHandle = new PredicateBuilder<MedulaYaniti>().Handle<TimeoutException>() }).Build();
    public async Task<MedulaYaniti> SendAsync(Fatura fatura, string submissionKey, CancellationToken cancellationToken) => await _pipeline.ExecuteAsync(async token =>
    {
        await Task.Delay(50, token);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(submissionKey));
        var isAccepted = bytes[0] % 10 != 0;
        return new MedulaYaniti(isAccepted, "SIM-" + Convert.ToHexString(bytes)[..16], isAccepted ? null : "SIM-RED-001");
    }, cancellationToken);
}
