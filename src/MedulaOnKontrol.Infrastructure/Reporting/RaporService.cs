using System.Globalization;
using ClosedXML.Excel;
using MedulaOnKontrol.Application;
using MedulaOnKontrol.Domain;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MedulaOnKontrol.Infrastructure.Reporting;
public sealed class RaporService : IRaporService
{
    private static readonly CultureInfo _turkishCulture = CultureInfo.GetCultureInfo("tr-TR");
    private const string SectionHeaderColor = "E6B8B7";
    private const string AlternateRowColor = "F2F2F2";
    private const string MoneyFormat = "#,##0.00 \"TL\"";
    private const string ReportCode = "MOK.RP.01";
    private const string Notice = "Tüm veriler sentetiktir. Bu sistem bir karar destek aracıdır. Nihai kodlama ve faturalandırma sorumluluğu hekim ve tıbbi kodlama uzmanındadır. Resmî SGK uyumluluk onayı değildir.";
    private const string ReportLogoSvg = """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64">
          <circle cx="32" cy="32" r="29" fill="white" stroke="#AC5555" stroke-width="1.5"/>
          <circle cx="32" cy="32" r="24" fill="none" stroke="#AC5555" stroke-width="0.6"/>
          <path d="M18 43V25H25V28Q32 21 37 29Q44 21 48 29V43H42V32Q39 28 36 33V43H30V32Q27 28 24 33V43Z" fill="#AC5555"/>
          <path d="M31 11V19M27 15H35" stroke="#AC5555" stroke-width="2"/>
        </svg>
        """;
    private sealed record ReportCell(object Value, string? Format = null)
    {
        public string Text => Value is IFormattable number ? number.ToString(Format, _turkishCulture) : Value.ToString() ?? "";
        public bool Numeric => Value is int or long or decimal;
    }

    private sealed record Section(string Title, int[] Spans, string[]? Headers, List<ReportCell[]> Rows);
    private static ReportCell TextCell(string text) => new(text);
    private static ReportCell CountCell(int count) => new(count, "#,##0");
    private static ReportCell MoneyCell(decimal amount) => new(amount, MoneyFormat);
    private static ReportCell PercentageCell(decimal? ratio) => ratio.HasValue ? new(ratio.Value, "0.00%") : TextCell("—");
    private static string FormatPeriod(int donem)
    {
        var first = new DateTime(donem / 100, donem % 100, 1);
        return $"{first:dd.MM.yyyy} - {first.AddMonths(1).AddDays(-1):dd.MM.yyyy}";
    }

    private static DateTime GetReportTime() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"));
    private static List<Section> CreateSections(RaporVerisi reportData)
    {
        var summary = reportData.Summary;
        var sent = reportData.Faturalar.Count(fatura => fatura.Status is FaturaDurum.Gonderildi or FaturaDurum.Reddedildi);
        var current = reportData.Faturalar.Count(fatura => fatura.SonCalistirmaId.HasValue && fatura.KontrolRevizyon == fatura.Revision);
        return [new("ÖZET GÖSTERGELER", [2, 2, 2, 2], null, [[TextCell("Toplam fatura"), CountCell(summary.FaturaSayisi), TextCell("Engelleyici bulgulu fatura"), CountCell(summary.EngelleyiciFatura)], [TextCell("Toplam fatura tutarı"), MoneyCell(summary.ToplamTutar), TextCell("Riskteki tutar"), MoneyCell(summary.RisktekiTutar)], [TextCell("Güncel kontrolü bulunan fatura"), CountCell(current), TextCell("Gönderimi sonuçlanan fatura"), CountCell(sent)], [TextCell("Tahmini risk azalması"), MoneyCell(summary.OnlenenTahminiTutar), TextCell("Riskteki tutarın payı"), PercentageCell(summary.ToplamTutar > 0 ? summary.RisktekiTutar / summary.ToplamTutar : null)]]), new("KLİNİK BAZLI SONUÇLAR", [3, 1, 2, 2], ["Klinik", "Fatura sayısı", "Riskteki tutar", "Toplam riskteki pay"], summary.Klinikler.Select(summaryRow => new[] { TextCell(summaryRow.Name), CountCell(summaryRow.Adet), MoneyCell(summaryRow.Tutar), PercentageCell(summary.RisktekiTutar > 0 ? summaryRow.Tutar / summary.RisktekiTutar : null) }).ToList()), new("KURAL BAZLI BULGU DAĞILIMI", [3, 2, 3], ["Kural kodu", "Bulgu sayısı", "Etkilenen tutar"], summary.Bulgular.Take(10).Select(summaryRow => new[] { TextCell(summaryRow.Name), CountCell(summaryRow.Adet), MoneyCell(summaryRow.Tutar) }).ToList()), new("DÖNEMSEL TREND", [2, 2, 2, 2], ["Dönem", "Gönderilen fatura", "Reddedilen fatura", "Red oranı"], summary.Trend.Select(periodTrend => new[] { TextCell($"{periodTrend.Donem % 100:00}.{periodTrend.Donem / 100}"), CountCell(periodTrend.SubmittedCount), CountCell(periodTrend.RejectedCount), PercentageCell(periodTrend.SubmittedCount > 0 ? (decimal)periodTrend.RejectedCount / periodTrend.SubmittedCount : null) }).ToList()), new("KONTROL SONUÇLARININ DEĞERLENDİRİLMESİ", [2, 2, 4], ["Gösterge", "Sonuç", "Açıklama"], [[TextCell("Güncel kontrol oranı"), PercentageCell(summary.FaturaSayisi > 0 ? (decimal)current / summary.FaturaSayisi : null), TextCell("Güncel revizyonu kontrol edilmiş faturaların payı.")], [TextCell("Engelleyici fatura oranı"), PercentageCell(summary.FaturaSayisi > 0 ? (decimal)summary.EngelleyiciFatura / summary.FaturaSayisi : null), TextCell("Gönderimden önce açık engelleyici bulgular çözülmelidir.")], [TextCell("Tahmini risk azalması"), MoneyCell(summary.OnlenenTahminiTutar), TextCell("İlk ve son kontrol farkıdır; tahsilat veya önlenmiş red garantisi değildir.")]])];
    }

