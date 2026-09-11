using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Services;
public sealed class FaturaKontrolService(IFaturaRepository faturaRepository, IKuralRepository kuralRepository, KuralEngine engine)
{
    public async Task<Result<KontrolSonucu>> ValidateAsync(ErisimKapsami scope, long id, CancellationToken cancellationToken)
    {
        var context = await faturaRepository.GetAsync(scope, id, cancellationToken);
        if (context == null)
            return Result<KontrolSonucu>.Failure("Fatura bulunamadı veya erişim yetkiniz yok.");
        if (context.Fatura.Status is FaturaDurum.Gonderildi or FaturaDurum.Reddedildi)
            return Result<KontrolSonucu>.Failure("Gönderilmiş faturanın kontrol geçmişi değiştirilemez.");
        var result = (await engine.EvaluateAsync(context, await kuralRepository.ListAsync(cancellationToken), cancellationToken));
        return result.IsSuccess ? await faturaRepository.SaveValidationAsync(scope, context, result.Value!, cancellationToken) : result;
    }
}
