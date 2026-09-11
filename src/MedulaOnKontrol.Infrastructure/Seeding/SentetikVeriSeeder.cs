using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using MedulaOnKontrol.Application;
using MedulaOnKontrol.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using static MedulaOnKontrol.Infrastructure.Persistence.OracleCommands;

namespace MedulaOnKontrol.Infrastructure.Seeding;
public sealed class SentetikVeriSeeder(ISifrelemeService encryptionService, IOptions<DatabaseOptions> options, ILogger<SentetikVeriSeeder> logger)
{
    public async Task RunAsync(OracleConnection connection, string contentRoot, CancellationToken cancellationToken)
    {
        if (await connection.ExecuteScalarAsync<int>(CreateCommand("SELECT COUNT(*) FROM TOHUMLAMA WHERE SURUM=:SeedVersion", new { SeedVersion = "sentetik-v1" }, cancellationToken)) > 0)
            return;
        using var transaction = connection.BeginTransaction();
        var hash = BCrypt.Net.BCrypt.HashPassword((await File.ReadAllTextAsync(Path.Combine(options.Value.SecretDirectory, "admin-password"), cancellationToken)).Trim(), 12);
        await connection.ExecuteAsync(CreateCommand("INSERT INTO KURUM(ID,AD) VALUES(1,:Name)", new { Name = "Anadolu Eğitim ve Araştırma Hastanesi · Sentetik" }, cancellationToken, transaction));
        await connection.ExecuteAsync(CreateCommand("INSERT INTO KURUM(ID,AD) VALUES(2,:Name)", new { Name = "İkinci Test Kurumu" }, cancellationToken, transaction));
        string[] klinikler = ["DAHILIYE", "KARDIYOLOJI", "CERRAHI", "COCUK", "KADIN"];
        for (var i = 0; i < klinikler.Length; i++)
            await connection.ExecuteAsync(CreateCommand("INSERT INTO KLINIK(ID,KURUM_ID,KOD,AD) VALUES(:Id,1,:Code,:Name)", new { Id = i + 1, Code = klinikler[i], Name = klinikler[i] }, cancellationToken, transaction));
        await connection.ExecuteAsync(CreateCommand("INSERT INTO KLINIK(ID,KURUM_ID,KOD,AD) VALUES(10,2,'DAHILIYE',:Name)", new { Name = "Diğer kurum dahiliye" }, cancellationToken, transaction));
        string[] names = ["fatura", "kodlama", "gelir", "kural", "yonetici", "denetci", "admin"];
        for (var i = 0; i < Roller.All.Length; i++)
        {
            await connection.ExecuteAsync(CreateCommand("INSERT INTO ROL(ID,AD) VALUES(:Id,:Name)", new { Id = i + 1, Name = Roller.All[i] }, cancellationToken, transaction));
            await connection.ExecuteAsync(CreateCommand("INSERT INTO KULLANICI(ID,KULLANICI_ADI,PAROLA_HASH,KURUM_ID,AKTIF_MI) VALUES(:Id,:Name,:Hash,1,1)", new { Id = i + 1, Name = names[i], Hash = hash }, cancellationToken, transaction));
            await connection.ExecuteAsync(CreateCommand("INSERT INTO KULLANICI_ROL(KULLANICI_ID,ROL_ID) VALUES(:Id,:Id)", new { Id = i + 1 }, cancellationToken, transaction));
            foreach (var klinik in i < 2 ? klinikler.Take(1) : klinikler)
                await connection.ExecuteAsync(CreateCommand("INSERT INTO KULLANICI_KLINIK_YETKI(KULLANICI_ID,KURUM_ID,KLINIK_KODU) VALUES(:Id,1,:Klinik)", new { Id = i + 1, Klinik = klinik }, cancellationToken, transaction));
        }

        await connection.ExecuteAsync(CreateCommand("INSERT INTO KULLANICI(ID,KULLANICI_ADI,PAROLA_HASH,KURUM_ID,AKTIF_MI) VALUES(8,'digerkurum',:Hash,2,1)", new { Hash = hash }, cancellationToken, transaction));
        await connection.ExecuteAsync(CreateCommand("INSERT INTO KULLANICI_ROL(KULLANICI_ID,ROL_ID) VALUES(8,7)", null, cancellationToken, transaction));
        await connection.ExecuteAsync(CreateCommand("INSERT INTO KULLANICI_KLINIK_YETKI(KULLANICI_ID,KURUM_ID,KLINIK_KODU) VALUES(8,2,'DAHILIYE')", null, cancellationToken, transaction));
        var jsonOptions = new JsonSerializerOptions
        {
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };
        var kurallar = JsonSerializer.Deserialize<List<Kural>>(await File.ReadAllTextAsync(Path.Combine(contentRoot, "data", "kural-katalogu.json"), cancellationToken), jsonOptions)!;
        await BulkAsync(connection, "INSERT INTO KURAL(KURAL_KODU,AD,KATEGORI,ACIKLAMA,SEVERITY,AGIRLIK,PARAMETRE_JSON,VERSIYON,YURURLUK_BASLANGIC,AKTIF_MI,GEREKCE,ONERILEN_AKSIYON) VALUES(:KuralKodu,:Name,:Category,:Description,:Severity,:Agirlik,:ParametersJson,:Version,:YururlukBaslangic,:IsActive,:Gerekce,:OnerilenAksiyon)", kurallar, cancellationToken, transaction);
        var referenceData = Reference();
        await BulkAsync(connection, "INSERT INTO SUT_ISLEM(KOD,AD,PUAN,BIRIM_FIYAT,GECERLILIK_BASLANGIC,GECERLILIK_BITIS,CINSIYET_KISITI,YAS_MIN,YAS_MAX,PAKET_MI,AMELIYAT_MI,BASVURU_TIPI_KISITI) VALUES(:Code,:Name,:Puan,:BirimFiyat,:GecerlilikBaslangic,:GecerlilikBitis,:CinsiyetKisiti,:YasMin,:YasMax,:PaketMi,:AmeliyatMi,:BasvuruTipiKisiti)", referenceData.Islemler, cancellationToken, transaction);
        await BulkAsync(connection, "INSERT INTO ICD10(KOD,AD,CINSIYET_KISITI,YAS_MIN,YAS_MAX,GECERLILIK_BASLANGIC,GECERLILIK_BITIS) VALUES(:Code,:Name,:CinsiyetKisiti,:YasMin,:YasMax,:GecerlilikBaslangic,:GecerlilikBitis)", referenceData.Tanilar, cancellationToken, transaction);
        await BulkAsync(connection, "INSERT INTO ISLEM_TANI_MATRIS(SUT_ISLEM_KODU,ICD10_KOD_ONEGI,ZORUNLU_MU) VALUES(:SutIslemKodu,:Icd10KodOnegi,:ZorunluMu)", referenceData.Matris, cancellationToken, transaction);
        await BulkAsync(connection, "INSERT INTO BIRLIKTE_FATURALANMAZ(SUT_ISLEM_KODU_A,SUT_ISLEM_KODU_B,KAPSAM,GEREKCE) VALUES(:SutIslemKoduA,:SutIslemKoduB,:Scope,:Gerekce)", referenceData.BirlikteFaturalanmaz, cancellationToken, transaction);
        await BulkAsync(connection, "INSERT INTO PAKET_ICERIK(PAKET_KODU,KAPSANAN_ISLEM_KODU) VALUES(:PaketKodu,:KapsananIslemKodu)", referenceData.Paketler, cancellationToken, transaction);
        await BulkAsync(connection, "INSERT INTO ISLEM_TEKRAR_LIMIT(SUT_ISLEM_KODU,PERIYOT_GUN,AZAMI_ADET) VALUES(:SutIslemKodu,:PeriyotGun,:AzamiAdet)", referenceData.TekrarLimitleri, cancellationToken, transaction);
        await BulkAsync(connection, "INSERT INTO BELGE_ZORUNLULUK(SUT_ISLEM_KODU,BELGE_TIPI,E_IMZA_ZORUNLU_MU) VALUES(:SutIslemKodu,:BelgeTip,:EImzaZorunluMu)", referenceData.BelgeZorunluluklari, cancellationToken, transaction);
        await BulkAsync(connection, "INSERT INTO ILAC_MALZEME(BARKOD,AD,ODEME_KISITI,RAPOR_ZORUNLU_MU,GECERLILIK_BASLANGIC,GECERLILIK_BITIS) VALUES(:Barkod,:Name,:OdemeKisiti,:RaporZorunluMu,:GecerlilikBaslangic,:GecerlilikBitis)", referenceData.Ilaclar, cancellationToken, transaction);
        var month = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        for (var offset = -2; offset <= 0; offset++)
        {
            var seedMonth = month.AddMonths(offset);
            await SeedPeriodAsync(connection, transaction, seedMonth, 1000, 1, cancellationToken);
            await connection.ExecuteAsync(CreateCommand("INSERT INTO DENETIM_ISI(DONEM,KURUM_ID,KULLANICI_ID,OLUSTURAN_KULLANICI_ID) VALUES(:Donem,1,7,7)", new { Donem = seedMonth.Year * 100 + seedMonth.Month }, cancellationToken, transaction));
        }

        await SeedPeriodAsync(connection, transaction, month, 2, 2, cancellationToken);
        await connection.ExecuteAsync(CreateCommand("INSERT INTO TOHUMLAMA(ID,SURUM) VALUES(1,:SeedVersion)", new { SeedVersion = "sentetik-v1" }, cancellationToken, transaction));
        transaction.Commit();
        logger.LogInformation("Sentetik veri hazır: 3 dönem, 15.000 kalem ve kurum izolasyonu örnekleri.");
    }

