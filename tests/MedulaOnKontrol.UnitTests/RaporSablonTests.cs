using ClosedXML.Excel;
using FluentAssertions;
using MedulaOnKontrol.Application;
using MedulaOnKontrol.Domain;
using MedulaOnKontrol.Infrastructure;
using Xunit;

namespace MedulaOnKontrol.UnitTests;
public sealed class RaporSablonTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(90)]
    public async Task FormTemplateHandlesEmptyNormalAndMultiplePageReportsAsync(int clinicCount)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var data = Data(clinicCount);
        var service = new RaporService();
        var pdf = (await service.CreatePdfAsync(data, 202609, default)).Value!;
        System.Text.Encoding.ASCII.GetString(pdf[..5]).Should().Be("%PDF-");
        var excel = (await service.CreateExcelAsync(data, 202609, default)).Value!;
        using var workbook = new XLWorkbook(new MemoryStream(excel));
        workbook.Worksheets.Select(worksheet => worksheet.Name).Should().Equal("Dönem özeti", "Bulgular");
        var summary = workbook.Worksheet("Dönem özeti");
        summary.Cell("H1").GetString().Should().Be("MOK.RP.01");
        summary.Cell("H2").DataType.Should().Be(XLDataType.DateTime);
        summary.Cell("C10").GetValue<decimal>().Should().Be(data.Summary.ToplamTutar);
        summary.Cell("G10").GetValue<decimal>().Should().Be(data.Summary.RisktekiTutar);
        summary.PageSetup.PaperSize.Should().Be(XLPaperSize.A4Paper);
        summary.Cell("A8").Style.Fill.BackgroundColor.Color.ToArgb().Should().Be(XLColor.FromHtml("#E6B8B7").Color.ToArgb());
        var details = workbook.Worksheet("Bulgular");
        details.SheetView.SplitRow.Should().Be(8);
        details.PageSetup.LastRowToRepeatAtTop.Should().Be(8);
        details.Cell("A9").GetString().Should().Be("20000000000000001");
        details.Cell("B9").GetString().Should().Be("20000000000000002");
        details.Cell("F9").GetString().Should().Be("=1+1");
        details.Cell("F9").HasFormula.Should().BeFalse();
        details.Cell("I9").GetValue<decimal>().Should().Be(1234.56m);
        var output = Environment.GetEnvironmentVariable("MEDULA_REPORT_QA_DIR");
        if (!string.IsNullOrEmpty(output))
        {
            Directory.CreateDirectory(output);
            File.WriteAllBytes(Path.Combine(output, $"template-{clinicCount}.pdf"), pdf);
            File.WriteAllBytes(Path.Combine(output, $"template-{clinicCount}.xlsx"), excel);
        }
    }

    [Fact]
    public async Task EmptyFindingListIsExplicitAndHasNoDummyDataRowAsync()
    {
        var data = new RaporVerisi(new(), [], []);
        using var workbook = new XLWorkbook(new MemoryStream((await new RaporService().CreateExcelAsync(data, 202609, default)).Value!));
        workbook.Worksheet("Bulgular").Tables.Should().BeEmpty();
        workbook.Worksheet("Bulgular").Cell("A9").GetString().Should().Contain("bulgu bulunmuyor");
        workbook.Worksheet("Dönem özeti").Cell("G12").GetString().Should().Be("—");
    }

    private static RaporVerisi Data(int clinicCount) => new(new() { FaturaSayisi = clinicCount, ToplamTutar = 1234567.89m, RisktekiTutar = 1234.56m, Klinikler = Enumerable.Range(1, clinicCount).Select(i => new OzetSatiri($"TEST KLİNİĞİ {i:00}", 100, 1)).ToList(), Trend = [new(202608, 0, 0), new(202609, 10, 2)] }, [], [new() { FaturaId = 20000000000000001, FaturaKalemiId = 20000000000000002, KuralKodu = "TANI-001", KuralVersiyon = 1, Message = "=1+1", Gerekce = "Türkçe gerekçe; kayıt uzman tarafından incelenmelidir.", OnerilenAksiyon = "Kaydı doğrulayın.", EtkilenenTutar = 1234.56m }]);
}
