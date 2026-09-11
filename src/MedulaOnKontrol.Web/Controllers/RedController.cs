using MedulaOnKontrol.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedulaOnKontrol.Web.Controllers;
[Authorize(Policy = Yetkiler.Rapor)]
public sealed class RedController(IRedRepository repository) : Controller
{
    public async Task<IActionResult> Index(int? donem, CancellationToken cancellationToken)
    {
        var count = donem ?? DateTime.UtcNow.Year * 100 + DateTime.UtcNow.Month;
        if (!KurumTakvimi.IsValidPeriod(count)) return BadRequest("Geçerli bir fatura dönemi girin.");
        ViewBag.Donem = count;
        return View(await repository.AnalyzeAsync(KullaniciClaims.Scope(HttpContext), count, cancellationToken));
    }

    [HttpPost, Authorize(Policy = Yetkiler.Approve)]
    public async Task<IActionResult> Create(RedRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest();
        request.KayitTarihi = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(request.KayitTarihi, DateTimeKind.Unspecified), TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"));
        var result = await repository.SaveAsync(KullaniciClaims.Scope(HttpContext), request, cancellationToken);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Red geri beslemesi kaydedildi." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Policy = Yetkiler.Approve)]
    public async Task<IActionResult> Classify(RedSiniflandirmaRequest request, int donem, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || !KurumTakvimi.IsValidPeriod(donem)) return BadRequest();
        var result = await repository.ClassifyAsync(KullaniciClaims.Scope(HttpContext), request, cancellationToken);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Red sınıflandırması ve isabet analizi güncellendi." : result.Error;
        return RedirectToAction(nameof(Index), new { donem });
    }
}
