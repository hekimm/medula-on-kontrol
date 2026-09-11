using System.Security.Cryptography;
using FluentAssertions;
using MedulaOnKontrol.Application;
using MedulaOnKontrol.Domain;
using MedulaOnKontrol.Infrastructure;
using Xunit;

namespace MedulaOnKontrol.UnitTests;
public sealed class GuvenlikVeMetrikTests
{
    [Fact]
    public async Task AesUsesUniqueNoncesAndAuthenticatesCiphertextAsync()
    {
        var aesEncryptionService = new SifrelemeService(RandomNumberGenerator.GetBytes(32));
        var textValue = (await aesEncryptionService.EncryptAsync("SENTETİK 00000000000", default));
        var selectedTextValue = (await aesEncryptionService.EncryptAsync("SENTETİK 00000000000", default));
        textValue.Should().NotBe(selectedTextValue);
        (await aesEncryptionService.DecryptAsync(textValue, default)).Should().Be("SENTETİK 00000000000");
        var bytes = Convert.FromBase64String(textValue[3..]);
        bytes[^1] ^= 1;
        var act = async () => (await aesEncryptionService.DecryptAsync("v1:" + Convert.ToBase64String(bytes), default));
        await act.Should().ThrowAsync<CryptographicException>();
    }

    [Fact]
    public async Task SimulatorIsIdempotentForASubmissionKeyAsync()
    {
        var medulaSimulator = new MedulaSimulator();
        var fatura = new Fatura
        {
            Id = 1,
            ToplamTutar = 100
        };
        var medulaResponse = await medulaSimulator.SendAsync(fatura, "TEST-1", default);
        var selectedMedulaResponse = await medulaSimulator.SendAsync(fatura, "TEST-1", default);
        medulaResponse.Should().Be(selectedMedulaResponse);
    }

    [Fact]
    public async Task FeedbackMatchesLineAndPreRejectionTimeAsync()
    {
        var before = new DateTime(2026, 6, 1);
        Bulgu[] predictions = [new()
        {
            FaturaId = 1,
            FaturaKalemiId = 10,
            KuralKodu = "A",
            CreatedAt = before
        }, new()
        {
            FaturaId = 2,
            FaturaKalemiId = 20,
            KuralKodu = "A",
            CreatedAt = before
        }

        ];
        RedKaydi[] redler = [new()
        {
            FaturaId = 1,
            FaturaKalemiId = 10,
            EslesenKuralKodu = "A",
            RedTarihi = before.AddDays(1)
        }, new()
        {
            FaturaId = 3,
            FaturaKalemiId = 30,
            EslesenKuralKodu = "A",
            RedTarihi = before.AddDays(1)
        }

        ];
        var kuralMetrik = (await KuralMetrikService.CalculateAsync(predictions, redler, default)).Single();
        kuralMetrik.TruePositiveCount.Should().Be(1);
        kuralMetrik.FalsePositiveCount.Should().Be(1);
        kuralMetrik.FalseNegativeCount.Should().Be(1);
        kuralMetrik.Precision.Should().Be(.5m);
        kuralMetrik.Recall.Should().Be(.5m);
        kuralMetrik.F1.Should().Be(.5m);
        redler[0].FaturaKalemiId = 11;
        RedEslesmesi.Matches(predictions[0], redler[0]).Should().BeFalse();
        redler[0].FaturaKalemiId = 10;
        redler[0].RedTarihi = before.AddDays(-1);
        RedEslesmesi.Matches(predictions[0], redler[0]).Should().BeFalse();
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{\"sql\":1}")]
    [InlineData("{\"periyotGun\":0}")]
    [InlineData("{\"azamiAdet\":1.5}")]
    [InlineData("{\"tolerans\":10}")]
    [InlineData("{\"ekGun\":-1}")]
    [InlineData("{\"tolerans\":0,\"tolerans\":1}")]
    [InlineData("x")]
    public void UnsafeParametersAreRejected(string json) => KuralVersionRequestValidator.IsValidJson(json).Should().BeFalse();
    [Theory]
    [InlineData("{}")]
    [InlineData("{\"tolerans\":0.01}")]
    [InlineData("{\"periyotGun\":30,\"azamiAdet\":1,\"ekGun\":0}")]
    public void BoundedParametersAreAccepted(string json) => KuralVersionRequestValidator.IsValidJson(json).Should().BeTrue();
    [Fact]
    public void NullMetricsAreDistinctFromZero()
    {
        var kuralMetrik = new KuralMetrik();
        kuralMetrik.Precision.Should().BeNull();
        kuralMetrik.Recall.Should().BeNull();
        kuralMetrik.F1.Should().BeNull();
        kuralMetrik.FalseNegativeCount = 1;
        kuralMetrik.Recall.Should().Be(0);
    }

    [Fact]
    public async Task ExportCreatesRealPdfAndXlsxContainersAsync()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var reportService = new RaporService();
        var data = new RaporVerisi(new() { FaturaSayisi = 1, ToplamTutar = 100, RisktekiTutar = 100, Klinikler = [new("DAHİLİYE", 100, 1)] }, [], [new() { FaturaId = 1, KuralKodu = "TANI-001", Gerekce = "Türkçe gerekçe", OnerilenAksiyon = "Kaydı doğrulayın." }]);
        var pdf = (await reportService.CreatePdfAsync(data, 202606, default)).Value!;
        System.Text.Encoding.ASCII.GetString(pdf[..5]).Should().Be("%PDF-");
        var excel = (await reportService.CreateExcelAsync(data, 202606, default)).Value!;
        excel[..2].Should().Equal((byte)'P', (byte)'K');
        using var archive = new System.IO.Compression.ZipArchive(new MemoryStream(excel));
        archive.GetEntry("xl/workbook.xml").Should().NotBeNull();
    }
}
