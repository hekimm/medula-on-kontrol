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
public sealed class AnonimlestirmeWorker(IOracleConnectionFactory factory, ISifrelemeService encryptionService, IOptions<SaklamaOptions> options, ILogger<AnonimlestirmeWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (options.Value.IsActive)
                {
                    await using var connection = await factory.OpenAsync(stoppingToken);
                    using var transaction = connection.BeginTransaction();
                    var ids = (await connection.QueryAsync<long>(CreateCommand("SELECT H.ID FROM HASTA H WHERE H.ANONIM_MI=0 AND H.OLUSTURMA_TARIHI<:Esik AND NOT EXISTS(SELECT 1 FROM BASVURU B JOIN FATURA F ON F.BASVURU_ID=B.ID WHERE B.HASTA_ID=H.ID AND (F.DURUM NOT IN(4,5) OR F.OLUSTURMA_TARIHI>=:Esik)) FOR UPDATE SKIP LOCKED", new { Esik = DateTime.UtcNow.AddDays(-options.Value.Days) }, stoppingToken, transaction))).ToList();
                    foreach (var id in ids)
                    {
                        await connection.ExecuteAsync(CreateCommand("UPDATE HASTA SET AD_SOYAD=:Name,KIMLIK_NO=:RevealIdentity,DOGUM_TARIHI=DATE '1900-01-01',CINSIYET=0,ANONIM_MI=1,GUNCELLEME_TARIHI=SYS_EXTRACT_UTC(SYSTIMESTAMP),GUNCELLEYEN_KULLANICI_ID=0 WHERE ID=:Id", new { Id = id, Name = (await encryptionService.EncryptAsync("ANONİMLEŞTİRİLDİ", stoppingToken)), RevealIdentity = (await encryptionService.EncryptAsync("00000000000", stoppingToken)) }, stoppingToken, transaction));
                        await connection.ExecuteAsync(CreateCommand("INSERT INTO DENETIM_IZI(KULLANICI_ID,KURUM_ID,ISLEM_TIPI,VARLIK_ADI,VARLIK_ID,YENI_DEGER_JSON,IP_ADRES) SELECT DISTINCT 0,KURUM_ID,2,'HASTA_ANONIMLESTIRME',:EntityId,:Yeni,'arka-plan' FROM BASVURU WHERE HASTA_ID=:Id", new { Id = id, EntityId = id.ToString(), Yeni = "{\"AnonimMi\":true}" }, stoppingToken, transaction));
                    }

                    transaction.Commit();
                    if (ids.Count > 0)
                        logger.LogInformation("Saklama süresi dolan {Count} hasta anonimleştirildi", ids.Count);
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Anonimleştirme işi başarısız");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
