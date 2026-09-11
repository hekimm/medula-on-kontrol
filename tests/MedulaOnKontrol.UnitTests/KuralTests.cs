using FluentAssertions;
using MedulaOnKontrol.Application;
using MedulaOnKontrol.Domain;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace MedulaOnKontrol.UnitTests;
public sealed class KuralTests
{
    public static IFaturaKurali[] Handlers() => [new ProvizyonKurali(), new TaniKurali(), new IslemTaniKurali(), new IslemKurali(), new TutarKurali(), new BelgeKurali(), new IlacKurali(), new SureKurali()];
    public static IEnumerable<object[]> Codes() => typeof(KuralKodlari).GetFields().Select(fieldInfo => new object[] { (string)fieldInfo.GetValue(null)! });
    public static Kural Kural(string code) => new()
    {
        KuralKodu = code,
        Category = code.Split('-')[0],
        Name = "Test kuralı",
        Gerekce = "Kaynak kayıt tutarsızdır.",
        OnerilenAksiyon = "Kaynağı doğrulayın.",
        Agirlik = 20,
        Severity = Severity.High,
        YururlukBaslangic = new(2020, 1, 1)
    };
    public static FaturaDenetimBaglami Clean(int takip = 1)
    {
        var date = new DateTime(2026, 6, 10);
        var context = new FaturaDenetimBaglami
        {
            Fatura = new()
            {
                Id = 1,
                Donem = 202606,
                FaturaNo = "TEST-FATURA",
                BasvuruId = 1,
                ToplamTutar = 300,
                Revision = 0
            },
            Basvuru = new()
            {
                Id = 1,
                HastaId = 1,
                KurumId = 1,
                TakipNo = "TEST-T",
                ProvizyonNo = "TEST-P",
                ProvizyonTarihi = date,
                BasvuruTip = BasvuruTip.Ayaktan
            },
            Hasta = new()
            {
                Id = 1,
                DogumTarihi = new(1986, 6, 11),
                Cinsiyet = Cinsiyet.Kadin
            },
            Donem = new()
            {
                DonemKodu = 202606,
                KapanisTarihi = new(2026, 7, 15)
            },
            Reference = new(),
            UtcNow = new(2026, 6, 20),
            AyniTakipFaturaSayisi = takip
        };
        context.Kalemler.AddRange([new() { Id = 10, FaturaId = 1, Donem = 202606, SutIslemKodu = "A", IslemTarihi = date, Adet = 1, BirimFiyat = 100, Tutar = 100, CreatedAt = date }, new() { Id = 11, FaturaId = 1, Donem = 202606, SutIslemKodu = "B", IslemTarihi = date, Adet = 1, BirimFiyat = 200, Tutar = 200, CreatedAt = date }]);
        context.Reference.Islemler.AddRange([new() { Code = "A", BirimFiyat = 100, GecerlilikBaslangic = new(2020, 1, 1), CinsiyetKisiti = Cinsiyet.Kadin, YasMin = 18, YasMax = 70, BasvuruTipiKisiti = BasvuruTip.Ayaktan }, new() { Code = "B", BirimFiyat = 200, GecerlilikBaslangic = new(2020, 1, 1) }]);
        context.Tanilar.Add(new() { Icd10Kod = "I10", TaniTip = TaniTip.Ana, KayitTarihi = date });
        context.Reference.Tanilar.Add(new() { Code = "I10", GecerlilikBaslangic = new(2020, 1, 1), CinsiyetKisiti = Cinsiyet.Kadin, YasMin = 18, YasMax = 70 });
        context.Reference.Matris.Add(new() { SutIslemKodu = "A", Icd10KodOnegi = "I", ZorunluMu = true });
        context.Reference.TekrarLimitleri.Add(new() { SutIslemKodu = "A", PeriyotGun = 30, AzamiAdet = 2 });
        context.Reference.BelgeZorunluluklari.AddRange([new() { SutIslemKodu = "A", BelgeTip = BelgeTip.Epikriz, EImzaZorunluMu = true }, new() { SutIslemKodu = "B", BelgeTip = BelgeTip.SaglikKuruluRaporu, EImzaZorunluMu = true }]);
        context.Belgeler.AddRange([new() { BelgeTip = BelgeTip.Epikriz, BelgeTarihi = date, ImzaDurum = ImzaDurum.EImzali }, new() { BelgeTip = BelgeTip.SaglikKuruluRaporu, BelgeTarihi = date, GecerlilikBitis = date.AddYears(1), ImzaDurum = ImzaDurum.EImzali }]);
        context.Reference.Ilaclar.Add(new() { Barkod = "TEST-BAR", RaporZorunluMu = true, OdemeKisiti = "Test koşulu", GecerlilikBaslangic = new(2020, 1, 1) });
        context.GecmisKalemler.AddRange(context.Kalemler.Select(faturaKalemi => new GecmisKalem { Id = faturaKalemi.Id, FaturaId = faturaKalemi.FaturaId, BasvuruId = 1, HastaId = 1, SutIslemKodu = faturaKalemi.SutIslemKodu, IslemTarihi = faturaKalemi.IslemTarihi, Adet = faturaKalemi.Adet }));
        return context;
    }

