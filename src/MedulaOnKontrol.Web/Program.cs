using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.RateLimiting;
using Dapper;
using MedulaOnKontrol.Application;
using MedulaOnKontrol.Domain;
using MedulaOnKontrol.Infrastructure;
using MedulaOnKontrol.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using Serilog;

if (args.Length > 0 && args[0] == "--init-secrets")
{
    var directory = args.Length > 1 ? args[1] : "/run/medula-secrets"; Directory.CreateDirectory(directory);
    foreach (var name in new[] { "oracle-password", "app-password", "runtime-password", "admin-password", "aes-key" })
    {
        var path = Path.Combine(directory, name); if (!File.Exists(path)) await File.WriteAllTextAsync(path, name == "aes-key" ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) : "M9a" + Convert.ToHexString(RandomNumberGenerator.GetBytes(12)));
    }
    return;
}
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, configuration) => configuration.MinimumLevel.Information().MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning).Enrich.FromLogContext().WriteTo.Console().WriteTo.File("logs/medula-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14));
var secretDirectory = Environment.GetEnvironmentVariable("MEDULA_SECRET_DIR") ?? Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "../../secrets"));
if (!Directory.Exists(secretDirectory)) throw new InvalidOperationException("Gizli ayar dizini bulunamadı. scripts/start-oracle.ps1 veya docker compose up ile hazırlayın.");
var ownerPassword = File.ReadAllText(Path.Combine(secretDirectory, "app-password")).Trim();
var source = builder.Configuration["Database:DataSource"] ?? "localhost:11521/XEPDB1";
var ownerConnection = builder.Configuration.GetConnectionString("OracleOwner") ?? new OracleConnectionStringBuilder { UserID = "MEDULA", Password = ownerPassword, DataSource = source, Pooling = true, MinPoolSize = 1, MaxPoolSize = 40 }.ConnectionString;
var connection = builder.Configuration.GetConnectionString("Oracle") ?? (builder.Configuration.GetValue("Database:UseRuntimeUser", false) ? new OracleConnectionStringBuilder { UserID = "MEDULA_RUN", Password = File.ReadAllText(Path.Combine(secretDirectory, "runtime-password")).Trim(), DataSource = source, Pooling = true, MinPoolSize = 1, MaxPoolSize = 40 }.ConnectionString : ownerConnection);
builder.Services.Configure<DatabaseOptions>(value => { value.ConnectionString = connection; value.OwnerConnectionString = ownerConnection; value.SecretDirectory = secretDirectory; value.Seed = builder.Configuration.GetValue("Database:Seed", true); });
builder.Services.Configure<SkorlamaOptions>(builder.Configuration.GetSection("Scoring"));
builder.Services.Configure<SaklamaOptions>(builder.Configuration.GetSection("Retention"));
builder.Services.AddSingleton(new AesGcmCodec(Convert.FromBase64String(File.ReadAllText(Path.Combine(secretDirectory, "aes-key")).Trim())));
builder.Services.AddSingleton<ISifrelemeService>(services => new SifrelemeService(services.GetRequiredService<AesGcmCodec>()));
builder.Services.AddSingleton<IOracleConnectionFactory, OracleConnectionFactory>();
builder.Services.AddScoped<MedulaRepository>();
builder.Services.AddScoped<IFaturaRepository>(value => value.GetRequiredService<MedulaRepository>()); builder.Services.AddScoped<IKuralRepository>(value => value.GetRequiredService<MedulaRepository>()); builder.Services.AddScoped<IKullaniciRepository>(value => value.GetRequiredService<MedulaRepository>()); builder.Services.AddScoped<IDenetimRepository>(value => value.GetRequiredService<MedulaRepository>()); builder.Services.AddScoped<IRedRepository>(value => value.GetRequiredService<MedulaRepository>()); builder.Services.AddScoped<IDenetimIsiRepository>(value => value.GetRequiredService<MedulaRepository>()); builder.Services.AddScoped<IGonderimRepository>(value => value.GetRequiredService<MedulaRepository>());
builder.Services.AddSingleton<RiskSkorlamaService>(); builder.Services.AddSingleton<KuralEngine>();
builder.Services.AddScoped<KuralKalibrasyonService>();
builder.Services.AddSingleton<IFaturaKurali, ProvizyonKurali>(); builder.Services.AddSingleton<IFaturaKurali, TaniKurali>(); builder.Services.AddSingleton<IFaturaKurali, IslemTaniKurali>(); builder.Services.AddSingleton<IFaturaKurali, IslemKurali>(); builder.Services.AddSingleton<IFaturaKurali, TutarKurali>(); builder.Services.AddSingleton<IFaturaKurali, BelgeKurali>(); builder.Services.AddSingleton<IFaturaKurali, IlacKurali>(); builder.Services.AddSingleton<IFaturaKurali, SureKurali>();
builder.Services.AddScoped<FaturaKontrolService>(); builder.Services.AddScoped<DonemDenetimService>(); builder.Services.AddSingleton<IMedulaAdapter, MedulaSimulator>(); builder.Services.AddSingleton<IEImzaDogrulayici, EImzaSimulator>(); builder.Services.AddSingleton<IRaporService, RaporService>(); builder.Services.AddScoped<SentetikVeriSeeder>(); builder.Services.AddScoped<DatabaseBootstrap>();
builder.Services.AddHostedService<DenetimWorker>(); builder.Services.AddHostedService<AnonimlestirmeWorker>();
builder.Services.AddOptions<Microsoft.AspNetCore.DataProtection.KeyManagement.KeyManagementOptions>().Configure<AesGcmCodec>((keyManagementOptions, encryptionCodec) => keyManagementOptions.XmlEncryptor = new DataProtectionXmlEncryptor(encryptionCodec));
builder.Services.AddDataProtection().SetApplicationName("MedulaOnKontrol").PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(secretDirectory, "session-keys")));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(cookieAuthenticationOptions =>
{
    cookieAuthenticationOptions.LoginPath = "/Hesap/Login"; cookieAuthenticationOptions.AccessDeniedPath = "/Hesap/AccessDenied"; cookieAuthenticationOptions.ExpireTimeSpan = TimeSpan.FromMinutes(30); cookieAuthenticationOptions.SlidingExpiration = true; cookieAuthenticationOptions.Cookie.Name = "MedulaOnKontrol.Session"; cookieAuthenticationOptions.Cookie.HttpOnly = true; cookieAuthenticationOptions.Cookie.SameSite = SameSiteMode.Strict; cookieAuthenticationOptions.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    cookieAuthenticationOptions.Events.OnValidatePrincipal = async context =>
    {
        var repository = context.HttpContext.RequestServices.GetRequiredService<IKullaniciRepository>(); var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = long.TryParse(id, out var uid) ? await repository.GetAsync(uid, context.HttpContext.RequestAborted) : null;
        if (user == null || !user.IsActive) { context.RejectPrincipal(); return; }
        context.ReplacePrincipal(KullaniciClaims.Create(user));
    };
});
builder.Services.AddAuthorization(value =>
{
    value.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    value.AddPolicy(Yetkiler.Correct, selectedValue => selectedValue.RequireRole(Roller.BillingOfficer, Roller.CodingSpecialist, Roller.RevenueOfficer, Roller.SystemAdministrator));
    value.AddPolicy(Yetkiler.Validate, selectedValue => selectedValue.RequireRole(Roller.BillingOfficer, Roller.CodingSpecialist, Roller.RevenueOfficer, Roller.Manager, Roller.SystemAdministrator));
    value.AddPolicy(Yetkiler.Approve, selectedValue => selectedValue.RequireRole(Roller.RevenueOfficer, Roller.Manager, Roller.SystemAdministrator));
    value.AddPolicy(Yetkiler.Kural, selectedValue => selectedValue.RequireRole(Roller.RuleAdministrator, Roller.SystemAdministrator));
    value.AddPolicy(Yetkiler.Rapor, selectedValue => selectedValue.RequireRole(Roller.RevenueOfficer, Roller.Manager, Roller.InternalAuditor, Roller.SystemAdministrator));
    value.AddPolicy(Yetkiler.Denetim, selectedValue => selectedValue.RequireRole(Roller.InternalAuditor, Roller.SystemAdministrator));
    value.AddPolicy(Yetkiler.RevealIdentity, selectedValue => selectedValue.RequireRole(Roller.CodingSpecialist, Roller.SystemAdministrator));
});
builder.Services.AddControllersWithViews(value => { value.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); value.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider()); });
builder.Services.AddAntiforgery(value => value.Cookie.SameSite = SameSiteMode.Strict);
builder.Services.AddRateLimiter(value => { value.RejectionStatusCode = 429; value.AddPolicy("login", http => RateLimitPartition.GetFixedWindowLimiter(http.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions { PermitLimit = 15, Window = TimeSpan.FromMinutes(5), QueueLimit = 0 })); });
var app = builder.Build();
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("tr-TR"); CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
DefaultTypeMap.MatchNamesWithUnderscores = true; OracleConfiguration.BindByName = true;
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
using (var scope = app.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<DatabaseBootstrap>().RunAsync(AppContext.BaseDirectory, CancellationToken.None);
app.UseExceptionHandler("/Hesap/Error"); app.UseSerilogRequestLogging();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff"; context.Response.Headers["X-Frame-Options"] = "DENY"; context.Response.Headers["Referrer-Policy"] = "same-origin";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    await next();
});
app.UseStaticFiles(); app.UseRouting(); app.UseRateLimiter(); app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true) context.Response.Headers.CacheControl = "no-store";
    if (context.User.Identity?.IsAuthenticated == true && context.Request.Method == "GET" && !context.Request.Path.StartsWithSegments("/health"))
    {
        var accessScope = KullaniciClaims.Scope(context); var audit = context.RequestServices.GetRequiredService<IDenetimRepository>();
        await audit.AppendAsync(new DenetimIzi { UserId = accessScope.UserId, KurumId = accessScope.KurumId, OperationType = DenetimIslem.View, EntityName = "EKRAN", EntityId = context.Request.Path.ToString()[..Math.Min(100, context.Request.Path.ToString().Length)], NewValueJson = null, IpAddress = accessScope.IpAddress }, context.RequestAborted);
    }
    await next();
});
app.UseAuthorization();
app.MapGet("/health", async (IOracleConnectionFactory factory, CancellationToken cancellationToken) => { try { await using var connection = await factory.OpenAsync(cancellationToken); await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT 1 FROM DUAL", cancellationToken: cancellationToken)); return Results.Ok(new { status = "healthy", database = "Oracle", medula = "simulator" }); } catch (OracleException) { return Results.Json(new { status = "not_ready" }, statusCode: 503); } }).AllowAnonymous();
app.MapControllerRoute("default", "{controller=Panel}/{action=Index}/{id?}");
await app.RunAsync();
public partial class Program { }
