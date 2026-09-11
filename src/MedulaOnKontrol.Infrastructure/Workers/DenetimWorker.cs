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

namespace MedulaOnKontrol.Infrastructure.Workers;
public sealed class DenetimWorker(IServiceScopeFactory scopeFactory, ILogger<DenetimWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var jobs = scope.ServiceProvider.GetRequiredService<IDenetimIsiRepository>();
            DenetimIsi? operation = null;
            try
            {
                operation = await jobs.ClaimAsync(stoppingToken);
                if (operation == null)
                {
                    await Task.Delay(1500, stoppingToken);
                    continue;
                }

                var user = await scope.ServiceProvider.GetRequiredService<IKullaniciRepository>().GetAsync(operation.UserId, stoppingToken);
                if (user == null || !user.IsActive || user.KurumId != operation.KurumId || !user.Roller.Any(textValue => textValue is Roller.Manager or Roller.SystemAdministrator or Roller.RevenueOfficer))
                {
                    await jobs.ProgressAsync(operation.Id, 0, 0, 1, "İş sahibinin dönem denetim yetkisi yok.", true, stoppingToken);
                    continue;
                }

                var result = await scope.ServiceProvider.GetRequiredService<DonemDenetimService>().RunAsync(operation, stoppingToken);
                logger.LogInformation("Dönem denetimi tamamlandı: {JobId}, {Count} fatura", operation.Id, result.FaturaSayisi);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Dönem denetimi başarısız: {JobId}", operation?.Id);
                if (operation != null)
                    await jobs.ProgressAsync(operation.Id, 0, 0, 1, "İş tamamlanamadı. Sistem günlüğündeki iş numarasını inceleyin.", true, stoppingToken);
                await Task.Delay(3000, stoppingToken);
            }
        }
    }
}
