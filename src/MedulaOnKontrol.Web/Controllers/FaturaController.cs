using MedulaOnKontrol.Application;
using MedulaOnKontrol.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedulaOnKontrol.Web.Controllers;
public sealed class FaturaController(IFaturaRepository repository, FaturaKontrolService service, IKullaniciRepository users, IEImzaDogrulayici signatureVerifier, IGonderimRepository submissionRepository, IMedulaAdapter medula, ISifrelemeService encryptionService, IDenetimRepository audit) : Controller
{
    private ErisimKapsami Scope => KullaniciClaims.Scope(HttpContext);

    public async Task<IActionResult> Index(FaturaFilter filter, CancellationToken cancellationToken)
    {
        ViewBag.Filter = filter;
        ViewBag.Klinikler = await repository.ListKliniklerAsync(Scope, cancellationToken);
        return View(await repository.ListAsync(Scope, filter, cancellationToken));
    }

    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        var context = await repository.GetAsync(Scope, id, cancellationToken);
        if (context == null)
            return NotFound();
        ViewBag.Bulgular = await repository.ListBulgularAsync(Scope, id, cancellationToken);
        return View(context);
    }

    [HttpPost, Authorize(Policy = Yetkiler.Validate)]
    public async Task<IActionResult> Validate(long id, CancellationToken cancellationToken)
    {
        var result = await service.ValidateAsync(Scope, id, cancellationToken);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? $"Kontrol tamamlandı: {result.Value!.Bulgular.Count} bulgu." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Policy = Yetkiler.Correct)]
    public async Task<IActionResult> Correct(DuzeltmeRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Form alanlarını kontrol edin.";
            return RedirectToAction(nameof(Details), new { id = request.FaturaId });
        }

        var result = await repository.CorrectAsync(Scope, request, cancellationToken);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Düzeltme kaydedildi. Faturayı yeniden kontrol edin." : result.Error;
        return RedirectToAction(nameof(Details), new { id = request.FaturaId });
    }

    [HttpPost, Authorize(Policy = Yetkiler.Approve)]
    public async Task<IActionResult> AddException(IstisnaRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest();
        var result = await repository.AddExceptionAsync(Scope, request, cancellationToken);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Gerekçeli istisna kaydedildi. Yeniden kontrol edin." : result.Error;
        return RedirectToAction(nameof(Details), new { id = request.FaturaId });
    }

    [HttpPost, Authorize(Policy = Yetkiler.Approve)]
    public async Task<IActionResult> Approve(long id, string password, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(Scope.UserId, cancellationToken);
        if (user == null || string.IsNullOrEmpty(password) || !await signatureVerifier.VerifyAsync(user, password, cancellationToken))
            TempData["Error"] = "Simüle e-imza doğrulanamadı.";
        else
        {
            var result = await repository.ApproveAsync(Scope, id, cancellationToken);
            TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Fatura simüle e-imza ile onaylandı." : result.Error;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Policy = Yetkiler.Approve)]
    public async Task<IActionResult> Submit(long id, CancellationToken cancellationToken)
    {
        var context = await repository.GetAsync(Scope, id, cancellationToken);
        if (context == null)
            return NotFound();
        var selectedResult = await submissionRepository.PrepareAsync(Scope, id, cancellationToken);
        if (!selectedResult.IsSuccess)
            TempData["Error"] = selectedResult.Error;
        else
        {
            var result = await medula.SendAsync(context.Fatura, selectedResult.Value!, cancellationToken);
            var completion = await submissionRepository.CompleteAsync(Scope, id, selectedResult.Value!, result, cancellationToken);
            TempData[completion.IsSuccess ? "Success" : "Error"] = completion.IsSuccess
                ? result.IsAccepted ? "Simülatör faturayı kabul etti." : "Simülatör red kaydı oluşturdu."
                : completion.Error;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Policy = Yetkiler.RevealIdentity)]
    public async Task<IActionResult> RevealIdentity(long id, CancellationToken cancellationToken)
    {
        var context = await repository.GetAsync(Scope, id, cancellationToken);
        if (context == null)
            return NotFound();
        await audit.AppendAsync(new() { UserId = Scope.UserId, KurumId = Scope.KurumId, KlinikKodu = context.Basvuru.KlinikKodu, OperationType = DenetimIslem.View, EntityName = "HASTA_MASKE_KALDIRMA", EntityId = context.Hasta.Id.ToString(), IpAddress = Scope.IpAddress }, cancellationToken);
        Response.Headers.CacheControl = "no-store";
        ViewBag.Name = (await encryptionService.DecryptAsync(context.Hasta.AdSoyad, cancellationToken));
        ViewBag.RevealIdentity = (await encryptionService.DecryptAsync(context.Hasta.KimlikNo, cancellationToken));
        ViewBag.Bulgular = await repository.ListBulgularAsync(Scope, id, cancellationToken);
        return View("Details", context);
    }
}
