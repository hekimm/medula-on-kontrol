using FluentAssertions;
using MedulaOnKontrol.Application;
using MedulaOnKontrol.Domain;
using Xunit;

namespace MedulaOnKontrol.UnitTests;
public sealed class MetrikBirimTests
{
    [Fact]
    public async Task InvoiceWideRulesUseTheSameUnitForTruePositivesAndFalseNegativesAsync()
    {
        var time = new DateTime(2026, 6, 1);
        Bulgu[] predictions = [new()
        {
            FaturaId = 1,
            KuralKodu = "PRV-001",
            CreatedAt = time
        }

        ];
        RedKaydi Red(long fatura, long line) => new()
        {
            FaturaId = fatura,
            FaturaKalemiId = line,
            EslesenKuralKodu = "PRV-001",
            RedTarihi = time.AddDays(1)
        };
        RedKaydi[] redler = [Red(1, 10), Red(1, 11), Red(2, 20), Red(2, 21)];
        var metric = (await KuralMetrikService.CalculateAsync(predictions, redler, default)).Single();
        metric.TruePositiveCount.Should().Be(1);
        metric.FalseNegativeCount.Should().Be(1);
        metric.Recall.Should().Be(.5m);
    }

    [Fact]
    public async Task DuplicateFeedbackIsOrderIndependentAndUsesTheEarliestEventAsync()
    {
        var time = new DateTime(2026, 6, 1);
        Bulgu[] predictions = [new()
        {
            FaturaId = 1,
            FaturaKalemiId = 10,
            KuralKodu = "A",
            CreatedAt = time
        }

        ];
        RedKaydi Red(DateTime date) => new()
        {
            FaturaId = 1,
            FaturaKalemiId = 10,
            EslesenKuralKodu = "A",
            RedTarihi = date
        };
        var metric = (await KuralMetrikService.CalculateAsync(predictions, [Red(time.AddDays(1)), Red(time.AddDays(-1))], default)).Single();
        metric.TruePositiveCount.Should().Be(0);
        metric.FalsePositiveCount.Should().Be(1);
        metric.FalseNegativeCount.Should().Be(1);
    }
}
