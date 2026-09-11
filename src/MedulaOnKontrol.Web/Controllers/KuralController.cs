using MedulaOnKontrol.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedulaOnKontrol.Web.Controllers;
[Authorize(Policy = Yetkiler.Kural)]
public sealed class KuralController(IKuralRepository repository, IFaturaRepository faturalar, KuralEngine engine, KuralKalibrasyonService kalibrasyon) : Controller
{
    public async Task<IActionResult> Calibration(int? donem, CancellationToken cancellationToken)
    {
        var seciliDonem = donem ?? DateTime.UtcNow.Year * 100 + DateTime.UtcNow.Month;
        if (!KurumTakvimi.IsValidPeriod(seciliDonem)) return BadRequest();
        ViewBag.Donem = seciliDonem;
        return View(await kalibrasyon.ListAsync(KullaniciClaims.Scope(HttpContext), seciliDonem, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Calibrate(KalibrasyonRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest();
        var result = await kalibrasyon.ApplyAsync(KullaniciClaims.Scope(HttpContext), request, cancellationToken);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? "Kalibrasyon yeni kural sürümü olarak kaydedildi." : result.Error;
        return RedirectToAction(nameof(Calibration), new { donem = request.Donem });
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(await repository.ListAsync(cancellationToken));
    public async Task<IActionResult> Details(string id, CancellationToken cancellationToken)
    {
        var rows = (await repository.ListAsync(cancellationToken)).Where(kural => kural.KuralKodu == id).ToList();
        if (rows.Count == 0)
            return NotFound();
        return View(rows);
    }

    [HttpPost]
    public async Task<IActionResult> Version(KuralVersionRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest();
        var result = await repository.VersionAsync(KullaniciClaims.Scope(HttpContext), request, cancellationToken);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Yeni kural versiyonu kaydedildi." : result.Error;
        return RedirectToAction(nameof(Details), new { id = request.KuralKodu });
    }

    [HttpPost]
    public async Task<IActionResult> Simulation(KuralVersionRequest request, int donem, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || !KuralVersionRequestValidator.IsValidJson(request.ParametersJson) || request.Agirlik is < 1 or > 100 || donem % 100 is < 1 or > 12 || donem is < 202001 or > 210012 || !Enum.IsDefined(request.Severity))
            return BadRequest();
        var kural = (await repository.ListAsync(cancellationToken)).FirstOrDefault(seciliKural => seciliKural.KuralKodu == request.KuralKodu);
        if (kural == null)
            return NotFound();
        kural.ParametersJson = request.ParametersJson;
        kural.Agirlik = request.Agirlik;
        kural.Severity = request.Severity;
        kural.IsActive = true;
        kural.YururlukBaslangic = new(donem / 100, donem % 100, 1);
        kural.YururlukBitis = null;
        var contexts = await faturalar.LoadPeriodAsync(KullaniciClaims.Scope(HttpContext), donem, cancellationToken);
        var evaluations = new List<Result<KontrolSonucu>>();
        foreach (var context in contexts)
            evaluations.Add(await engine.EvaluateAsync(context, [kural], cancellationToken));
        if (evaluations.Any(result => !result.IsSuccess))
        {
            TempData["Error"] = evaluations.First(result => !result.IsSuccess).Error;
            return RedirectToAction(nameof(Details), new { id = request.KuralKodu });
        }
        var results = evaluations.Select(result => result.Value!).ToList();
        ViewBag.Kural = kural.KuralKodu;
        ViewBag.Donem = donem;
        ViewBag.FaturaSayisi = results.Count(validationResult => validationResult.Bulgular.Count > 0);
        ViewBag.BulguSayisi = results.Sum(validationResult => validationResult.Bulgular.Count);
        ViewBag.Risk = results.Sum(validationResult => validationResult.Risk.RisktekiTutar);
        return View();
    }
}
