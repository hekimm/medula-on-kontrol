using System.Security.Claims;
using MedulaOnKontrol.Application;
using MedulaOnKontrol.Domain;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MedulaOnKontrol.Web.Security;
public static class Yetkiler
{
    public const string Correct = "Correct", Validate = "Validate", Approve = "Approve", Kural = "Kural", Rapor = "Rapor", Denetim = "Denetim", RevealIdentity = "RevealIdentity";
}