    public static FaturaDenetimBaglami Violation(string code)
    {
        var context = Clean(code == KuralKodlari.Prv004 ? 2 : 1);
        var faturaKalemi = context.Kalemler[0];
        var islem = context.Reference.Islemler[0];
        var icd10Tani = context.Reference.Tanilar[0];
        switch (code)
        {
            case KuralKodlari.Prv001:
                context.Basvuru.ProvizyonNo = null;
                break;
            case KuralKodlari.Prv002:
                context.Basvuru.ProvizyonTarihi = faturaKalemi.IslemTarihi.AddDays(1);
                break;
            case KuralKodlari.Prv003:
                context.Basvuru.BasvuruTip = BasvuruTip.Yatan;
                context.Basvuru.YatisTarihi = faturaKalemi.IslemTarihi;
                context.Basvuru.CikisTarihi = faturaKalemi.IslemTarihi.AddDays(-1);
                break;
            case KuralKodlari.Prv004:
                break;
            case KuralKodlari.Prv005:
                context.Basvuru.TakipNo = " ";
                break;
            case KuralKodlari.Tani001:
                context.Tanilar.Clear();
                break;
            case KuralKodlari.Tani002:
                icd10Tani.GecerlilikBitis = faturaKalemi.IslemTarihi.AddDays(-1);
                break;
            case KuralKodlari.Tani003:
                icd10Tani.CinsiyetKisiti = Cinsiyet.Erkek;
                break;
            case KuralKodlari.Tani004:
                icd10Tani.YasMax = 18;
                break;
            case KuralKodlari.Tani005:
                context.Tanilar.Add(new() { Icd10Kod = "I10", TaniTip = TaniTip.Ana });
                break;
            case KuralKodlari.Tani006:
                context.Tanilar[0].Icd10Kod = "TEST-YOK";
                break;
            case KuralKodlari.Itu001:
                context.Reference.Matris[0].Icd10KodOnegi = "N";
                break;
            case KuralKodlari.Itu002:
                islem.BasvuruTipiKisiti = BasvuruTip.Yatan;
                break;
            case KuralKodlari.Isl001:
                context.Reference.BirlikteFaturalanmaz.Add(new() { SutIslemKoduA = "A", SutIslemKoduB = "B", Scope = "AYNI_GUN" });
                break;
            case KuralKodlari.Isl002:
                context.Reference.Paketler.Add(new() { PaketKodu = "B", KapsananIslemKodu = "A" });
                break;
            case KuralKodlari.Isl003:
                context.GecmisKalemler.Add(new() { Id = 9, FaturaId = 2, BasvuruId = 2, HastaId = 1, SutIslemKodu = "A", IslemTarihi = faturaKalemi.IslemTarihi.AddDays(-1), Adet = 2 });
                break;
            case KuralKodlari.Isl004:
                islem.CinsiyetKisiti = Cinsiyet.Erkek;
                break;
            case KuralKodlari.Isl005:
                islem.YasMin = 60;
                break;
            case KuralKodlari.Isl006:
                islem.GecerlilikBitis = faturaKalemi.IslemTarihi.AddDays(-1);
                break;
            case KuralKodlari.Isl007:
                context.GecmisKalemler.Add(new() { Id = 99, FaturaId = 2, HastaId = 1, SutIslemKodu = "A", IslemTarihi = faturaKalemi.IslemTarihi, Adet = 1 });
                break;
            case KuralKodlari.Isl008:
                faturaKalemi.Adet = 0;
                break;
            case KuralKodlari.Isl009:
                faturaKalemi.IslemTarihi = context.UtcNow.AddDays(1);
                break;
            case KuralKodlari.Tut001:
                faturaKalemi.Tutar = 105;
                break;
            case KuralKodlari.Tut002:
                faturaKalemi.BirimFiyat = 105;
                break;
            case KuralKodlari.Tut003:
                context.Fatura.ToplamTutar = 100;
                break;
            case KuralKodlari.Tut004:
                faturaKalemi.BirimFiyat = -1;
                break;
            case KuralKodlari.Blg001:
                context.Belgeler.Clear();
                break;
            case KuralKodlari.Blg002:
                context.Belgeler[0].ImzaDurum = ImzaDurum.Imzasiz;
                break;
            case KuralKodlari.Blg003:
                context.Belgeler[1].GecerlilikBitis = faturaKalemi.IslemTarihi.AddDays(-1);
                break;
            case KuralKodlari.Blg004:
                context.Basvuru.BasvuruTip = BasvuruTip.Yatan;
                context.Belgeler.RemoveAt(0);
                break;
            case KuralKodlari.Blg005:
                islem.AmeliyatMi = true;
                break;
            case KuralKodlari.Blg006:
                context.Belgeler[0].BelgeTarihi = context.UtcNow.AddDays(1);
                break;
            case KuralKodlari.Ilc001:
                faturaKalemi.KalemTipi = KalemTip.Ilac;
                faturaKalemi.Barkod = "YOK";
                break;
            case KuralKodlari.Ilc002:
                faturaKalemi.KalemTipi = KalemTip.Ilac;
                faturaKalemi.Barkod = "TEST-BAR";
                context.Belgeler.Clear();
                break;
            case KuralKodlari.Ilc003:
                faturaKalemi.KalemTipi = KalemTip.Malzeme;
                faturaKalemi.Barkod = "TEST-BAR";
                break;
            case KuralKodlari.Ilc004:
                faturaKalemi.KalemTipi = KalemTip.Ilac;
                faturaKalemi.Barkod = "TEST-BAR";
                context.Reference.Ilaclar[0].GecerlilikBitis = faturaKalemi.IslemTarihi.AddDays(-1);
                break;
            case KuralKodlari.Sur001:
                context.Donem.KapanisTarihi = context.UtcNow.AddDays(-1);
                break;
            case KuralKodlari.Sur002:
                context.Donem.KapaliMi = true;
                context.Donem.KapanisTarihi = faturaKalemi.CreatedAt.AddDays(-1);
                break;
            case KuralKodlari.Sur003:
                faturaKalemi.IslemTarihi = faturaKalemi.IslemTarihi.AddMonths(-1);
                break;
            case KuralKodlari.Sur004:
                context.Basvuru.BasvuruTip = BasvuruTip.Yatan;
                context.Basvuru.CikisTarihi = faturaKalemi.IslemTarihi.AddDays(-1);
                break;
            default:
                throw new InvalidOperationException(code);
        }

        return context;
    }

