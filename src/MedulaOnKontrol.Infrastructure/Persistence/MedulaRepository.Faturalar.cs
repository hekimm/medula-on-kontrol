using System.Text.Json;
using Dapper;
using MedulaOnKontrol.Application;
using MedulaOnKontrol.Domain;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using static MedulaOnKontrol.Infrastructure.Persistence.OracleCommands;
namespace MedulaOnKontrol.Infrastructure.Persistence;

public sealed partial class MedulaRepository(IOracleConnectionFactory factory, IOptions<SkorlamaOptions> scoring) : IFaturaRepository, IKuralRepository, IKullaniciRepository, IDenetimRepository, IRedRepository, IDenetimIsiRepository, IGonderimRepository
{
    private const string AccessPredicate = " B.KURUM_ID=:KurumId AND EXISTS (SELECT 1 FROM KULLANICI_KLINIK_YETKI Y JOIN KULLANICI U ON U.ID=Y.KULLANICI_ID WHERE Y.KULLANICI_ID=:UserId AND U.AKTIF_MI=1 AND U.KURUM_ID=:KurumId AND Y.KURUM_ID=B.KURUM_ID AND Y.KLINIK_KODU=B.KLINIK_KODU) ";
    private const string SeciliFaturaSql = "SELECT F.ID FROM FATURA F JOIN BASVURU B ON B.ID=F.BASVURU_ID WHERE " + AccessPredicate + " AND (:Donem IS NULL OR F.DONEM=:Donem) AND (:Id IS NULL OR F.ID=:Id)";
    private const string FaturaSelectSql = "SELECT F.*, B.KLINIK_KODU, B.BASVURU_TIPI, H.HASTA_NO, (SELECT COUNT(*) FROM BULGU U WHERE U.FATURA_ID=F.ID AND U.DONEM=F.DONEM AND U.KURAL_CALISTIRMA_ID=F.SON_CALISTIRMA_ID AND U.SEVERITY=0 AND U.DURUM IN (0,3)) ENGELLEYICI_SAYISI FROM FATURA F JOIN BASVURU B ON B.ID=F.BASVURU_ID JOIN HASTA H ON H.ID=B.HASTA_ID ";
    private const string InsertAuditSql = "INSERT INTO DENETIM_IZI(KULLANICI_ID,KURUM_ID,KLINIK_KODU,ISLEM_TIPI,VARLIK_ADI,VARLIK_ID,ESKI_DEGER_JSON,YENI_DEGER_JSON,IP_ADRES,OLUSTURAN_KULLANICI_ID) VALUES(:UserId,:KurumId,:KlinikKodu,:OperationType,:EntityName,:EntityId,:PreviousValueJson,:NewValueJson,:IpAddress,:UserId)";
    private static Task AppendAuditAsync(OracleConnection connection, ErisimKapsami accessScope, DenetimIslem operation, string entityName, string id, object? newValue, CancellationToken cancellationToken, OracleTransaction? transaction = null, string? klinik = null, object? previousValue = null) => connection.ExecuteAsync(CreateCommand(InsertAuditSql, new { accessScope.UserId, accessScope.KurumId, KlinikKodu = klinik, OperationType = (int)operation, EntityName = entityName, EntityId = id, PreviousValueJson = previousValue == null ? null : JsonSerializer.Serialize(previousValue), NewValueJson = newValue == null ? null : JsonSerializer.Serialize(newValue), accessScope.IpAddress }, cancellationToken, transaction));

