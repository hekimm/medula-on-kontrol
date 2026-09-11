using MedulaOnKontrol.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedulaOnKontrol.Web.Controllers;
public sealed class PanelController(IFaturaRepository repository, IDenetimIsiRepository jobs) : Controller
{
    public async Task<IActionResult> Index(int? donem, CancellationToken cancellationToken)
    {
        var count = donem ?? DateTime.UtcNow.Year * 100 + DateTime.UtcNow.Month;
        ViewBag.Donem = count;
        ViewBag.Jobs = await jobs.ListAsync(KullaniciClaims.Scope(HttpContext), cancellationToken);
        return View(await repository.GetSummaryAsync(KullaniciClaims.Scope(HttpContext), count, cancellationToken));
    }

    [HttpPost, Authorize(Policy = Yetkiler.Approve)]
    public async Task<IActionResult> ValidatePeriod(int donem, CancellationToken cancellationToken)
    {
        if (donem % 100 is < 1 or > 12 || donem is < 202001 or > 210012)
            return BadRequest();
        var id = await jobs.EnqueueAsync(KullaniciClaims.Scope(HttpContext), donem, cancellationToken);
        TempData["Success"] = $"Dönem kontrolü kuyruğa alındı. İş #{id}";
        return RedirectToAction(nameof(Index), new { donem });
    }

    public async Task<IActionResult> Jobs(CancellationToken cancellationToken) => Json(await jobs.ListAsync(KullaniciClaims.Scope(HttpContext), cancellationToken));
}
