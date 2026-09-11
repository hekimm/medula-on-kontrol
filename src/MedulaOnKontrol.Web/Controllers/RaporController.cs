using MedulaOnKontrol.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedulaOnKontrol.Web.Controllers;
[Authorize(Policy = Yetkiler.Rapor)]
public sealed class RaporController(IFaturaRepository repository, IRaporService reportService) : Controller
{
    public async Task<IActionResult> Index(int? donem, CancellationToken cancellationToken)
    {
        var count = donem ?? DateTime.UtcNow.Year * 100 + DateTime.UtcNow.Month;
        ViewBag.Donem = count;
        return View(await repository.GetSummaryAsync(KullaniciClaims.Scope(HttpContext), count, cancellationToken));
    }

    public async Task<IActionResult> Pdf(int donem, CancellationToken cancellationToken)
    {
        if (!IsValidPeriod(donem))
            return BadRequest("Geçerli bir fatura dönemi girin.");
        Response.Headers.CacheControl = "no-store";
        var result = await reportService.CreatePdfAsync(await repository.GetReportAsync(KullaniciClaims.Scope(HttpContext), donem, cancellationToken), donem, cancellationToken);
        return result.IsSuccess ? File(result.Value!, "application/pdf", $"medula-denetim-{donem}.pdf") : BadRequest(result.Error);
    }

    public async Task<IActionResult> Excel(int donem, CancellationToken cancellationToken)
    {
        if (!IsValidPeriod(donem))
            return BadRequest("Geçerli bir fatura dönemi girin.");
        Response.Headers.CacheControl = "no-store";
        var result = await reportService.CreateExcelAsync(await repository.GetReportAsync(KullaniciClaims.Scope(HttpContext), donem, cancellationToken), donem, cancellationToken);
        return result.IsSuccess ? File(result.Value!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"medula-bulgular-{donem}.xlsx") : BadRequest(result.Error);
    }

    private static bool IsValidPeriod(int period) => period is >= 202001 and <= 210012 && period % 100 is >= 1 and <= 12;
}
