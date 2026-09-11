using Dapper;
using FluentAssertions;
using MedulaOnKontrol.Application;
using MedulaOnKontrol.Domain;
using MedulaOnKontrol.Infrastructure;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using Xunit;
using static MedulaOnKontrol.Infrastructure.Persistence.OracleCommands;

namespace MedulaOnKontrol.IntegrationTests;
public sealed class OracleFixture
{
    public IOracleConnectionFactory Factory { get; }
    public MedulaRepository Repository { get; }
    public KuralEngine Engine { get; }
    public FaturaKontrolService Service { get; }
    public ISifrelemeService Cipher { get; }
    public ErisimKapsami Admin { get; } = new(7, 1, "entegrasyon-testi");

    public OracleFixture()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        OracleConfiguration.BindByName = true;
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "MedulaOnKontrol.sln")) && Directory.GetParent(root) != null)
            root = Directory.GetParent(root)!.FullName;
        var connectionEnvironmentVariable = Environment.GetEnvironmentVariable("MEDULA_TEST_CONNECTION");
        var connection = connectionEnvironmentVariable ?? new OracleConnectionStringBuilder
        {
            UserID = "MEDULA",
            Password = File.ReadAllText(Path.Combine(root, "secrets/app-password")).Trim(),
            DataSource = "localhost:11521/XEPDB1"
        }.ConnectionString;
        Cipher = new SifrelemeService(Convert.FromBase64String(File.ReadAllText(Path.Combine(root, "secrets/aes-key")).Trim()));
        Factory = new OracleConnectionFactory(Options.Create(new DatabaseOptions { ConnectionString = connection }));
        var score = Options.Create(new SkorlamaOptions { NormalizeWeights = true });
        Repository = new(Factory, score);
        Engine = new([new ProvizyonKurali(), new TaniKurali(), new IslemTaniKurali(), new IslemKurali(), new TutarKurali(), new BelgeKurali(), new IlacKurali(), new SureKurali()], new RiskSkorlamaService(score));
        Service = new(Repository, Repository, Engine);
    }

    public async Task<long> InvoiceAsync(string klinik = "DAHILIYE", bool provizyon = true)
    {
        var id = Random.Shared.NextInt64(2000000000000000, 3000000000000000);
        var date = DateTime.UtcNow.Date;
        var donem = date.Year * 100 + date.Month;
        await using var connection = await Factory.OpenAsync(default);
        using var transaction = connection.BeginTransaction();
        var parameters = new
        {
            Id = id,
            Name = (await Cipher.EncryptAsync("SENTETİK ENTEGRASYON", default)),
            RevealIdentity = (await Cipher.EncryptAsync("00000000000", default)),
            No = "IT-" + id,
            KayitTarihi = date,
            Donem = donem,
            Klinik = klinik,
            Provizyon = provizyon ? "TEST-P" : null
        };
        await connection.ExecuteAsync(CreateCommand("INSERT INTO HASTA(ID,HASTA_NO,AD_SOYAD,KIMLIK_NO,DOGUM_TARIHI,CINSIYET) VALUES(:Id,:No,:Name,:RevealIdentity,DATE '1980-01-01',1)", parameters, default, transaction));
        await connection.ExecuteAsync(CreateCommand("INSERT INTO BASVURU(ID,HASTA_ID,KURUM_ID,TAKIP_NO,PROVIZYON_NO,PROVIZYON_TARIHI,BASVURU_TIPI,KLINIK_KODU) VALUES(:Id,:Id,1,:No,:Provizyon,:KayitTarihi,0,:Klinik)", parameters, default, transaction));
        await connection.ExecuteAsync(CreateCommand("INSERT INTO FATURA(ID,DONEM,BASVURU_ID,FATURA_NO,TOPLAM_TUTAR) VALUES(:Id,:Donem,:Id,:No,225)", parameters, default, transaction));
        await connection.ExecuteAsync(CreateCommand("INSERT INTO FATURA_KALEMI(ID,FATURA_ID,DONEM,SUT_ISLEM_KODU,ISLEM_TARIHI,ADET,BIRIM_FIYAT,TUTAR,KLINIK_KODU) VALUES(:Id*10,:Id,:Donem,'TST001',:KayitTarihi,1,225,225,:Klinik)", parameters, default, transaction));
        await connection.ExecuteAsync(CreateCommand("INSERT INTO BASVURU_TANI(BASVURU_ID,ICD10_KOD,TANI_TIPI,TARIH) VALUES(:Id,'J06.9',0,:KayitTarihi)", parameters, default, transaction));
        transaction.Commit();
        return id;
    }
}
