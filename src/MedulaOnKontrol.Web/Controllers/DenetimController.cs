using MedulaOnKontrol.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedulaOnKontrol.Web.Controllers;
[Authorize(Policy = Yetkiler.Denetim)]
public sealed class DenetimController(IDenetimRepository repository) : Controller
{
    public async Task<IActionResult> Index(DenetimFilter filter, CancellationToken cancellationToken)
    {
        ViewBag.Filter = filter;
        return View(await repository.ListAsync(KullaniciClaims.Scope(HttpContext), filter, cancellationToken));
    }
}
