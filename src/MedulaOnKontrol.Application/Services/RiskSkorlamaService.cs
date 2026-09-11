using MedulaOnKontrol.Domain;
using Microsoft.Extensions.Options;

namespace MedulaOnKontrol.Application.Services;
public sealed class RiskSkorlamaService(IOptions<SkorlamaOptions> options)
{
    public ValueTask<RiskSonucu> CalculateAsync(FaturaDenetimBaglami context, IReadOnlyList<Bulgu> bulgular, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var acik = bulgular.Where(bulgu => bulgu.Status is BulguDurum.Acik or BulguDurum.KabulEdildi).ToList();
        var total = Math.Max(0, context.Fatura.ToplamTutar);
        var score = total == 0 ? (acik.Count > 0 ? 100 : 0) : Math.Min(100, acik.Sum(bulgu => bulgu.Agirlik / (options.Value.NormalizeWeights ? 100m : 1m) * options.Value.SeverityMultipliers[bulgu.Severity] * Math.Min(total, Math.Max(0, bulgu.EtkilenenTutar)) / total) * 100);
        var risk = acik.Where(bulgu => bulgu.Severity is Severity.Blocking or Severity.High).ToList();
        var amount = risk.Any(bulgu => bulgu.FaturaKalemiId == null) ? total : context.Kalemler.Where(faturaKalemi => risk.Any(bulgu => bulgu.FaturaKalemiId == faturaKalemi.Id)).Sum(faturaKalemi => Math.Max(0, faturaKalemi.Tutar));
        return ValueTask.FromResult(new RiskSonucu(decimal.Round(score, 2), Math.Min(total, amount)));
    }
}
