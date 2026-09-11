using MedulaOnKontrol.Domain;
using Microsoft.Extensions.Options;

namespace MedulaOnKontrol.Application.Rules;
public sealed class KuralEngine(IEnumerable<IFaturaKurali> kurallar, RiskSkorlamaService scoring)
{
    private readonly Dictionary<string, IFaturaKurali> _handlers = kurallar.SelectMany(faturaKurali => typeof(KuralKodlari).GetFields().Select(fieldInfo => (string)fieldInfo.GetValue(null)!).Where(code => code.Split('-')[0] == faturaKurali.Category).Select(code => new KeyValuePair<string, IFaturaKurali>(code, faturaKurali))).ToDictionary();
    public async ValueTask<Result<KontrolSonucu>> EvaluateAsync(FaturaDenetimBaglami context, IEnumerable<Kural> catalog, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var activeRules = catalog.Where(kural => kural.IsActive && FaturaDenetimBaglami.IsValidOn(kural.YururlukBaslangic, kural.YururlukBitis, context.DonemBaslangic)).OrderBy(kural => kural.Severity).ThenBy(kural => kural.KuralKodu).ToList();
        if (activeRules.GroupBy(kural => kural.KuralKodu).Any(group => group.Count() > 1))
            return Result<KontrolSonucu>.Failure("Çakışan kural versiyonları bulundu.");
        List<Bulgu> bulgular = [];
        foreach (var kural in activeRules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_handlers.TryGetValue(kural.KuralKodu, out var handler) || handler.Category != kural.Category)
                return Result<KontrolSonucu>.Failure("Kural uygulayıcısı bulunamadı: " + kural.KuralKodu);
            if (!KuralVersionRequestValidator.IsValidJson(kural.ParametersJson))
                return Result<KontrolSonucu>.Failure("Kural parametreleri geçersiz: " + kural.KuralKodu);
            foreach (var bulgu in handler.Evaluate(context, kural))
            {
                if (context.Exceptions.Any(bulguIstisna => bulguIstisna.KuralKodu == bulgu.KuralKodu && bulguIstisna.KuralVersiyon == bulgu.KuralVersiyon && bulguIstisna.FaturaKalemiId == bulgu.FaturaKalemiId && bulguIstisna.FaturaRevizyon == context.Fatura.Revision && bulguIstisna.GecerlilikBitis > context.UtcNow))
                    bulgu.Status = BulguDurum.IstisnaTanimlandi;
                bulgular.Add(bulgu);
            }
        }

        return Result<KontrolSonucu>.Success(new(0, activeRules.Count, bulgular, await scoring.CalculateAsync(context, bulgular, cancellationToken)));
    }
}