    public static ReferansVeri Reference()
    {
        var referenceData = new ReferansVeri();
        for (var i = 1; i <= 12; i++)
            referenceData.Islemler.Add(new SutIslem { Code = $"TST{i:000}", Name = $"Temsili hizmet {i:00}", Puan = 100 + i * 10, BirimFiyat = 150 + i * 75, GecerlilikBaslangic = new(2020, 1, 1), YasMin = 0, YasMax = 120 });
        referenceData.Islemler[5].AmeliyatMi = true;
        referenceData.Islemler[5].PaketMi = true;
        referenceData.Islemler[5].BasvuruTipiKisiti = BasvuruTip.Yatan;
        referenceData.Islemler[6].CinsiyetKisiti = Cinsiyet.Kadin;
        referenceData.Islemler[7].YasMax = 17;
        referenceData.Tanilar.AddRange([new() { Code = "J06.9", Name = "Üst solunum yolu enfeksiyonu, tanımlanmamış · temsili", GecerlilikBaslangic = new(2020, 1, 1) }, new() { Code = "I10", Name = "Esansiyel hipertansiyon · temsili", GecerlilikBaslangic = new(2020, 1, 1) }, new() { Code = "N40", Name = "Prostat hiperplazisi · temsili", CinsiyetKisiti = Cinsiyet.Erkek, YasMin = 18, GecerlilikBaslangic = new(2020, 1, 1) }]);
        referenceData.Matris.Add(new() { SutIslemKodu = "TST006", Icd10KodOnegi = "I", ZorunluMu = true });
        referenceData.BirlikteFaturalanmaz.Add(new() { SutIslemKoduA = "TST006", SutIslemKoduB = "TST005", Scope = "AYNI_GUN", Gerekce = "Yalnızca portföy için tanımlı temsili çakışma." });
        referenceData.Paketler.Add(new() { PaketKodu = "TST006", KapsananIslemKodu = "TST004" });
        referenceData.TekrarLimitleri.Add(new() { SutIslemKodu = "TST002", PeriyotGun = 30, AzamiAdet = 1 });
        referenceData.BelgeZorunluluklari.AddRange([new() { SutIslemKodu = "TST006", BelgeTip = BelgeTip.AmeliyatNotu, EImzaZorunluMu = true }, new() { SutIslemKodu = "TST009", BelgeTip = BelgeTip.SaglikKuruluRaporu, EImzaZorunluMu = true }]);
        referenceData.Ilaclar.Add(new() { Barkod = "TEST-BARKOD-001", Name = "Sentetik ilaç", OdemeKisiti = "Belgelendirme kontrolü", RaporZorunluMu = true, GecerlilikBaslangic = new(2020, 1, 1) });
        return referenceData;
    }

