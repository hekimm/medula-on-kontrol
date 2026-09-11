using MedulaOnKontrol.Application;
using MedulaOnKontrol.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MedulaOnKontrol.Web.Controllers;
public sealed class HesapController(IKullaniciRepository users, IDenetimRepository audit) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [AllowAnonymous, HttpPost, EnableRateLimiting("login")]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password) || password.Length > 128)
        {
            ModelState.AddModelError("", "Kullanıcı adı ve parola gereklidir.");
            return View();
        }

        var user = await users.FindAsync(username.Trim(), cancellationToken);
        var valid = user is { IsActive: true } && (user.LockedUntil == null || user.LockedUntil <= DateTime.UtcNow) && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        if (user != null)
            await users.RecordLoginResultAsync(user.Id, valid, cancellationToken);
        await audit.AppendAsync(new() { UserId = user?.Id ?? 0, KurumId = user?.KurumId ?? 0, OperationType = DenetimIslem.View, EntityName = "OTURUM_GIRIS", EntityId = user?.Id.ToString() ?? "BILINMEYEN", NewValueJson = valid ? "{\"IsSuccessful\":true}" : "{\"IsSuccessful\":false}", IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "local" }, cancellationToken);
        if (!valid)
        {
            ModelState.AddModelError("", "Kullanıcı adı veya parola geçersiz; hesap geçici olarak kilitli olabilir.");
            return View();
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, KullaniciClaims.Create(user!), new AuthenticationProperties { IsPersistent = false });
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }

    [HttpPost]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var accessScope = KullaniciClaims.Scope(HttpContext);
        await audit.AppendAsync(new() { UserId = accessScope.UserId, KurumId = accessScope.KurumId, OperationType = DenetimIslem.Update, EntityName = "OTURUM_CIKIS", EntityId = accessScope.UserId.ToString(), IpAddress = accessScope.IpAddress }, cancellationToken);
        await HttpContext.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = 403;
        ViewBag.Message = "Bu işlem için yetkiniz bulunmuyor.";
        return View("Error");
    }

    [AllowAnonymous]
    public IActionResult Error()
    {
        Response.StatusCode = 500;
        ViewBag.Message = "İşlem tamamlanamadı. Lütfen tekrar deneyin. Destek kaydı: " + HttpContext.TraceIdentifier;
        return View();
    }
}
