namespace MedulaOnKontrol.Application.Services;

public sealed class KuralKalibrasyonService(IKuralRepository kurallar, IRedRepository redler)
{
    public static ValueTask<int?> CalculateOnerilenAgirlikAsync(int agirlik, KuralMetrik metrik, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(OnerilenAgirlik(agirlik, metrik));
    }

    private static int? OnerilenAgirlik(int agirlik, KuralMetrik metrik)
    {
        var gozlemSayisi = metrik.TruePositiveCount + metrik.FalsePositiveCount + metrik.FalseNegativeCount;
        if (gozlemSayisi < 10)
            return null;
        // Laplace düzeltmesi az gözlemde ağırlığın aşırı değişmesini sınırlar.
        var duzeltilmisF1 = (2m * metrik.TruePositiveCount + 2m) /
            (2m * metrik.TruePositiveCount + metrik.FalsePositiveCount + metrik.FalseNegativeCount + 4m);
        return Math.Clamp((int)decimal.Round(agirlik * (0.5m + duzeltilmisF1), 0, MidpointRounding.AwayFromZero), 1, 100);
    }

    public async Task<IReadOnlyList<KuralKalibrasyon>> ListAsync(ErisimKapsami scope, int donem, CancellationToken cancellationToken)
    {
        var analiz = await redler.AnalyzeAsync(scope, donem, cancellationToken);
        var metrikler = analiz.Metrics.ToDictionary(metrik => metrik.KuralKodu);
        return (await kurallar.ListAsync(cancellationToken)).GroupBy(kural => kural.KuralKodu)
            .Select(grup => grup.MaxBy(kural => kural.Version)!)
            .Select(kural =>
            {
                var metrik = metrikler.GetValueOrDefault(kural.KuralKodu) ?? new KuralMetrik { KuralKodu = kural.KuralKodu };
                return new KuralKalibrasyon(kural, metrik, OnerilenAgirlik(kural.Agirlik, metrik));
            }).OrderBy(satir => satir.Kural.KuralKodu).ToList();
    }

    public async Task<Result<bool>> ApplyAsync(ErisimKapsami scope, KalibrasyonRequest request, CancellationToken cancellationToken)
    {
        if (!KurumTakvimi.IsValidPeriod(request.Donem) || request.YururlukBaslangic.Date <= DateTime.UtcNow.Date ||
            string.IsNullOrWhiteSpace(request.Gerekce) || request.Gerekce.Trim().Length is < 10 or > 700)
            return Result<bool>.Failure("Geçerli gözlem dönemi, ileri yürürlük tarihi ve 10–700 karakter gerekçe girin.");
        var satir = (await ListAsync(scope, request.Donem, cancellationToken)).SingleOrDefault(s => s.Kural.KuralKodu == request.KuralKodu);
        if (satir == null || satir.Kural.Version != request.ExpectedVersion)
            return Result<bool>.Failure("Kural sürümü değişmiş veya bulunamadı. Kalibrasyonu yenileyin.");
        if (!satir.Kural.IsActive || satir.OnerilenAgirlik == null || satir.OnerilenAgirlik == satir.Kural.Agirlik)
            return Result<bool>.Failure("En az 10 gözlem ve aktif kural için farklı bir ağırlık önerisi gereklidir.");
        return await kurallar.VersionAsync(scope, new KuralVersionRequest
        {
            KuralKodu = satir.Kural.KuralKodu, ExpectedVersion = satir.Kural.Version,
            Severity = satir.Kural.Severity, Agirlik = satir.OnerilenAgirlik.Value,
            ParametersJson = satir.Kural.ParametersJson, IsActive = satir.Kural.IsActive,
            StartedAt = request.YururlukBaslangic,
            Gerekce = $"Kalibrasyon dönemi {request.Donem}; TP={satir.Metrik.TruePositiveCount}, FP={satir.Metrik.FalsePositiveCount}, FN={satir.Metrik.FalseNegativeCount}; ağırlık {satir.Kural.Agirlik} → {satir.OnerilenAgirlik}. {request.Gerekce.Trim()}"
        }, cancellationToken);
    }
}