    public async Task SeedPeriodAsync(OracleConnection connection, OracleTransaction transaction, DateTime month, int faturaSayisi, long kurum, CancellationToken cancellationToken)
    {
        var donem = month.Year * 100 + month.Month;
        var referenceData = (long)donem * 100000 + kurum * 20000;
        await connection.ExecuteAsync(CreateCommand("INSERT INTO DONEM(DONEM_KODU,KURUM_ID,KAPANIS_TARIHI,KAPALI_MI) VALUES(:Donem,:Kurum,:Kapanis,0)", new { Donem = donem, Kurum = kurum, Kapanis = month.AddMonths(1).AddDays(15) }, cancellationToken, transaction));
        List<Hasta> hastalar = [];
        List<Basvuru> basvurular = [];
        List<Fatura> faturalar = [];
        List<FaturaKalemi> kalemler = [];
        List<BasvuruTani> tanilar = [];
        List<Belge> belgeler = [];
        string[] klinikler = ["DAHILIYE", "KARDIYOLOJI", "CERRAHI", "COCUK", "KADIN"];
        for (var i = 1; i <= faturaSayisi; i++)
        {
            var id = referenceData + i;
            var klinik = kurum == 2 ? "DAHILIYE" : klinikler[(i - 1) % 5];
            var date = month.AddDays(Math.Min((i - 1) % 8, Math.Max(0, (DateTime.UtcNow.Date - month).Days)));
            var variant = i % 20;
            hastalar.Add(new() { Id = id, HastaNo = $"S-{id}", AdSoyad = (await encryptionService.EncryptAsync($"SENTETİK HASTA {id}", cancellationToken)), KimlikNo = (await encryptionService.EncryptAsync("0" + (id % 10000000000).ToString("D10"), cancellationToken)), DogumTarihi = month.AddYears(-40).AddDays(-i % 300), Cinsiyet = i % 2 == 0 ? Cinsiyet.Kadin : Cinsiyet.Erkek, CreatedAt = date });
            basvurular.Add(new() { Id = id, HastaId = id, KurumId = kurum, TakipNo = $"TEST-T-{id}", ProvizyonNo = variant == 1 ? null : $"TEST-P-{id}", ProvizyonTarihi = variant == 2 ? date.AddDays(1) : date, BasvuruTip = variant == 3 ? BasvuruTip.Yatan : BasvuruTip.Ayaktan, KlinikKodu = klinik, HekimKodu = "TEST-H01", YatisTarihi = variant == 3 ? date : null, CikisTarihi = variant == 3 ? date.AddDays(-1) : null, CreatedAt = date });
            var fatura = new Fatura
            {
                Id = id,
                Donem = donem,
                BasvuruId = id,
                FaturaNo = $"FT-{donem}-{kurum}-{i:D5}",
                CreatedAt = date
            };
            if (variant != 4)
                tanilar.Add(new() { BasvuruId = id, Icd10Kod = variant == 5 ? "N40" : "J06.9", TaniTip = TaniTip.Ana, KayitTarihi = date, CreatedAt = date });
            for (var j = 1; j <= 5; j++)
            {
                var code = variant is 6 or 7 && j == 1 ? "TST006" : variant == 8 && j == 1 ? "TST007" : variant == 9 && j == 1 ? "TST008" : variant == 10 && j == 1 ? "TST009" : $"TST{j:000}";
                var price = 150 + int.Parse(code[3..]) * 75;
                var faturaKalemi = new FaturaKalemi
                {
                    Id = id * 10 + j,
                    FaturaId = id,
                    Donem = donem,
                    SutIslemKodu = code,
                    IslemTarihi = date,
                    Adet = 1,
                    BirimFiyat = price,
                    Tutar = price,
                    HekimKodu = "TEST-H01",
                    KlinikKodu = klinik,
                    CreatedAt = date
                };
                if (variant == 11 && j == 2)
                {
                    faturaKalemi.Adet = 3;
                    faturaKalemi.Tutar = price * 3;
                }

                if (variant == 12 && j == 1)
                    faturaKalemi.Tutar += 100;
                if (variant == 13 && j == 1)
                {
                    faturaKalemi.KalemTipi = KalemTip.Ilac;
                    faturaKalemi.Barkod = "TEST-BARKOD-001";
                }

                if (variant == 14 && j == 1)
                    faturaKalemi.IslemTarihi = month.AddDays(-1);
                if (variant == 15 && j == 1)
                    faturaKalemi.SutIslemKodu = "TST-YOK";
                if (variant == 16 && j == 1)
                {
                    faturaKalemi.KalemTipi = KalemTip.Malzeme;
                    faturaKalemi.Barkod = "TEST-YOK";
                }

                kalemler.Add(faturaKalemi);
                fatura.ToplamTutar += faturaKalemi.Tutar;
            }

            if (variant == 17)
                fatura.ToplamTutar += 250;
            if (variant == 10)
                belgeler.Add(new() { BasvuruId = id, BelgeTip = BelgeTip.SaglikKuruluRaporu, BelgeTarihi = date.AddDays(-10), GecerlilikBitis = date.AddDays(-1), ImzaDurum = ImzaDurum.Imzasiz, DosyaReferansi = $"sentetik:{id}", CreatedAt = date });
            faturalar.Add(fatura);
        }

        await BulkAsync(connection, "INSERT INTO HASTA(ID,HASTA_NO,AD_SOYAD,KIMLIK_NO,DOGUM_TARIHI,CINSIYET,OLUSTURMA_TARIHI) VALUES(:Id,:HastaNo,:AdSoyad,:KimlikNo,:DogumTarihi,:Cinsiyet,:CreatedAt)", hastalar, cancellationToken, transaction);
        await BulkAsync(connection, "INSERT INTO BASVURU(ID,HASTA_ID,KURUM_ID,TAKIP_NO,PROVIZYON_NO,PROVIZYON_TARIHI,BASVURU_TIPI,KLINIK_KODU,YATIS_TARIHI,CIKIS_TARIHI,HEKIM_KODU,OLUSTURMA_TARIHI) VALUES(:Id,:HastaId,:KurumId,:TakipNo,:ProvizyonNo,:ProvizyonTarihi,:BasvuruTip,:KlinikKodu,:YatisTarihi,:CikisTarihi,:HekimKodu,:CreatedAt)", basvurular, cancellationToken, transaction);
        await BulkAsync(connection, "INSERT INTO FATURA(ID,DONEM,BASVURU_ID,FATURA_NO,TOPLAM_TUTAR,OLUSTURMA_TARIHI) VALUES(:Id,:Donem,:BasvuruId,:FaturaNo,:ToplamTutar,:CreatedAt)", faturalar, cancellationToken, transaction);
        await BulkAsync(connection, "INSERT INTO FATURA_KALEMI(ID,FATURA_ID,DONEM,SUT_ISLEM_KODU,ISLEM_TARIHI,ADET,BIRIM_FIYAT,TUTAR,HEKIM_KODU,KLINIK_KODU,PAKET_KALEM_MI,KALEM_TIPI,BARKOD,ODEME_KOSULU_SAGLANDI_MI,OLUSTURMA_TARIHI) VALUES(:Id,:FaturaId,:Donem,:SutIslemKodu,:IslemTarihi,:Adet,:BirimFiyat,:Tutar,:HekimKodu,:KlinikKodu,:PaketKalemMi,:KalemTipi,:Barkod,:OdemeKosuluSaglandiMi,:CreatedAt)", kalemler, cancellationToken, transaction);
        await BulkAsync(connection, "INSERT INTO BASVURU_TANI(BASVURU_ID,ICD10_KOD,TANI_TIPI,TARIH,OLUSTURMA_TARIHI) VALUES(:BasvuruId,:Icd10Kod,:TaniTip,:KayitTarihi,:CreatedAt)", tanilar, cancellationToken, transaction);
        await BulkAsync(connection, "INSERT INTO BELGE(BASVURU_ID,BELGE_TIPI,BELGE_TARIHI,GECERLILIK_BITIS,IMZA_DURUMU,DOSYA_REFERANSI,OLUSTURMA_TARIHI) VALUES(:BasvuruId,:BelgeTip,:BelgeTarihi,:GecerlilikBitis,:ImzaDurum,:DosyaReferansi,:CreatedAt)", belgeler, cancellationToken, transaction);
    }
}