    [Theory, MemberData(nameof(Codes))]
    public void EveryRuleAcceptsCleanRecord(string code)
    {
        var context = Clean();
        if (code.StartsWith("ILC"))
        {
            context.Kalemler[0].KalemTipi = KalemTip.Ilac;
            context.Kalemler[0].Barkod = "TEST-BAR";
            context.Kalemler[0].OdemeKosuluSaglandiMi = true;
        }

        var kural = Kural(code);
        Handlers().Single(faturaKurali => faturaKurali.Category == kural.Category).Evaluate(context, kural).Should().BeEmpty();
    }

    [Theory, MemberData(nameof(Codes))]
    public void EveryRuleReportsViolationWithEvidence(string code)
    {
        var context = Violation(code);
        var kural = Kural(code);
        var result = Handlers().Single(faturaKurali => faturaKurali.Category == kural.Category).Evaluate(context, kural).ToList();
        result.Should().NotBeEmpty();
        result.Should().OnlyContain(bulgu => bulgu.KuralKodu == code && bulgu.KuralVersiyon == 1 && bulgu.FaturaId == 1 && bulgu.Gerekce.Contains("TEST-FATURA") && bulgu.OnerilenAksiyon.Length > 5);
    }

    [Fact]
    public async Task EffectiveDatesExcludeFutureAndExpiredRulesAsync()
    {
        var engine = Engine();
        var kural = Kural(KuralKodlari.Prv001);
        kural.YururlukBaslangic = new(2026, 7, 1);
        (await engine.EvaluateAsync(Violation(kural.KuralKodu), [kural], default)).Value!.Bulgular.Should().BeEmpty();
        kural.YururlukBaslangic = new(2020, 1, 1);
        kural.YururlukBitis = new(2026, 5, 31);
        (await engine.EvaluateAsync(Violation(kural.KuralKodu), [kural], default)).Value!.Bulgular.Should().BeEmpty();
        kural.YururlukBitis = new(2026, 6, 1);
        (await engine.EvaluateAsync(Violation(kural.KuralKodu), [kural], default)).Value!.Bulgular.Should().ContainSingle();
        kural.IsActive = false;
        (await engine.EvaluateAsync(Violation(kural.KuralKodu), [kural], default)).Value!.Bulgular.Should().BeEmpty();
    }

