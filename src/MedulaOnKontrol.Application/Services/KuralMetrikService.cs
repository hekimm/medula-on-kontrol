using MedulaOnKontrol.Domain;

namespace MedulaOnKontrol.Application.Services;
public static class KuralMetrikService
{
    public static ValueTask<IReadOnlyList<KuralMetrik>> CalculateAsync(IEnumerable<Bulgu> bulguGecmisi, IEnumerable<RedKaydi> redGecmisi, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var predictions = bulguGecmisi.OrderBy(bulgu => bulgu.CreatedAt).DistinctBy(bulgu => (bulgu.FaturaId, bulgu.FaturaKalemiId, bulgu.KuralKodu)).ToList();
        var redler = redGecmisi.Where(red => red.EslesenKuralKodu != null).OrderBy(red => red.RedTarihi).DistinctBy(red => (red.FaturaId, red.FaturaKalemiId, red.EslesenKuralKodu)).ToList();
        // Fatura genelindeki kurallarda tahmin ve red aynı fatura/kural birimini kullanır.
        // Kalem ve fatura birimlerini karıştırmak recall hesabını bozar.
        var genelKodlar = predictions.Where(bulgu => bulgu.FaturaKalemiId == null).Select(bulgu => bulgu.KuralKodu).ToHashSet();
        var metrics = new List<KuralMetrik>();
        foreach (var kuralKodu in predictions.Select(bulgu => bulgu.KuralKodu).Concat(redler.Select(red => red.EslesenKuralKodu!)).Distinct().Order())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var prediction = predictions.Where(bulgu => bulgu.KuralKodu == kuralKodu).ToList();
            var gercek = redler.Where(red => red.EslesenKuralKodu == kuralKodu).ToList();
            var kuralMetrik = new KuralMetrik
            {
                KuralKodu = kuralKodu
            };
            if (genelKodlar.Contains(kuralKodu))
            {
                foreach (var id in prediction.Select(bulgu => bulgu.FaturaId).Concat(gercek.Select(red => red.FaturaId)).Distinct())
                {
                    var matchingFindings = prediction.Where(bulgu => bulgu.FaturaId == id).ToList();
                    var matchingRejections = gercek.Where(red => red.FaturaId == id).ToList();
                    var isMatched = matchingFindings.Any(bulgu => matchingRejections.Any(red => RedEslesmesi.Matches(bulgu, red)));
                    if (isMatched)
                        kuralMetrik.TruePositiveCount++;
                    else
                    {
                        if (matchingFindings.Count > 0)
                            kuralMetrik.FalsePositiveCount++;
                        if (matchingRejections.Count > 0)
                            kuralMetrik.FalseNegativeCount++;
                    }
                }
            }
            else
            {
                kuralMetrik.TruePositiveCount = prediction.Count(bulgu => gercek.Any(red => RedEslesmesi.Matches(bulgu, red)));
                kuralMetrik.FalsePositiveCount = prediction.Count(bulgu => !gercek.Any(red => RedEslesmesi.Matches(bulgu, red)));
                kuralMetrik.FalseNegativeCount = gercek.Count(red => !prediction.Any(bulgu => RedEslesmesi.Matches(bulgu, red)));
            }

            metrics.Add(kuralMetrik);
        }

        return ValueTask.FromResult<IReadOnlyList<KuralMetrik>>(metrics);
    }
}
