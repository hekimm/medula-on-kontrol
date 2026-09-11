using Dapper;
using MedulaOnKontrol.Application;
using MedulaOnKontrol.Domain;
using Oracle.ManagedDataAccess.Client;
using static MedulaOnKontrol.Infrastructure.Persistence.OracleCommands;
namespace MedulaOnKontrol.Infrastructure.Persistence;

public sealed partial class MedulaRepository
{
    private async Task<Fatura?> LockAsync(OracleConnection connection, OracleTransaction transaction, ErisimKapsami accessScope, long id, CancellationToken cancellationToken)
    {
        var parameters = new { accessScope.UserId, accessScope.KurumId, Id = (long?)id, Donem = (int?)null };
        // Düzeltme diğer başvuruları da etkileyebilir; hasta kilidi fatura kilidinden önce alınır.
        await connection.QueryAsync<long>(CreateCommand("SELECT H.ID FROM HASTA H WHERE H.ID IN (SELECT B.HASTA_ID FROM FATURA F JOIN BASVURU B ON B.ID=F.BASVURU_ID WHERE F.ID=:Id AND " + AccessPredicate + ") FOR UPDATE", parameters, cancellationToken, transaction));
        return await connection.QuerySingleOrDefaultAsync<Fatura>(CreateCommand("SELECT F.* FROM FATURA F WHERE F.ID=:Id AND F.ID IN (" + SeciliFaturaSql + ") FOR UPDATE", parameters, cancellationToken, transaction));
    }
    public async Task<Result<bool>> CorrectAsync(ErisimKapsami accessScope, DuzeltmeRequest duzeltmeRequest, CancellationToken cancellationToken)
    {
        var valid = await new DuzeltmeRequestValidator().ValidateAsync(duzeltmeRequest, cancellationToken); if (!valid.IsValid) return Result<bool>.Failure(string.Join(" ", valid.Errors.Select(validationFailure => validationFailure.ErrorMessage)));
        await using var connection = await factory.OpenAsync(cancellationToken); using var transaction = connection.BeginTransaction(); var fatura = await LockAsync(connection, transaction, accessScope, duzeltmeRequest.FaturaId, cancellationToken);
        if (fatura == null || fatura.Revision != duzeltmeRequest.Revision || fatura.Status is FaturaDurum.Gonderildi or FaturaDurum.Reddedildi) return Result<bool>.Failure("Fatura değişmiş, gönderilmiş veya erişiminiz yok.");
        if (await connection.ExecuteScalarAsync<int>(CreateCommand("SELECT COUNT(*) FROM GONDERIM WHERE FATURA_ID=:Id AND DURUM='BEKLIYOR'", new { Id = fatura.Id }, cancellationToken, transaction)) > 0) return Result<bool>.Failure("Gönderimi devam eden fatura değiştirilemez.");
        var basvuru = await connection.QuerySingleAsync<Basvuru>(CreateCommand("SELECT * FROM BASVURU WHERE ID=:Id", new { Id = fatura.BasvuruId }, cancellationToken, transaction));
        if (await connection.ExecuteScalarAsync<int>(CreateCommand("SELECT COUNT(*) FROM GONDERIM G JOIN FATURA F ON F.ID=G.FATURA_ID JOIN BASVURU B ON B.ID=F.BASVURU_ID WHERE B.HASTA_ID=:HastaId AND B.KURUM_ID=:KurumId AND G.DURUM='BEKLIYOR'", new { basvuru.HastaId, accessScope.KurumId }, cancellationToken, transaction)) > 0) return Result<bool>.Failure("Hastanın ilişkili bir faturasının gönderimi sürüyor. Tamamlandıktan sonra düzeltin.");
        if (await connection.ExecuteScalarAsync<int>(CreateCommand("SELECT KAPALI_MI FROM DONEM WHERE DONEM_KODU=:Donem AND KURUM_ID=:KurumId", new { fatura.Donem, accessScope.KurumId }, cancellationToken, transaction)) == 1) return Result<bool>.Failure("Kapalı dönem değiştirilemez.");
        object? previousValue = null;
        switch (duzeltmeRequest.Kind)
        {
            case "line":
                previousValue = await connection.QuerySingleOrDefaultAsync<FaturaKalemi>(CreateCommand("SELECT * FROM FATURA_KALEMI WHERE ID=:Id AND FATURA_ID=:FaturaId", new { Id = duzeltmeRequest.KalemId, duzeltmeRequest.FaturaId }, cancellationToken, transaction));
                if (previousValue == null) return Result<bool>.Failure("Kalem bu faturaya ait değil.");
                await connection.ExecuteAsync(CreateCommand("UPDATE FATURA_KALEMI SET SUT_ISLEM_KODU=:Code,ADET=:Adet,BIRIM_FIYAT=:BirimFiyat,TUTAR=:Tutar,ISLEM_TARIHI=:KayitTarihi,PAKET_KALEM_MI=:Paket,ODEME_KOSULU_SAGLANDI_MI=:Kosul,GUNCELLEME_TARIHI=SYS_EXTRACT_UTC(SYSTIMESTAMP),GUNCELLEYEN_KULLANICI_ID=:UserId WHERE ID=:Id AND FATURA_ID=:FaturaId", new { duzeltmeRequest.Code, duzeltmeRequest.Adet, duzeltmeRequest.BirimFiyat, Tutar = duzeltmeRequest.PaketMi ? 0 : decimal.Round(duzeltmeRequest.Adet * duzeltmeRequest.BirimFiyat, 2, MidpointRounding.AwayFromZero), duzeltmeRequest.KayitTarihi, Paket = duzeltmeRequest.PaketMi ? 1 : 0, Kosul = duzeltmeRequest.IsConditionSatisfied ? 1 : 0, accessScope.UserId, Id = duzeltmeRequest.KalemId, duzeltmeRequest.FaturaId }, cancellationToken, transaction)); break;
            case "tani":
                previousValue = (await connection.QueryAsync<BasvuruTani>(CreateCommand("SELECT * FROM BASVURU_TANI WHERE BASVURU_ID=:Id", new { Id = fatura.BasvuruId }, cancellationToken, transaction))).ToList();
                await connection.ExecuteAsync(CreateCommand("UPDATE BASVURU_TANI SET TANI_TIPI=1,GUNCELLEME_TARIHI=SYS_EXTRACT_UTC(SYSTIMESTAMP),GUNCELLEYEN_KULLANICI_ID=:UserId WHERE BASVURU_ID=:Id AND TANI_TIPI=0", new { Id = fatura.BasvuruId, accessScope.UserId }, cancellationToken, transaction));
                await connection.ExecuteAsync(CreateCommand("INSERT INTO BASVURU_TANI(BASVURU_ID,ICD10_KOD,TANI_TIPI,TARIH,OLUSTURAN_KULLANICI_ID) VALUES(:Id,:Code,0,:KayitTarihi,:UserId)", new { Id = fatura.BasvuruId, duzeltmeRequest.Code, KayitTarihi = DateTime.UtcNow.Date, accessScope.UserId }, cancellationToken, transaction)); break;
            case "doc":
                await connection.ExecuteAsync(CreateCommand("INSERT INTO BELGE(BASVURU_ID,BELGE_TIPI,BELGE_TARIHI,GECERLILIK_BITIS,IMZA_DURUMU,DOSYA_REFERANSI,OLUSTURAN_KULLANICI_ID) VALUES(:Id,:BelgeTip,:KayitTarihi,:GecerlilikBitis,:Signature,:Dosya,:UserId)", new { Id = fatura.BasvuruId, duzeltmeRequest.BelgeTip, duzeltmeRequest.KayitTarihi, duzeltmeRequest.GecerlilikBitis, duzeltmeRequest.Signature, Dosya = "sentetik-belge:" + Guid.NewGuid().ToString("N"), accessScope.UserId }, cancellationToken, transaction)); break;
            case "authorization":
                previousValue = new { basvuru.ProvizyonNo }; await connection.ExecuteAsync(CreateCommand("UPDATE BASVURU SET PROVIZYON_NO=:Code,GUNCELLEME_TARIHI=SYS_EXTRACT_UTC(SYSTIMESTAMP),GUNCELLEYEN_KULLANICI_ID=:UserId WHERE ID=:Id", new { duzeltmeRequest.Code, accessScope.UserId, Id = basvuru.Id }, cancellationToken, transaction)); break;
            case "total": previousValue = new { fatura.ToplamTutar }; break;
        }
        await connection.ExecuteAsync(CreateCommand("UPDATE FATURA SET TOPLAM_TUTAR=(SELECT NVL(SUM(TUTAR),0) FROM FATURA_KALEMI WHERE FATURA_ID=:Id) WHERE ID=:Id", new { Id = fatura.Id }, cancellationToken, transaction));
        await connection.ExecuteAsync(CreateCommand("UPDATE FATURA SET REVIZYON=REVIZYON+1,KONTROL_REVIZYON=NULL,DURUM=2,GUNCELLEME_TARIHI=SYS_EXTRACT_UTC(SYSTIMESTAMP),GUNCELLEYEN_KULLANICI_ID=:UserId WHERE DURUM IN (0,1,2,3) AND BASVURU_ID IN (SELECT ID FROM BASVURU WHERE HASTA_ID=:HastaId AND KURUM_ID=:KurumId)", new { accessScope.UserId, basvuru.HastaId, accessScope.KurumId }, cancellationToken, transaction));
        await AppendAuditAsync(connection, accessScope, DenetimIslem.Update, "FATURA_DUZELTME", fatura.Id.ToString(), duzeltmeRequest, cancellationToken, transaction, basvuru.KlinikKodu, previousValue); transaction.Commit(); return Result<bool>.Success(true);
    }
    public async Task<Result<bool>> AddExceptionAsync(ErisimKapsami accessScope, IstisnaRequest istisnaRequest, CancellationToken cancellationToken)
    {
        if (istisnaRequest.Gerekce.Trim().Length < 10 || istisnaRequest.Gerekce.Length > 1000 || istisnaRequest.GecerlilikBitis <= DateTime.UtcNow || istisnaRequest.GecerlilikBitis > DateTime.UtcNow.AddDays(90)) return Result<bool>.Failure("En az 10 karakter gerekçe ve en fazla 90 gün geçerli bir bitiş tarihi girin.");
        await using var connection = await factory.OpenAsync(cancellationToken); using var transaction = connection.BeginTransaction(); var fatura = await LockAsync(connection, transaction, accessScope, istisnaRequest.FaturaId, cancellationToken);
        if (fatura == null || fatura.KontrolRevizyon != fatura.Revision || fatura.Status is FaturaDurum.Gonderildi or FaturaDurum.Reddedildi) return Result<bool>.Failure("Önce güncel faturayı kontrol edin.");
        if (await connection.ExecuteScalarAsync<int>(CreateCommand("SELECT COUNT(*) FROM GONDERIM WHERE FATURA_ID=:Id AND DURUM='BEKLIYOR'", new { Id = fatura.Id }, cancellationToken, transaction)) > 0) return Result<bool>.Failure("Gönderim sürerken istisna tanımlanamaz.");
        var bulgu = await connection.QuerySingleOrDefaultAsync<Bulgu>(CreateCommand("SELECT * FROM BULGU WHERE ID=:Id AND FATURA_ID=:FaturaId AND KURAL_CALISTIRMA_ID=:RunId", new { Id = istisnaRequest.BulguId, istisnaRequest.FaturaId, RunId = fatura.SonCalistirmaId }, cancellationToken, transaction));
        if (bulgu == null) return Result<bool>.Failure("Bulgu güncel kontrole ait değil.");
        await connection.ExecuteAsync(CreateCommand("INSERT INTO BULGU_ISTISNA(BULGU_ID,GEREKCE,ONAYLAYAN_KULLANICI_ID,GECERLILIK_BITIS,FATURA_REVIZYON,OLUSTURAN_KULLANICI_ID) VALUES(:BulguId,:Gerekce,:UserId,:GecerlilikBitis,:Revision,:UserId)", new { istisnaRequest.BulguId, istisnaRequest.Gerekce, accessScope.UserId, istisnaRequest.GecerlilikBitis, fatura.Revision }, cancellationToken, transaction));
        // İstisna sonraki kontrolde uygulanır; geçmiş bulgular değiştirilmez.
        await connection.ExecuteAsync(CreateCommand("UPDATE FATURA SET KONTROL_REVIZYON=NULL,DURUM=2 WHERE ID=:Id", new { Id = fatura.Id }, cancellationToken, transaction));
        await AppendAuditAsync(connection, accessScope, DenetimIslem.Create, "BULGU_ISTISNA", bulgu.Id.ToString(), istisnaRequest, cancellationToken, transaction); transaction.Commit(); return Result<bool>.Success(true);
    }
    private static async Task<bool> EngelliAsync(OracleConnection connection, OracleTransaction transaction, Fatura fatura, CancellationToken cancellationToken) => fatura.KontrolRevizyon != fatura.Revision || fatura.SonCalistirmaId == null || await connection.ExecuteScalarAsync<int>(CreateCommand("SELECT COUNT(*) FROM BULGU U WHERE U.KURAL_CALISTIRMA_ID=:RunId AND U.SEVERITY=0 AND (U.DURUM IN (0,3) OR (U.DURUM=2 AND NOT EXISTS(SELECT 1 FROM BULGU_ISTISNA I JOIN BULGU E ON E.ID=I.BULGU_ID WHERE E.FATURA_ID=U.FATURA_ID AND E.KURAL_KODU=U.KURAL_KODU AND E.KURAL_VERSIYON=U.KURAL_VERSIYON AND NVL(E.FATURA_KALEMI_ID,-1)=NVL(U.FATURA_KALEMI_ID,-1) AND I.FATURA_REVIZYON=:Revision AND I.GECERLILIK_BITIS>SYS_EXTRACT_UTC(SYSTIMESTAMP))))", new { RunId = fatura.SonCalistirmaId, fatura.Revision }, cancellationToken, transaction)) > 0;
    public async Task<Result<bool>> ApproveAsync(ErisimKapsami accessScope, long id, CancellationToken cancellationToken)
    {
        await using var connection = await factory.OpenAsync(cancellationToken); using var transaction = connection.BeginTransaction(); var fatura = await LockAsync(connection, transaction, accessScope, id, cancellationToken);
        if (fatura == null || fatura.Status != FaturaDurum.KontrolEdildi || await EngelliAsync(connection, transaction, fatura, cancellationToken)) return Result<bool>.Failure("Onay için güncel kontrol ve çözülmüş engelleyici bulgular gereklidir.");
        await connection.ExecuteAsync(CreateCommand("UPDATE FATURA SET DURUM=3,GUNCELLEME_TARIHI=SYS_EXTRACT_UTC(SYSTIMESTAMP),GUNCELLEYEN_KULLANICI_ID=:UserId WHERE ID=:Id", new { Id = id, accessScope.UserId }, cancellationToken, transaction));
        await AppendAuditAsync(connection, accessScope, DenetimIslem.Update, "FATURA_ONAY", id.ToString(), new { EImza = "SIMULATOR", fatura.Revision }, cancellationToken, transaction); transaction.Commit(); return Result<bool>.Success(true);
    }
    public async Task<IReadOnlyList<Kural>> ListAsync(CancellationToken cancellationToken) { await using var connection = await factory.OpenAsync(cancellationToken); return (await connection.QueryAsync<Kural>(CreateCommand("SELECT * FROM KURAL ORDER BY KURAL_KODU,VERSIYON DESC", null, cancellationToken))).ToList(); }
    public async Task<Result<bool>> VersionAsync(ErisimKapsami accessScope, KuralVersionRequest kuralVersionRequest, CancellationToken cancellationToken)
    {
        var valid = await new KuralVersionRequestValidator().ValidateAsync(kuralVersionRequest, cancellationToken); if (!valid.IsValid) return Result<bool>.Failure(string.Join(" ", valid.Errors.Select(validationFailure => validationFailure.ErrorMessage)));
        await using var connection = await factory.OpenAsync(cancellationToken); using var transaction = connection.BeginTransaction();
        await connection.QueryAsync<Kural>(CreateCommand("SELECT * FROM KURAL WHERE KURAL_KODU=:KuralKodu FOR UPDATE", kuralVersionRequest, cancellationToken, transaction));
        var old = await connection.QueryFirstOrDefaultAsync<Kural>(CreateCommand("SELECT * FROM KURAL WHERE KURAL_KODU=:KuralKodu ORDER BY VERSIYON DESC", kuralVersionRequest, cancellationToken, transaction));
        if (old == null || old.Version != kuralVersionRequest.ExpectedVersion || kuralVersionRequest.StartedAt <= old.YururlukBaslangic) return Result<bool>.Failure("Versiyon değişmiş veya başlangıç önceki versiyondan ileri değil.");
        // Kontrol edilmiş dönemin kuralları değişmez; geçmiş dönem denemeleri simülasyonda yapılır.
        if (await connection.ExecuteScalarAsync<int>(CreateCommand("SELECT COUNT(*) FROM KURAL_CALISTIRMA C JOIN FATURA F ON F.ID=C.FATURA_ID WHERE F.DONEM>=:Donem", new { Donem = kuralVersionRequest.StartedAt.Year * 100 + kuralVersionRequest.StartedAt.Month }, cancellationToken, transaction)) > 0) return Result<bool>.Failure("Kontrol edilmiş dönemlerin kuralları değiştirilemez; ileri dönem seçin veya simülasyon kullanın.");
        await connection.ExecuteAsync(CreateCommand("UPDATE KURAL SET YURURLUK_BITIS=:FinishedAt,GUNCELLEME_TARIHI=SYS_EXTRACT_UTC(SYSTIMESTAMP),GUNCELLEYEN_KULLANICI_ID=:UserId WHERE ID=:Id", new { FinishedAt = kuralVersionRequest.StartedAt.AddDays(-1), accessScope.UserId, old.Id }, cancellationToken, transaction));
        await connection.ExecuteAsync(CreateCommand("INSERT INTO KURAL(KURAL_KODU,AD,KATEGORI,ACIKLAMA,SEVERITY,AGIRLIK,PARAMETRE_JSON,VERSIYON,YURURLUK_BASLANGIC,AKTIF_MI,GEREKCE,ONERILEN_AKSIYON,OLUSTURAN_KULLANICI_ID) VALUES(:KuralKodu,:Name,:Category,:Description,:Severity,:Agirlik,:ParametersJson,:Version,:StartedAt,:Aktif,:Gerekce,:OnerilenAksiyon,:UserId)", new { kuralVersionRequest.KuralKodu, old.Name, old.Category, old.Description, kuralVersionRequest.Severity, kuralVersionRequest.Agirlik, kuralVersionRequest.ParametersJson, Version = old.Version + 1, kuralVersionRequest.StartedAt, Aktif = kuralVersionRequest.IsActive ? 1 : 0, old.Gerekce, old.OnerilenAksiyon, accessScope.UserId }, cancellationToken, transaction));
        await AppendAuditAsync(connection, accessScope, DenetimIslem.Create, "KURAL_VERSIYON", kuralVersionRequest.KuralKodu, kuralVersionRequest, cancellationToken, transaction, previousValue: old); transaction.Commit(); return Result<bool>.Success(true);
    }
}