    public static KuralEngine Engine(bool normalize = true) => new(Handlers(), new RiskSkorlamaService(Options.Create(new SkorlamaOptions { NormalizeWeights = normalize })));
    [Fact]
    public async Task ExceptionIsBoundToRevisionVersionAndExpiryAsync()
    {
        var context = Violation(KuralKodlari.Prv001);
        var kural = Kural(KuralKodlari.Prv001);
        context.Exceptions.Add(new() { KuralKodu = kural.KuralKodu, KuralVersiyon = 1, FaturaRevizyon = 0, GecerlilikBitis = context.UtcNow.AddDays(1) });
        (await Engine().EvaluateAsync(context, [kural], default)).Value!.Risk.Score.Should().Be(0);
        context.Fatura.Revision++;
        (await Engine().EvaluateAsync(context, [kural], default)).Value!.Risk.Score.Should().BeGreaterThan(0);
        context.Fatura.Revision = 0;
        context.Exceptions[0].GecerlilikBitis = context.UtcNow;
        (await Engine().EvaluateAsync(context, [kural], default)).Value!.Risk.Score.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RiskAmountDeduplicatesItemsAndCapsInvoiceLevelFindingsAsync()
    {
        var context = Clean();
        var score = new RiskSkorlamaService(Options.Create(new SkorlamaOptions { NormalizeWeights = true }));
        Bulgu[] bulgular = [new()
        {
            FaturaKalemiId = 10,
            Severity = Severity.High,
            Agirlik = 20,
            EtkilenenTutar = 100
        }, new()
        {
            FaturaKalemiId = 10,
            Severity = Severity.Blocking,
            Agirlik = 20,
            EtkilenenTutar = 100
        }

        ];
        (await score.CalculateAsync(context, bulgular, default)).RisktekiTutar.Should().Be(100);
        bulgular[1].FaturaKalemiId = null;
        bulgular[1].EtkilenenTutar = 300;
        (await score.CalculateAsync(context, bulgular, default)).RisktekiTutar.Should().Be(300);
        context.Fatura.ToplamTutar = 0;
        (await score.CalculateAsync(context, bulgular, default)).Score.Should().Be(100);
        (await score.CalculateAsync(context, [], default)).Score.Should().Be(0);
    }

    [Fact]
    public void PriceToleranceAndRepeatParametersAreUsed()
    {
        var context = Violation(KuralKodlari.Tut001);
        var kural = Kural(KuralKodlari.Tut001);
        kural.ParametersJson = "{\"tolerans\":10}";
        new TutarKurali().Evaluate(context, kural).Should().BeEmpty();
        context = Violation(KuralKodlari.Isl003);
        kural = Kural(KuralKodlari.Isl003);
        kural.ParametersJson = "{\"periyotGun\":1,\"azamiAdet\":1}";
        new IslemKurali().Evaluate(context, kural).Should().BeEmpty();
    }

    [Fact]
    public void BirthdayAndExpiryBoundariesAreInclusive()
    {
        var context = Clean();
        context.CalculateAge(new(2026, 6, 10)).Should().Be(39);
        context.CalculateAge(new(2026, 6, 11)).Should().Be(40);
        FaturaDenetimBaglami.IsValidOn(new(2026, 1, 1), new(2026, 6, 10), new(2026, 6, 10)).Should().BeTrue();
    }

    [Fact]
    public async Task InaccessibleInvoiceDoesNotLoadCatalogOrPersistAsync()
    {
        var faturalar = Substitute.For<IFaturaRepository>();
        var ruleRepository = Substitute.For<IKuralRepository>();
        var invoiceValidationService = new FaturaKontrolService(faturalar, ruleRepository, Engine());
        var result = await invoiceValidationService.ValidateAsync(new(1, 1), 9, default);
        result.IsSuccess.Should().BeFalse();
        await ruleRepository.DidNotReceive().ListAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConflictingVersionsFailClosedAsync()
    {
        var kural = Kural(KuralKodlari.Prv001);
        var result = (await Engine().EvaluateAsync(Clean(), [kural, kural], default));
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Çakışan");
    }

    [Fact]
    public async Task CancellationIsObservedAsync()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        var action = async () => (await Engine().EvaluateAsync(Clean(), [Kural(KuralKodlari.Prv001)], cancellationTokenSource.Token));
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void SameDayExclusionSpansEncountersButEncounterScopeDoesNot()
    {
        var context = Violation(KuralKodlari.Isl001);
        context.GecmisKalemler[1].BasvuruId = 99;
        var kural = Kural(KuralKodlari.Isl001);
        new IslemKurali().Evaluate(context, kural).Should().NotBeEmpty();
        context.Reference.BirlikteFaturalanmaz[0].Scope = "AYNI_BASVURU";
        new IslemKurali().Evaluate(context, kural).Where(bulgu => bulgu.FaturaKalemiId == 10).Should().BeEmpty();
    }

    [Fact]
    public async Task UnknownRuleCodeCannotSilentlyPassAsync()
    {
        var kural = Kural("PRV-999");
        var result = (await Engine().EvaluateAsync(Clean(), [kural], default));
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("uygulayıcısı");
    }
}
