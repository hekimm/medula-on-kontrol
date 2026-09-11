using System.Security.Claims;
using MedulaOnKontrol.Application;
using MedulaOnKontrol.Domain;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MedulaOnKontrol.Web.Security;
public static class KullaniciClaims
{
    public static ClaimsPrincipal Create(Kullanici user) => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.Username), new Claim("institution_id", user.KurumId.ToString()) }.Concat(user.Roller.Select(textValue => new Claim(ClaimTypes.Role, textValue))), CookieAuthenticationDefaults.AuthenticationScheme));
    public static ErisimKapsami Scope(HttpContext httpContext) => new(long.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!), long.Parse(httpContext.User.FindFirstValue("institution_id")!), httpContext.Connection.RemoteIpAddress?.ToString() ?? "local");
}