    public ValueTask<Result<byte[]>> CreatePdfAsync(RaporVerisi reportData, int donem, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!KurumTakvimi.IsValidPeriod(donem))
            return ValueTask.FromResult(Result<byte[]>.Failure("Geçerli bir fatura dönemi gereklidir."));
        var bytes = CreatePdf(reportData, donem, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(Result<byte[]>.Success(bytes));
    }

    private static byte[] CreatePdf(RaporVerisi reportData, int donem, CancellationToken cancellationToken)
    {
        var period = FormatPeriod(donem);
        var date = GetReportTime();
        var sections = CreateSections(reportData);
        return Document.Create(documentContainer => documentContainer.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(23);
            page.DefaultTextStyle(textStyle => textStyle.FontFamily("Lato").FontSize(8).FontColor("000000"));
            page.Header().Column(column =>
            {
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(57);
                        columns.RelativeColumn();
                        columns.ConstantColumn(67);
                        columns.ConstantColumn(62);
                    });
                    table.Cell().RowSpan(4).Element(container => Grid(container).AlignCenter().AlignMiddle().Width(47).Height(47)).Svg(ReportLogoSvg);
                    table.Cell().RowSpan(3).Element(container => Grid(container).AlignCenter().AlignMiddle()).Column(selectedColumn =>
                    {
                        selectedColumn.Item().AlignCenter().Text("MEDULA ÖN KONTROL").Bold().FontSize(9);
                        selectedColumn.Item().AlignCenter().Text("FATURA DENETİM BİRİMİ").Bold().FontSize(8);
                        selectedColumn.Item().AlignCenter().Text("SENTETİK UYGULAMA ORTAMI").FontSize(7);
                    });
                    HeaderLabel(table, "Sayfa No");
                    table.Cell().Element(container => Grid(container).AlignCenter()).Text(textDescriptor =>
                    {
                        textDescriptor.CurrentPageNumber();
                        textDescriptor.Span(" / ");
                        textDescriptor.TotalPages();
                    });
                    HeaderLabel(table, "Doküman Kodu");
                    HeaderValue(table, ReportCode);
                    HeaderLabel(table, "Düzenleme Tarihi");
                    HeaderValue(table, date.ToString("dd.MM.yyyy"));
                    table.Cell().Element(container => Grid(container).AlignCenter()).Text("FATURA ÖN KONTROL VE DENETİM RAPORU").Bold().FontSize(8);
                    HeaderLabel(table, "Şablon Revizyonu");
                    HeaderValue(table, "01");
                });
                column.Item().PaddingTop(6).Element(container => Grid(container).AlignCenter()).Text($"Raporlama Dönemi: {period}").Bold();
            });
            page.Content().PaddingTop(6).Column(column =>
            {
                column.Spacing(6);
                foreach (var section in sections)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    column.Item().Element(box => PdfSection(box, section));
                }
                column.Item().PaddingTop(1).Text("Not: Kural dağılımı en sık ilk 10 bulguyu içerir. Aynı kalemdeki farklı bulguların tutarları bu tabloda tekrar edebilir. Riskteki tutar hesabında kalemler tekilleştirilir. Gönderim olmayan dönemlerde red oranı hesaplanmaz.").FontSize(7);
            });
            page.Footer().PaddingTop(6).Column(column =>
            {
                column.Item().BorderTop(0.6f).PaddingTop(4).Text(Notice).FontSize(7);
                column.Item().PaddingTop(3).AlignRight().Text($"Oluşturulma: {date:dd.MM.yyyy HH:mm} (Türkiye saati)").FontSize(6.5f);
            });
        })).GeneratePdf();
    }

    private static IContainer Grid(IContainer container, string background = "FFFFFF") => container.Border(0.55f).BorderColor("000000").Background(background).PaddingHorizontal(4).PaddingVertical(2.4f);
    private static void HeaderLabel(TableDescriptor table, string text) => table.Cell().Element(container => Grid(container).AlignCenter()).Text(text).Bold().FontSize(6.6f);
    private static void HeaderValue(TableDescriptor table, string text) => table.Cell().Element(container => Grid(container).AlignCenter()).Text(text).FontSize(7);
    private static void PdfSection(IContainer container, Section section)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var span in section.Spans)
                    columns.RelativeColumn(span);
            });
            table.Header(tableCellDescriptor =>
            {
                tableCellDescriptor.Cell().ColumnSpan((uint)section.Spans.Length).Element(selectedContainer => Grid(selectedContainer, SectionHeaderColor).AlignCenter()).Text(section.Title).Bold().FontSize(8.2f);
                if (section.Headers != null)
                    foreach (var header in section.Headers)
                        tableCellDescriptor.Cell().Element(selectedContainer => Grid(selectedContainer, SectionHeaderColor).AlignCenter()).Text(header).Bold().FontSize(7.2f);
            });
            if (section.Rows.Count == 0)
                table.Cell().ColumnSpan((uint)section.Spans.Length).Element(selectedContainer => Grid(selectedContainer)).Text("Bu dönemde listelenecek kayıt bulunmuyor.");
            for (var row = 0; row < section.Rows.Count; row++)
            {
                for (var columnIndex = 0; columnIndex < section.Rows[row].Length; columnIndex++)
                {
                    var value = section.Rows[row][columnIndex];
                    var cell = Grid(table.Cell().ShowEntire(), row % 2 == 1 ? AlternateRowColor : "FFFFFF");
                    if (value.Numeric)
                        cell = cell.AlignRight();
                    var text = cell.Text(value.Text);
                    if (section.Headers == null && columnIndex % 2 == 0)
                        text.Bold();
                }
            }
        });
    }

    public ValueTask<Result<byte[]>> CreateExcelAsync(RaporVerisi reportData, int donem, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!KurumTakvimi.IsValidPeriod(donem))
            return ValueTask.FromResult(Result<byte[]>.Failure("Geçerli bir fatura dönemi gereklidir."));
        var bytes = CreateExcel(reportData, donem, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(Result<byte[]>.Success(bytes));
    }

    private static byte[] CreateExcel(RaporVerisi reportData, int donem, CancellationToken cancellationToken)
    {
        var period = FormatPeriod(donem);
        var date = GetReportTime();
        using var workbook = new XLWorkbook();
        workbook.Style.Font.FontName = "Arial";
        workbook.Style.Font.FontSize = 10;
        var summary = workbook.Worksheets.Add("Dönem özeti");
        summary.Style.Font.FontSize = 9;
        summary.Columns(1, 8).Width = 13;
        ExcelHeader(summary, 8, ReportCode, "FATURA ÖN KONTROL VE DENETİM RAPORU", period, date);
        var row = 8;
        foreach (var section in CreateSections(reportData))
        {
            WriteMerged(summary, row, 1, 8, TextCell(section.Title), SectionHeaderColor, true, true);
            summary.Row(row++).Height = 17;
            if (section.Headers != null)
                WriteExcelRow(summary, row++, section.Spans, section.Headers.Select(TextCell).ToArray(), SectionHeaderColor, true);
            if (section.Rows.Count == 0)
                WriteMerged(summary, row++, 1, 8, TextCell("Bu dönemde listelenecek kayıt bulunmuyor."));
            for (var i = 0; i < section.Rows.Count; i++)
                WriteExcelRow(summary, row++, section.Spans, section.Rows[i], i % 2 == 1 ? AlternateRowColor : "FFFFFF", section.Headers == null);
            summary.Row(row++).Height = 5;
        }

        WriteMerged(summary, row, 1, 8, TextCell("Kural dağılımında aynı kalemin farklı bulguları tekrar tutar içerebilir. Riskteki tutar hesabında kalemler tekilleştirilir."));
        summary.Row(row++).Height = 22;
        WriteMerged(summary, row, 1, 8, TextCell(Notice));
        summary.Row(row).Height = 35;
        PrintSetup(summary, row, 8, false);
        if (row <= 55)
            summary.PageSetup.FitToPages(1, 1);
        summary.SheetView.FreezeRows(6);
        summary.SetTabActive();
        var details = workbook.Worksheets.Add("Bulgular");
        ExcelHeader(details, 10, "MOK.LS.01", "AYRINTILI BULGU LİSTESİ", period, date);
        string[] headers = ["Fatura ID", "Kalem ID", "Kural", "Versiyon", "Önem", "Bulgu", "Gerekçe", "Önerilen aksiyon", "Etkilenen tutar (TL)", "Durum"];
        for (var columnIndex = 1; columnIndex <= headers.Length; columnIndex++)
            details.Cell(8, columnIndex).Value = headers[columnIndex - 1];
        StyleRange(details.Range(8, 1, 8, 10), SectionHeaderColor, true);
        row = 9;
        foreach (var bulgu in reportData.Bulgular)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Excel 15 haneden uzun sayıları yuvarladığı için kimlikler metin olarak yazılır.
            details.Cell(row, 1).Value = bulgu.FaturaId.ToString(CultureInfo.InvariantCulture);
            details.Cell(row, 2).Value = bulgu.FaturaKalemiId?.ToString(CultureInfo.InvariantCulture) ?? "Fatura geneli";
            details.Cell(row, 3).Value = bulgu.KuralKodu;
            details.Cell(row, 4).Value = bulgu.KuralVersiyon;
            details.Cell(row, 5).Value = bulgu.Severity.ToDisplayName();
            details.Cell(row, 6).Value = bulgu.Message;
            details.Cell(row, 7).Value = bulgu.Gerekce;
            details.Cell(row, 8).Value = bulgu.OnerilenAksiyon;
            details.Cell(row, 9).Value = bulgu.EtkilenenTutar;
            details.Cell(row, 10).Value = bulgu.Status.ToDisplayName();
            StyleRange(details.Range(row, 1, row, 10), row % 2 == 0 ? AlternateRowColor : "FFFFFF");
            var lines = new[]
            {
                bulgu.Message.Length / 35 + 1,
                bulgu.Gerekce.Length / 55 + 1,
                bulgu.OnerilenAksiyon.Length / 50 + 1
            }.Max();
            details.Row(row).Height = Math.Min(409, Math.Max(30, lines * 14 + 8));
            row++;
        }

        if (reportData.Bulgular.Count > 0)
        {
            var table = details.Range(8, 1, row - 1, 10).CreateTable("DenetimBulgulari");
            table.Theme = XLTableTheme.None;
            table.ShowRowStripes = false;
        }
        else
            WriteMerged(details, row++, 1, 10, TextCell("Bu dönemde listelenecek bulgu bulunmuyor."));
        details.Columns(1, 2).Width = 22;
        details.Column(3).Width = 12;
        details.Column(4).Width = 10;
        details.Column(5).Width = 17;
        details.Column(6).Width = 37;
        details.Column(7).Width = 60;
        details.Column(8).Width = 55;
        details.Column(9).Width = 22;
        details.Column(9).Style.NumberFormat.Format = "#,##0.00";
        details.Column(10).Width = 24;
        details.SheetView.FreezeRows(8);
        details.SheetView.FreezeColumns(2);
        PrintSetup(details, row - 1, 10, true);
        details.PageSetup.SetRowsToRepeatAtTop(1, 8);
        using var stream = new MemoryStream();
        cancellationToken.ThrowIfCancellationRequested();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void ExcelHeader(IXLWorksheet sheet, int columns, string code, string title, string period, DateTime date)
    {
        sheet.ShowGridLines = false;
        var split = columns - 2;
        WriteMerged(sheet, 1, 1, 1, TextCell("M+"), "FFFFFF", true, true);
        sheet.Range(1, 1, 4, 1).Merge();
        sheet.Cell(1, 1).Style.Font.FontSize = 24;
        sheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#AC5555");
        StyleRange(sheet.Range(1, 1, 4, 1));
        WriteMerged(sheet, 1, 2, split, TextCell("MEDULA ÖN KONTROL"), "FFFFFF", true, true);
        WriteMerged(sheet, 2, 2, split, TextCell("FATURA DENETİM BİRİMİ"), "FFFFFF", true, true);
        WriteMerged(sheet, 3, 2, split, TextCell("SENTETİK UYGULAMA ORTAMI"), "FFFFFF", false, true);
        WriteMerged(sheet, 4, 2, split, TextCell(title), "FFFFFF", true, true);
        string[] labels = ["Doküman Kodu", "Düzenleme Tarihi", "Şablon Revizyonu", "Sayfa No"];
        ReportCell[] values = [TextCell(code), new(date.Date, "dd.mm.yyyy"), TextCell("01"), TextCell("Baskı alt bilgisinde")];
        for (var i = 0; i < labels.Length; i++)
        {
            WriteMerged(sheet, i + 1, split + 1, split + 1, TextCell(labels[i]), "FFFFFF", true, true);
            WriteMerged(sheet, i + 1, columns, columns, values[i], "FFFFFF", false, true);
            sheet.Row(i + 1).Height = columns == 8 ? 22 : 27;
        }

        sheet.Row(5).Height = 7;
        WriteMerged(sheet, 6, 1, columns, TextCell("Raporlama Dönemi: " + period), "FFFFFF", true, true);
        sheet.Row(6).Height = 22;
        sheet.Row(7).Height = 7;
    }

    private static void WriteExcelRow(IXLWorksheet sheet, int row, int[] spans, ReportCell[] values, string color, bool bold)
    {
        var column = 1;
        for (var i = 0; i < spans.Length; i++)
        {
            WriteMerged(sheet, row, column, column + spans[i] - 1, values[i], color, bold && !values[i].Numeric);
            column += spans[i];
        }

        sheet.Row(row).Height = spans.Length == 3 && spans[^1] == 4 ? 26 : bold ? 23 : 16;
    }

    private static void WriteMerged(IXLWorksheet sheet, int row, int first, int last, ReportCell value, string color = "FFFFFF", bool bold = false, bool center = false)
    {
        var range = sheet.Range(row, first, row, last);
        if (first != last)
            range.Merge();
        var cell = sheet.Cell(row, first);
        cell.Value = XLCellValue.FromObject(value.Value, _turkishCulture);
        if (value.Format != null)
            cell.Style.NumberFormat.Format = value.Format;
        StyleRange(range, color, bold);
        range.Style.Alignment.Horizontal = center ? XLAlignmentHorizontalValues.Center : value.Numeric ? XLAlignmentHorizontalValues.Right : XLAlignmentHorizontalValues.Left;
    }

    private static void StyleRange(IXLRange range, string color = "FFFFFF", bool bold = false)
    {
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#" + color);
        range.Style.Font.Bold = bold;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.OutsideBorderColor = XLColor.Black;
        range.Style.Border.InsideBorderColor = XLColor.Black;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        range.Style.Alignment.WrapText = true;
    }

    private static void PrintSetup(IXLWorksheet sheet, int lastRow, int columns, bool landscape)
    {
        sheet.PageSetup.PaperSize = landscape ? XLPaperSize.A3Paper : XLPaperSize.A4Paper;
        sheet.PageSetup.PageOrientation = landscape ? XLPageOrientation.Landscape : XLPageOrientation.Portrait;
        sheet.PageSetup.FitToPages(1, 0);
        sheet.PageSetup.CenterHorizontally = true;
        sheet.PageSetup.Margins.Top = 0.3;
        sheet.PageSetup.Margins.Bottom = 0.4;
        sheet.PageSetup.Margins.Left = 0.3;
        sheet.PageSetup.Margins.Right = 0.3;
        sheet.PageSetup.PrintAreas.Add(1, 1, lastRow, columns);
        sheet.PageSetup.Footer.Right.AddText("Sayfa &P / &N");
        sheet.PageSetup.Footer.Left.AddText("MEDULA - Sentetik karar destek raporu");
        sheet.PageSetup.SetRowsToRepeatAtTop(1, 6);
    }
}
