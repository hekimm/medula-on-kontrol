using Dapper;
using FluentAssertions;
using MedulaOnKontrol.Application;
using MedulaOnKontrol.Domain;
using MedulaOnKontrol.Infrastructure;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using Xunit;
using static MedulaOnKontrol.Infrastructure.Persistence.OracleCommands;

namespace MedulaOnKontrol.IntegrationTests;
[Collection("Oracle")]
public sealed class OracleTests(OracleFixture fixture)
{
    [Fact]
    public async Task SimulatorRejectionCanBeClassifiedWithoutDuplicatingItsAmountAsync()
    {
        var id = await fixture.InvoiceAsync("KARDIYOLOJI");
        await fixture.Service.ValidateAsync(fixture.Admin, id, default);
        (await fixture.Repository.ApproveAsync(fixture.Admin, id, default)).IsSuccess.Should().BeTrue();
        var key = await fixture.Repository.PrepareAsync(fixture.Admin, id, default);
        (await fixture.Repository.CompleteAsync(fixture.Admin, id, key.Value!, new(false, "SIM-CLASSIFY", "SIM-RED-001"), default)).IsSuccess.Should().BeTrue();
        await using var connection = await fixture.Factory.OpenAsync(default);
        var red = await connection.QuerySingleAsync<RedKaydi>("SELECT * FROM RED_KAYDI WHERE FATURA_ID=:Id", new { Id = id });
        var request = new RedSiniflandirmaRequest
        {
            RedId = red.Id, FaturaId = id, KalemId = id * 10,
            KuralKodu = KuralKodlari.Tut001, Gerekce = "Simülatör reddi kaynak kayıtla karşılaştırıldı."
        };
        (await fixture.Repository.ClassifyAsync(new(1, 1), request, default)).IsSuccess.Should().BeFalse();
        (await fixture.Repository.ClassifyAsync(new(8, 2), request, default)).IsSuccess.Should().BeFalse();
        request.KalemId = -1;
        (await fixture.Repository.ClassifyAsync(fixture.Admin, request, default)).IsSuccess.Should().BeFalse();
        request.KalemId = id * 10;
        request.KuralKodu = "INVALID";
        (await fixture.Repository.ClassifyAsync(fixture.Admin, request, default)).IsSuccess.Should().BeFalse();
        request.KuralKodu = KuralKodlari.Tut001;
        var donem = DateTime.UtcNow.Year * 100 + DateTime.UtcNow.Month;
        var before = (await fixture.Repository.AnalyzeAsync(fixture.Admin, donem, default)).Metrics.Single(m => m.KuralKodu == KuralKodlari.Tut001);
        (await fixture.Repository.ClassifyAsync(fixture.Admin, request, default)).IsSuccess.Should().BeTrue();
        (await fixture.Repository.ClassifyAsync(fixture.Admin, request, default)).IsSuccess.Should().BeFalse("stale edits cannot replace a newer classification");
        var after = (await fixture.Repository.AnalyzeAsync(fixture.Admin, donem, default)).Metrics.Single(m => m.KuralKodu == KuralKodlari.Tut001);
        after.FalseNegativeCount.Should().Be(before.FalseNegativeCount + 1);
        var saved = await connection.QuerySingleAsync<RedKaydi>("SELECT * FROM RED_KAYDI WHERE FATURA_ID=:Id", new { Id = id });
        saved.RedTutar.Should().Be(red.RedTutar);
        saved.RedTarihi.Should().Be(red.RedTarihi);
        saved.EslesenKuralKodu.Should().Be(KuralKodlari.Tut001);
        (await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM DENETIM_IZI WHERE VARLIK_ADI='RED_SINIFLANDIRMA' AND VARLIK_ID=:Id AND ESKI_DEGER_JSON IS NOT NULL AND YENI_DEGER_JSON IS NOT NULL", new { Id = red.Id.ToString() })).Should().Be(1);
    }

    [Fact]
    public async Task EveryTableHasRequiredAuditColumnsAsync()
    {
        await using var connection = await fixture.Factory.OpenAsync(default);
        var missing = await connection.QueryAsync<string>("""
            SELECT T.TABLE_NAME FROM USER_TABLES T
            WHERE T.TABLE_NAME NOT LIKE 'BIN$%'
            AND (SELECT COUNT(*) FROM USER_TAB_COLUMNS C WHERE C.TABLE_NAME=T.TABLE_NAME
              AND C.COLUMN_NAME IN ('OLUSTURMA_TARIHI','OLUSTURAN_KULLANICI_ID','GUNCELLEME_TARIHI','GUNCELLEYEN_KULLANICI_ID'))<>4
            """);
        missing.Should().BeEmpty();
    }

    [Fact]
    public async Task InvalidSubmissionCompletionDoesNotChangeInvoiceOrThrowAsync()
    {
        var id = await fixture.InvoiceAsync();
        await fixture.Service.ValidateAsync(fixture.Admin, id, default);
        await fixture.Repository.ApproveAsync(fixture.Admin, id, default);
        var key = await fixture.Repository.PrepareAsync(fixture.Admin, id, default);
        key.IsSuccess.Should().BeTrue();
        (await fixture.Repository.CompleteAsync(fixture.Admin, id, "wrong-revision", new(true, "SIM-TEST", null), default)).IsSuccess.Should().BeFalse();
        (await fixture.Repository.GetAsync(fixture.Admin, id, default))!.Fatura.Status.Should().Be(FaturaDurum.Onaylandi);
        (await fixture.Repository.CompleteAsync(fixture.Admin, id, key.Value!, new(true, "SIM-TEST", null), default)).IsSuccess.Should().BeTrue();
        (await fixture.Repository.CompleteAsync(fixture.Admin, id, key.Value!, new(true, "SIM-TEST", null), default)).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ClassifiedFeedbackMatchesThePersistedSubmissionSnapshotAsync()
    {
        var donem = DateTime.UtcNow.Year * 100 + DateTime.UtcNow.Month;
        var before = (await fixture.Repository.AnalyzeAsync(fixture.Admin, donem, default)).Metrics.Single(m => m.KuralKodu == KuralKodlari.Tut001);
        var id = await fixture.InvoiceAsync();
        await using var connection = await fixture.Factory.OpenAsync(default);
        await connection.ExecuteAsync("UPDATE FATURA_KALEMI SET BIRIM_FIYAT=200,TUTAR=200 WHERE FATURA_ID=:Id", new { Id = id });
        await connection.ExecuteAsync("UPDATE FATURA SET TOPLAM_TUTAR=200 WHERE ID=:Id", new { Id = id });
        var kontrol = await fixture.Service.ValidateAsync(fixture.Admin, id, default);
        kontrol.Value!.Bulgular.Should().Contain(b => b.KuralKodu == KuralKodlari.Tut001);
        (await fixture.Repository.ApproveAsync(fixture.Admin, id, default)).IsSuccess.Should().BeTrue();
        var key = await fixture.Repository.PrepareAsync(fixture.Admin, id, default);
        (await fixture.Repository.CompleteAsync(fixture.Admin, id, key.Value!, new(true, "SIM-METRIK", null), default)).IsSuccess.Should().BeTrue();
        (await fixture.Repository.SaveAsync(fixture.Admin, new RedRequest
        {
            FaturaId = id, KalemId = id * 10, KuralKodu = KuralKodlari.Tut001,
            SgkRedKodu = "TEST-TUTAR", Tutar = 25, KayitTarihi = DateTime.UtcNow,
            Description = "Sentetik dönem fiyatı geri besleme eşleştirmesi."
        }, default)).IsSuccess.Should().BeTrue();
        var after = (await fixture.Repository.AnalyzeAsync(fixture.Admin, donem, default)).Metrics.Single(m => m.KuralKodu == KuralKodlari.Tut001);
        after.TruePositiveCount.Should().Be(before.TruePositiveCount + 1);
        after.FalseNegativeCount.Should().Be(before.FalseNegativeCount);
    }

    [Fact]
    public async Task PendingSubmissionFreezesCorrectionsExceptionsAndRechecksAsync()
    {
        var id = await fixture.InvoiceAsync();
        await fixture.Service.ValidateAsync(fixture.Admin, id, default);
        await fixture.Repository.ApproveAsync(fixture.Admin, id, default);
        var key = await fixture.Repository.PrepareAsync(fixture.Admin, id, default);
        key.IsSuccess.Should().BeTrue();
        var correction = new DuzeltmeRequest
        {
            FaturaId = id,
            Revision = 0,
            Kind = "total",
            Gerekce = "Gönderim sırasında eşzamanlı düzeltme testi."
        };
        (await fixture.Repository.CorrectAsync(fixture.Admin, correction, default)).IsSuccess.Should().BeFalse();
        (await fixture.Service.ValidateAsync(fixture.Admin, id, default)).IsSuccess.Should().BeFalse();
        (await fixture.Repository.AddExceptionAsync(fixture.Admin, new() { FaturaId = id, BulguId = -1, Gerekce = "Gönderim sırasında istisna kontrol testi.", GecerlilikBitis = DateTime.UtcNow.AddDays(1) }, default)).IsSuccess.Should().BeFalse();
        await fixture.Repository.CompleteAsync(fixture.Admin, id, key.Value!, new(true, "SIM-PENDING-TEST", null), default);
        (await fixture.Repository.GetAsync(fixture.Admin, id, default))!.Fatura.Status.Should().Be(FaturaDurum.Gonderildi);
    }

    [Fact]
    public async Task DashboardAndExportsMaterializeOracleNumbersAsync()
    {
        var selectedCount = DateTime.UtcNow.Year * 100 + DateTime.UtcNow.Month;
        var summary = await fixture.Repository.GetSummaryAsync(fixture.Admin, selectedCount, default);
        summary.FaturaSayisi.Should().BeGreaterThan(0);
        summary.Klinikler.Should().NotBeEmpty();
        summary.Trend.Should().HaveCount(6);
        summary.Trend.Last().Donem.Should().Be(selectedCount);
        var reportData = await fixture.Repository.GetReportAsync(fixture.Admin, selectedCount, default);
        reportData.Faturalar.Should().NotBeEmpty();
        (await fixture.Repository.AnalyzeAsync(fixture.Admin, selectedCount, default)).Should().NotBeNull();
    }

    [Fact]
    public async Task RequiredTablesArePartitionedAndCatalogHas40RulesAsync()
    {
        await using var connection = await fixture.Factory.OpenAsync(default);
        (await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM USER_PART_TABLES WHERE TABLE_NAME IN ('FATURA_KALEMI','BULGU')")).Should().Be(2);
        (await fixture.Repository.ListAsync(default)).Select(kural => kural.KuralKodu).Distinct().Should().HaveCount(40);
    }

    [Fact]
    public async Task AuditRejectsUpdateAndDeleteAtDatabaseLevelAsync()
    {
        await using var connection = await fixture.Factory.OpenAsync(default);
        foreach (var sql in new[]
        {
            "UPDATE DENETIM_IZI SET VARLIK_ID='x' WHERE ID=-1",
            "DELETE FROM DENETIM_IZI WHERE ID=-1"
        }

        )
        {
            var act = async () => await connection.ExecuteAsync(sql);
            var exceptionAssertions = await act.Should().ThrowAsync<OracleException>();
            exceptionAssertions.Which.Number.Should().Be(20001);
        }
    }

    [Fact]
    public async Task RepositoryDeniesOtherClinicAndInstitutionAsync()
    {
        var id = await fixture.InvoiceAsync("KARDIYOLOJI");
        (await fixture.Repository.GetAsync(new(1, 1), id, default)).Should().BeNull();
        (await fixture.Repository.GetAsync(new(8, 2), id, default)).Should().BeNull();
        (await fixture.Repository.GetAsync(new(7, 2), id, default)).Should().BeNull();
        (await fixture.Repository.GetAsync(fixture.Admin, id, default)).Should().NotBeNull();
        var result = await fixture.Repository.ListAsync(new(1, 1), new FaturaFilter() { Klinik = "KARDIYOLOJI" }, default);
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task ControlPersistsFindingsAndBlocksApprovalUntilFixedAsync()
    {
        var id = await fixture.InvoiceAsync(provizyon: false);
        var result = await fixture.Service.ValidateAsync(fixture.Admin, id, default);
        result.IsSuccess.Should().BeTrue();
        result.Value!.Bulgular.Should().Contain(bulgu => bulgu.KuralKodu == KuralKodlari.Prv001);
        (await fixture.Repository.ApproveAsync(fixture.Admin, id, default)).IsSuccess.Should().BeFalse();
        var context = (await fixture.Repository.GetAsync(fixture.Admin, id, default))!;
        (await fixture.Repository.CorrectAsync(fixture.Admin, new() { FaturaId = id, Revision = context.Fatura.Revision, Kind = "authorization", Code = "TEST-DUZELTILDI", Gerekce = "Kaynak sentetik provizyon kaydı doğrulandı." }, default)).IsSuccess.Should().BeTrue();
        (await fixture.Service.ValidateAsync(fixture.Admin, id, default)).IsSuccess.Should().BeTrue();
        (await fixture.Repository.ApproveAsync(fixture.Admin, id, default)).IsSuccess.Should().BeTrue();
        await using var connection = await fixture.Factory.OpenAsync(default);
        (await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM KURAL_CALISTIRMA WHERE FATURA_ID=:Id", new { Id = id })).Should().Be(2);
    }

    [Fact]
    public async Task StaleEvaluationCannotOverwriteACorrectionAsync()
    {
        var id = await fixture.InvoiceAsync();
        var context = (await fixture.Repository.GetAsync(fixture.Admin, id, default))!;
        var result = (await fixture.Engine.EvaluateAsync(context, await fixture.Repository.ListAsync(default), default));
        await fixture.Repository.CorrectAsync(fixture.Admin, new() { FaturaId = id, Revision = 0, Kind = "total", Gerekce = "Eşzamanlı revizyon kontrol testi." }, default);
        (await fixture.Repository.SaveValidationAsync(fixture.Admin, context, result.Value!, default)).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task SubmissionUsesSnapshotAndRejectsDuplicateAsync()
    {
        var id = await fixture.InvoiceAsync();
        await fixture.Service.ValidateAsync(fixture.Admin, id, default);
        (await fixture.Repository.ApproveAsync(fixture.Admin, id, default)).IsSuccess.Should().BeTrue();
        var key = await fixture.Repository.PrepareAsync(fixture.Admin, id, default);
        key.IsSuccess.Should().BeTrue();
        (await fixture.Repository.PrepareAsync(fixture.Admin, id, default)).IsSuccess.Should().BeFalse();
        await fixture.Repository.CompleteAsync(fixture.Admin, id, key.Value!, new(true, "SIM-TEST", null), default);
        (await fixture.Repository.PrepareAsync(fixture.Admin, id, default)).IsSuccess.Should().BeFalse();
        (await fixture.Repository.GetAsync(fixture.Admin, id, default))!.Fatura.Status.Should().Be(FaturaDurum.Gonderildi);
    }

    [Fact]
    public async Task RedAmountAndLineOwnershipAreValidatedAsync()
    {
        var id = await fixture.InvoiceAsync();
        await fixture.Service.ValidateAsync(fixture.Admin, id, default);
        await fixture.Repository.ApproveAsync(fixture.Admin, id, default);
        var key = await fixture.Repository.PrepareAsync(fixture.Admin, id, default);
        await fixture.Repository.CompleteAsync(fixture.Admin, id, key.Value!, new(true, "SIM-TEST", null), default);
        var redRequest = new RedRequest
        {
            FaturaId = id,
            KalemId = id * 10,
            SgkRedKodu = "TEST-RED",
            Description = "Sentetik red tutarı sınır testi.",
            Tutar = 300,
            KayitTarihi = DateTime.UtcNow
        };
        (await fixture.Repository.SaveAsync(fixture.Admin, redRequest, default)).IsSuccess.Should().BeFalse();
        redRequest.Tutar = 100;
        (await fixture.Repository.SaveAsync(fixture.Admin, redRequest, default)).IsSuccess.Should().BeTrue();
        redRequest.Tutar = 150;
        (await fixture.Repository.SaveAsync(fixture.Admin, redRequest, default)).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task PlsqlCandidatesRespectClinicScopeAsync()
    {
        var id = await fixture.Repository.EnqueueAsync(new(1, 1), DateTime.UtcNow.Year * 100 + DateTime.UtcNow.Month, default);
        var candidates = await fixture.Repository.GetCandidatesAsync(new() { Id = id, UserId = 1, KurumId = 1, Donem = DateTime.UtcNow.Year * 100 + DateTime.UtcNow.Month }, default);
        candidates.Should().NotBeEmpty();
        await using var connection = await fixture.Factory.OpenAsync(default);
        var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM DENETIM_ADAY A JOIN FATURA F ON F.ID=A.FATURA_ID JOIN BASVURU B ON B.ID=F.BASVURU_ID WHERE A.IS_ID=:Id AND B.KLINIK_KODU<>'DAHILIYE'", new { Id = id });
        count.Should().Be(0);
    }

    [Fact]
    public async Task ExceptionSurvivesRecheckAndOldFindingRemainsOpenAsync()
    {
        var id = await fixture.InvoiceAsync(provizyon: false);
        await fixture.Service.ValidateAsync(fixture.Admin, id, default);
        var bulgu = (await fixture.Repository.ListBulgularAsync(fixture.Admin, id, default)).Single(seciliBulgu => seciliBulgu.KuralKodu == KuralKodlari.Prv001);
        (await fixture.Repository.AddExceptionAsync(fixture.Admin, new() { FaturaId = id, BulguId = bulgu.Id, Gerekce = "Sentetik istisna yaşam döngüsü testi.", GecerlilikBitis = DateTime.UtcNow.AddDays(1) }, default)).IsSuccess.Should().BeTrue();
        var result = await fixture.Service.ValidateAsync(fixture.Admin, id, default);
        result.Value!.Bulgular.Single(seciliBulgu => seciliBulgu.KuralKodu == KuralKodlari.Prv001).Status.Should().Be(BulguDurum.IstisnaTanimlandi);
        await using var connection = await fixture.Factory.OpenAsync(default);
        (await connection.ExecuteScalarAsync<int>("SELECT DURUM FROM BULGU WHERE ID=:Id", new { bulgu.Id })).Should().Be(0);
    }
}