    public async Task<PagedResult<Fatura>> ListAsync(ErisimKapsami accessScope, FaturaFilter filter, CancellationToken cancellationToken)
    {
        await using var connection = await factory.OpenAsync(cancellationToken);
        const string FilterPredicate = " WHERE " + AccessPredicate + " AND (:Donem IS NULL OR F.DONEM=:Donem) AND (:Klinik IS NULL OR B.KLINIK_KODU=:Klinik) AND (:BasvuruTip IS NULL OR B.BASVURU_TIPI=:BasvuruTip) AND (:Status IS NULL OR F.DURUM=:Status) AND F.RISK_SKORU BETWEEN :RiskMin AND :RiskMax";
        var size = Math.Clamp(filter.PageSize, 1, 100); var page = Math.Max(1, filter.PageNumber);
        var parameters = new { accessScope.UserId, accessScope.KurumId, filter.Donem, filter.Klinik, filter.BasvuruTip, filter.Status, filter.RiskMin, filter.RiskMax, filter.SortBy, filter.SortDirection, PageOffset = (page - 1) * size, PageLimit = size };
        var count = await connection.ExecuteScalarAsync<int>(CreateCommand("SELECT COUNT(*) FROM FATURA F JOIN BASVURU B ON B.ID=F.BASVURU_ID" + FilterPredicate, parameters, cancellationToken));
        var rows = (await connection.QueryAsync<Fatura>(CreateCommand(FaturaSelectSql + FilterPredicate + " ORDER BY CASE WHEN :SortBy='risk' AND :SortDirection='descending' THEN F.RISK_SKORU END DESC, CASE WHEN :SortBy='risk' AND :SortDirection='ascending' THEN F.RISK_SKORU END ASC, CASE WHEN :SortBy='amount' AND :SortDirection='descending' THEN F.TOPLAM_TUTAR END DESC, CASE WHEN :SortBy='amount' AND :SortDirection='ascending' THEN F.TOPLAM_TUTAR END ASC, CASE WHEN :SortBy='invoiceNumber' AND :SortDirection='ascending' THEN F.FATURA_NO END ASC, CASE WHEN :SortBy='invoiceNumber' AND :SortDirection='descending' THEN F.FATURA_NO END DESC, F.ID OFFSET :PageOffset ROWS FETCH NEXT :PageLimit ROWS ONLY", parameters, cancellationToken))).ToList();
        await AppendAuditAsync(connection, accessScope, DenetimIslem.View, "FATURA_LISTESI", filter.Donem?.ToString() ?? "TUM", new { page, size, count }, cancellationToken);
        return new(rows, count, page, size);
    }
    public async Task<FaturaDenetimBaglami?> GetAsync(ErisimKapsami scope, long id, CancellationToken cancellationToken) => (await LoadAsync(scope, null, id, cancellationToken)).FirstOrDefault();
    public Task<IReadOnlyList<FaturaDenetimBaglami>> LoadPeriodAsync(ErisimKapsami scope, int donem, CancellationToken cancellationToken) => LoadAsync(scope, donem, null, cancellationToken);
    private async Task<IReadOnlyList<FaturaDenetimBaglami>> LoadAsync(ErisimKapsami accessScope, int? donem, long? id, CancellationToken cancellationToken)
    {
        await using var connection = await factory.OpenAsync(cancellationToken);
        var parameters = new { accessScope.UserId, accessScope.KurumId, Donem = donem, Id = id };
        var faturalar = (await connection.QueryAsync<Fatura>(CreateCommand(FaturaSelectSql + " WHERE F.ID IN (" + SeciliFaturaSql + ")", parameters, cancellationToken))).ToList();
        if (faturalar.Count == 0) return [];
        var basvurular = (await connection.QueryAsync<Basvuru>(CreateCommand("SELECT B.* FROM BASVURU B WHERE B.ID IN (SELECT BASVURU_ID FROM FATURA WHERE ID IN (" + SeciliFaturaSql + "))", parameters, cancellationToken))).ToDictionary(basvuru => basvuru.Id);
        var hastalar = (await connection.QueryAsync<Hasta>(CreateCommand("SELECT H.* FROM HASTA H WHERE H.ID IN (SELECT B.HASTA_ID FROM BASVURU B JOIN FATURA F ON F.BASVURU_ID=B.ID WHERE F.ID IN (" + SeciliFaturaSql + "))", parameters, cancellationToken))).ToDictionary(hasta => hasta.Id);
        var kalemler = (await connection.QueryAsync<FaturaKalemi>(CreateCommand("SELECT K.* FROM FATURA_KALEMI K WHERE K.FATURA_ID IN (" + SeciliFaturaSql + ")", parameters, cancellationToken))).ToLookup(faturaKalemi => faturaKalemi.FaturaId);
        var tanilar = (await connection.QueryAsync<BasvuruTani>(CreateCommand("SELECT T.* FROM BASVURU_TANI T WHERE T.BASVURU_ID IN (SELECT BASVURU_ID FROM FATURA WHERE ID IN (" + SeciliFaturaSql + "))", parameters, cancellationToken))).ToLookup(basvuruTani => basvuruTani.BasvuruId);
        var belgeler = (await connection.QueryAsync<Belge>(CreateCommand("SELECT T.* FROM BELGE T WHERE T.BASVURU_ID IN (SELECT BASVURU_ID FROM FATURA WHERE ID IN (" + SeciliFaturaSql + "))", parameters, cancellationToken))).ToLookup(belge => belge.BasvuruId);
        var periods = (await connection.QueryAsync<Donem>(CreateCommand("SELECT * FROM DONEM WHERE KURUM_ID=:KurumId", parameters, cancellationToken))).ToDictionary(selectedBillingPeriod => selectedBillingPeriod.DonemKodu);
        // Klinikler arası geçmiş sorgusu aynı kurumla ve gerekli işlem alanlarıyla sınırlıdır.
        var historicalLines = (await connection.QueryAsync<GecmisKalem>(CreateCommand("SELECT K.ID,K.FATURA_ID,K.DONEM,K.SUT_ISLEM_KODU,K.ISLEM_TARIHI,K.ADET,B.HASTA_ID,B.ID BASVURU_ID,B.TAKIP_NO FROM FATURA_KALEMI K JOIN FATURA F ON F.ID=K.FATURA_ID JOIN BASVURU B ON B.ID=F.BASVURU_ID WHERE B.KURUM_ID=:KurumId AND B.HASTA_ID IN (SELECT BX.HASTA_ID FROM BASVURU BX JOIN FATURA FX ON FX.BASVURU_ID=BX.ID WHERE FX.ID IN (" + SeciliFaturaSql + "))", parameters, cancellationToken))).ToLookup(gecmisKalem => gecmisKalem.HastaId);
        var takipler = (await connection.QueryAsync<(string TakipNo, int Donem, int Adet)>(CreateCommand("SELECT B.TAKIP_NO,F.DONEM,COUNT(*) FROM FATURA F JOIN BASVURU B ON B.ID=F.BASVURU_ID WHERE B.KURUM_ID=:KurumId GROUP BY B.TAKIP_NO,F.DONEM", parameters, cancellationToken))).ToDictionary(valueTuple => (valueTuple.TakipNo ?? "", valueTuple.Donem), valueTuple => valueTuple.Adet);
        var exceptions = (await connection.QueryAsync<IstisnaRow>(CreateCommand("SELECT I.*,U.FATURA_ID,U.KURAL_KODU,U.KURAL_VERSIYON,U.FATURA_KALEMI_ID FROM BULGU_ISTISNA I JOIN BULGU U ON U.ID=I.BULGU_ID WHERE U.FATURA_ID IN (" + SeciliFaturaSql + ") AND I.GECERLILIK_BITIS>SYS_EXTRACT_UTC(SYSTIMESTAMP)", parameters, cancellationToken))).ToLookup(findingExceptionRow => findingExceptionRow.FaturaId);
        var referenceData = await LoadReferenceDataAsync(connection, cancellationToken);
        var contexts = faturalar.Select(fatura => { var basvuru = basvurular[fatura.BasvuruId]; return new FaturaDenetimBaglami { Fatura = fatura, Basvuru = basvuru, Hasta = hastalar[basvuru.HastaId], Donem = periods[fatura.Donem], Kalemler = kalemler[fatura.Id].ToList(), Tanilar = tanilar[basvuru.Id].ToList(), Belgeler = belgeler[basvuru.Id].ToList(), Reference = referenceData, GecmisKalemler = historicalLines[basvuru.HastaId].ToList(), AyniTakipFaturaSayisi = takipler.GetValueOrDefault((basvuru.TakipNo ?? "", fatura.Donem)), Exceptions = exceptions[fatura.Id].Select(findingExceptionRow => findingExceptionRow.ToException()).ToList() }; }).ToList();
        await AppendAuditAsync(connection, accessScope, DenetimIslem.View, "FATURA", id?.ToString() ?? donem?.ToString() ?? "TUM", new { Adet = faturalar.Count }, cancellationToken, klinik: id.HasValue ? contexts[0].Basvuru.KlinikKodu : null);
        return contexts;
    }
    private sealed class IstisnaRow { public long FaturaId { get; set; } public long BulguId { get; set; } public string Gerekce { get; set; } = ""; public long OnaylayanKullaniciId { get; set; } public DateTime GecerlilikBitis { get; set; } public long FaturaRevizyon { get; set; } public string KuralKodu { get; set; } = ""; public int KuralVersiyon { get; set; } public long? FaturaKalemiId { get; set; } public BulguIstisna ToException() => new() { BulguId = BulguId, Gerekce = Gerekce, OnaylayanKullaniciId = OnaylayanKullaniciId, GecerlilikBitis = GecerlilikBitis, FaturaRevizyon = FaturaRevizyon, KuralKodu = KuralKodu, KuralVersiyon = KuralVersiyon, FaturaKalemiId = FaturaKalemiId }; }
    private static async Task<ReferansVeri> LoadReferenceDataAsync(OracleConnection connection, CancellationToken cancellationToken) => new()
    {
        Islemler = (await connection.QueryAsync<SutIslem>(CreateCommand("SELECT * FROM SUT_ISLEM", null, cancellationToken))).ToList(),
        Tanilar = (await connection.QueryAsync<Icd10>(CreateCommand("SELECT * FROM ICD10", null, cancellationToken))).ToList(),
        Matris = (await connection.QueryAsync<IslemTaniMatris>(CreateCommand("SELECT * FROM ISLEM_TANI_MATRIS", null, cancellationToken))).ToList(),
        BirlikteFaturalanmaz = (await connection.QueryAsync<BirlikteFaturalanmaz>(CreateCommand("SELECT * FROM BIRLIKTE_FATURALANMAZ", null, cancellationToken))).ToList(),
        Paketler = (await connection.QueryAsync<PaketIcerik>(CreateCommand("SELECT * FROM PAKET_ICERIK", null, cancellationToken))).ToList(),
        TekrarLimitleri = (await connection.QueryAsync<IslemTekrarLimit>(CreateCommand("SELECT * FROM ISLEM_TEKRAR_LIMIT", null, cancellationToken))).ToList(),
        BelgeZorunluluklari = (await connection.QueryAsync<BelgeZorunluluk>(CreateCommand("SELECT * FROM BELGE_ZORUNLULUK", null, cancellationToken))).ToList(),
        Ilaclar = (await connection.QueryAsync<IlacMalzeme>(CreateCommand("SELECT * FROM ILAC_MALZEME", null, cancellationToken))).ToList()
    };
    public async Task<IReadOnlyList<Bulgu>> ListBulgularAsync(ErisimKapsami accessScope, long id, CancellationToken cancellationToken)
    {
        await using var connection = await factory.OpenAsync(cancellationToken);
        return (await connection.QueryAsync<Bulgu>(CreateCommand("SELECT U.* FROM BULGU U JOIN FATURA F ON F.ID=U.FATURA_ID JOIN BASVURU B ON B.ID=F.BASVURU_ID WHERE " + AccessPredicate + " AND F.ID=:Id AND U.KURAL_CALISTIRMA_ID=F.SON_CALISTIRMA_ID ORDER BY U.SEVERITY,U.KURAL_KODU", new { accessScope.UserId, accessScope.KurumId, Id = id }, cancellationToken))).ToList();
    }
    public async Task<Result<KontrolSonucu>> SaveValidationAsync(ErisimKapsami accessScope, FaturaDenetimBaglami context, KontrolSonucu result, CancellationToken cancellationToken)
    {
        await using var connection = await factory.OpenAsync(cancellationToken); using var transaction = connection.BeginTransaction();
        var updated = await connection.ExecuteAsync(CreateCommand("UPDATE FATURA SET RISK_SKORU=:Score,RISKTEKI_TUTAR=:Tutar,SON_KONTROL_TARIHI=SYS_EXTRACT_UTC(SYSTIMESTAMP),KONTROL_REVIZYON=REVIZYON,DURUM=1,GUNCELLEME_TARIHI=SYS_EXTRACT_UTC(SYSTIMESTAMP),GUNCELLEYEN_KULLANICI_ID=:UserId WHERE ID=:Id AND REVIZYON=:Revision AND DURUM IN (0,1,2,3) AND NOT EXISTS (SELECT 1 FROM GONDERIM G WHERE G.FATURA_ID=FATURA.ID AND G.DURUM='BEKLIYOR') AND ID IN (" + SeciliFaturaSql + ")", new { accessScope.UserId, accessScope.KurumId, Donem = (int?)null, Id = (long?)context.Fatura.Id, context.Fatura.Revision, result.Risk.Score, Tutar = result.Risk.RisktekiTutar }, cancellationToken, transaction));
        if (updated != 1) return Result<KontrolSonucu>.Failure("Fatura değişti veya erişim yetkisi kaldırıldı; kontrolü yeniden başlatın.");
        var runId = await InsertIdAsync(connection, "INSERT INTO KURAL_CALISTIRMA(FATURA_ID,BASLANGIC,BITIS,KURAL_SAYISI,BULGU_SAYISI,CALISTIRAN_KULLANICI_ID,FATURA_REVIZYON,RISK_SKORU,RISKTEKI_TUTAR,SKORLAMA_JSON,OLUSTURAN_KULLANICI_ID) VALUES(:FaturaId,:StartedAt,:FinishedAt,:RuleCount,:BulguSayisi,:UserId,:Revision,:Score,:Tutar,:ScoringJson,:UserId) RETURNING ID INTO :NewId", new { FaturaId = context.Fatura.Id, StartedAt = context.UtcNow, FinishedAt = DateTime.UtcNow, result.RuleCount, BulguSayisi = result.Bulgular.Count, accessScope.UserId, context.Fatura.Revision, result.Risk.Score, Tutar = result.Risk.RisktekiTutar, ScoringJson = JsonSerializer.Serialize(scoring.Value) }, cancellationToken, transaction);
        foreach (var bulgu in result.Bulgular) { bulgu.KuralCalistirmaId = runId; bulgu.CreatedByUserId = accessScope.UserId; }
        await BulkAsync(connection, "INSERT INTO BULGU(KURAL_CALISTIRMA_ID,FATURA_ID,FATURA_KALEMI_ID,DONEM,KURAL_KODU,KURAL_VERSIYON,SEVERITY,AGIRLIK,MESAJ,GEREKCE,ONERILEN_AKSIYON,ETKILENEN_TUTAR,DURUM,OLUSTURAN_KULLANICI_ID) VALUES(:KuralCalistirmaId,:FaturaId,:FaturaKalemiId,:Donem,:KuralKodu,:KuralVersiyon,:Severity,:Agirlik,:Message,:Gerekce,:OnerilenAksiyon,:EtkilenenTutar,:Status,:CreatedByUserId)", result.Bulgular, cancellationToken, transaction);
        await connection.ExecuteAsync(CreateCommand("UPDATE FATURA SET SON_CALISTIRMA_ID=:RunId WHERE ID=:Id", new { RunId = runId, Id = context.Fatura.Id }, cancellationToken, transaction));
        await AppendAuditAsync(connection, accessScope, DenetimIslem.Update, "FATURA_KONTROL", context.Fatura.Id.ToString(), new { KuralCalistirmaId = runId, result.Risk, result.RuleCount, BulguSayisi = result.Bulgular.Count }, cancellationToken, transaction, context.Basvuru.KlinikKodu);
        transaction.Commit(); return Result<KontrolSonucu>.Success(result with { RunId = runId });
    }
    public async Task<IReadOnlyList<OzetSatiri>> ListKliniklerAsync(ErisimKapsami accessScope, CancellationToken cancellationToken)
    {
        await using var connection = await factory.OpenAsync(cancellationToken); return (await connection.QueryAsync<OzetSatiri>(CreateCommand("SELECT K.KOD AD,0 TUTAR,0 ADET FROM KLINIK K JOIN KULLANICI_KLINIK_YETKI Y ON Y.KURUM_ID=K.KURUM_ID AND Y.KLINIK_KODU=K.KOD WHERE Y.KULLANICI_ID=:UserId AND K.KURUM_ID=:KurumId ORDER BY K.KOD", accessScope, cancellationToken))).ToList();
    }
}
