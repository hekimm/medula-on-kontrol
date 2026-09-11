using System.Diagnostics;
using System.Text.Json;
using Dapper;
using MedulaOnKontrol.Application;
using MedulaOnKontrol.Domain;
using MedulaOnKontrol.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

var root = Directory.GetCurrentDirectory(); var secret = Path.Combine(root, "secrets");
var connectionString = Environment.GetEnvironmentVariable("MEDULA_TEST_CONNECTION") ?? new OracleConnectionStringBuilder { UserID = "MEDULA", Password = File.ReadAllText(Path.Combine(secret, "app-password")).Trim(), DataSource = "localhost:11521/XEPDB1" }.ConnectionString;
DefaultTypeMap.MatchNamesWithUnderscores = true; OracleConfiguration.BindByName = true;
var options = Options.Create(new DatabaseOptions { ConnectionString = connectionString, OwnerConnectionString = connectionString, SecretDirectory = secret });
var factory = new OracleConnectionFactory(options); await using var connection = await factory.OpenAsync(default);
var month = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-3); var donem = month.Year * 100 + month.Month;
if (args.Contains("--seed"))
{
    if (await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM DONEM WHERE DONEM_KODU=:Donem AND KURUM_ID=1", new { Donem = donem }) == 0)
    {
        using var transaction = connection.BeginTransaction(); var seed = new SentetikVeriSeeder(new SifrelemeService(Convert.FromBase64String(File.ReadAllText(Path.Combine(secret, "aes-key")))), options, NullLogger<SentetikVeriSeeder>.Instance); await seed.SeedPeriodAsync(connection, transaction, month, 10000, 1, default); transaction.Commit();
    }
    Console.WriteLine($"Performans verisi hazır: {donem}, 50.000 kalem.");
}
var repository = new MedulaRepository(factory, Options.Create(new SkorlamaOptions()));
var engine = new KuralEngine([new ProvizyonKurali(), new TaniKurali(), new IslemTaniKurali(), new IslemKurali(), new TutarKurali(), new BelgeKurali(), new IlacKurali(), new SureKurali()], new RiskSkorlamaService(Options.Create(new SkorlamaOptions())));
var scope = new ErisimKapsami(7, 1, "performans-testi");
var watch = Stopwatch.StartNew();
var job = await connection.ExecuteScalarAsync<long>("SELECT NVL(MAX(ID),0)+1 FROM DENETIM_ISI");
// İş aynı transaction içinde rezerve edilir; arka plan işçisi devralamaz.
using (var transaction = connection.BeginTransaction())
{
    job = await OracleCommands.InsertIdAsync(connection, "INSERT INTO DENETIM_ISI(DONEM,KURUM_ID,KULLANICI_ID,DURUM,BASLANGIC,KIRA_BITIS) VALUES(:Donem,1,7,'CALISIYOR',SYS_EXTRACT_UTC(SYSTIMESTAMP),SYS_EXTRACT_UTC(SYSTIMESTAMP)+NUMTODSINTERVAL(10,'MINUTE')) RETURNING ID INTO :NewId", new { Donem = donem }, default, transaction); transaction.Commit();
}
var result = await new DonemDenetimService(repository, repository, repository, engine).RunAsync(new() { Id = job, Donem = donem, KurumId = 1, UserId = 7 }, default); watch.Stop();
var output = new { KayitTarihi = DateTime.UtcNow, Donem = donem, FaturaSayisi = result.FaturaSayisi, LineCount = result.LineCount, RuleCount = 40, BulguSayisi = result.BulguSayisi, FailedCount = result.FailedCount, StageDurationsSeconds = result.StageDurationsSeconds, WorkerCount = 8, ElapsedSeconds = watch.Elapsed.TotalSeconds, TargetMet = result.LineCount >= 50000 && result.FailedCount == 0 && watch.Elapsed.TotalSeconds < 60, OperatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription, ProcessorCount = Environment.ProcessorCount, Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription, Scope = "Oracle PL/SQL aday seçimi + tüm bağlamların Oracle'dan okunması + C# kuralları + bulguların ve denetim izinin Oracle'a transaction ile yazılması; seed hariç" };
Directory.CreateDirectory(Path.Combine(root, "artifacts", "verification")); var json = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }); await File.WriteAllTextAsync(Path.Combine(root, "artifacts", "verification", "performance.json"), json); Console.WriteLine(json); Environment.ExitCode = output.TargetMet ? 0 : 1;
