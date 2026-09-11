using FluentAssertions;
using NSubstitute;
using Xunit;

namespace MedulaOnKontrol.UnitTests;

public sealed class KalibrasyonTests
{
    [Fact]
    public async Task InsufficientObservationsNeverProduceWeightChangesAsync()
    {
        (await KuralKalibrasyonService.CalculateOnerilenAgirlikAsync(40, new() { TruePositiveCount = 9 }, default)).Should().BeNull();
        (await KuralKalibrasyonService.CalculateOnerilenAgirlikAsync(40, new(), default)).Should().BeNull();
    }

    [Fact]
    public async Task ReliableRulesGainWeightAndFalseAlarmsLoseWeightWithinBoundsAsync()
    {
        (await KuralKalibrasyonService.CalculateOnerilenAgirlikAsync(40, new() { TruePositiveCount = 100 }, default)).Should().BeGreaterThan(40);
        (await KuralKalibrasyonService.CalculateOnerilenAgirlikAsync(40, new() { FalsePositiveCount = 100 }, default)).Should().BeLessThan(40);
        (await KuralKalibrasyonService.CalculateOnerilenAgirlikAsync(100, new() { TruePositiveCount = 100 }, default)).Should().Be(100);
        (await KuralKalibrasyonService.CalculateOnerilenAgirlikAsync(1, new() { FalseNegativeCount = 100 }, default)).Should().Be(1);
    }

    [Fact]
    public async Task ApplyingCalibrationUsesScopedEvidenceAndPreservesRuleDefinitionAsync()
    {
        var kurallar = Substitute.For<IKuralRepository>();
        var redler = Substitute.For<IRedRepository>();
        var scope = new ErisimKapsami(7, 1);
        var kural = new Kural { KuralKodu = KuralKodlari.Prv001, Version = 2, Agirlik = 30, Severity = Severity.Blocking, IsActive = true, ParametersJson = "{}" };
        kurallar.ListAsync(default).Returns(new List<Kural> { kural });
        redler.AnalyzeAsync(scope, 202606, default).Returns(new RedAnalizi([], [new KuralMetrik { KuralKodu = kural.KuralKodu, TruePositiveCount = 20 }], []));
        kurallar.VersionAsync(scope, Arg.Any<KuralVersionRequest>(), default).Returns(Result<bool>.Success(true));
        var service = new KuralKalibrasyonService(kurallar, redler);
        var baslangic = new DateTime(DateTime.UtcNow.Year + 1, 1, 1);
        var result = await service.ApplyAsync(scope, new() { Donem = 202606, KuralKodu = kural.KuralKodu, ExpectedVersion = 2, YururlukBaslangic = baslangic, Gerekce = "Sentetik gözlemler uzman tarafından incelendi." }, default);
        result.IsSuccess.Should().BeTrue();
        await kurallar.Received(1).VersionAsync(scope, Arg.Is<KuralVersionRequest>(request =>
            request.Agirlik > 30 && request.Severity == Severity.Blocking && request.ExpectedVersion == 2 &&
            request.ParametersJson == "{}" && request.StartedAt == baslangic && request.Gerekce.Contains("TP=20")), default);
        kural.Agirlik.Should().Be(30);
    }

    [Fact]
    public async Task StaleVersionAndMissingEvidenceCannotBeAppliedAsync()
    {
        var kurallar = Substitute.For<IKuralRepository>();
        var redler = Substitute.For<IRedRepository>();
        var scope = new ErisimKapsami(7, 1);
        kurallar.ListAsync(default).Returns(new List<Kural> { new() { KuralKodu = KuralKodlari.Prv001, Version = 3, Agirlik = 30, IsActive = true } });
        redler.AnalyzeAsync(scope, 202606, default).Returns(new RedAnalizi([], [], []));
        var request = new KalibrasyonRequest { Donem = 202606, KuralKodu = KuralKodlari.Prv001, ExpectedVersion = 2, YururlukBaslangic = new(DateTime.UtcNow.Year + 1, 1, 1), Gerekce = "Uzman değerlendirmesi tamamlandı." };
        var service = new KuralKalibrasyonService(kurallar, redler);
        (await service.ApplyAsync(scope, request, default)).IsSuccess.Should().BeFalse();
        request.ExpectedVersion = 3;
        (await service.ApplyAsync(scope, request, default)).IsSuccess.Should().BeFalse();
        await kurallar.DidNotReceive().VersionAsync(Arg.Any<ErisimKapsami>(), Arg.Any<KuralVersionRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvalidCatalogParametersReturnFailureWithoutFindingsAsync()
    {
        var kural = KuralTests.Kural(KuralKodlari.Prv001);
        kural.ParametersJson = "{broken";
        var result = (await KuralTests.Engine().EvaluateAsync(KuralTests.Clean(), [kural], default));
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
    }
}
