using Dapper;
using MedulaOnKontrol.Application;
using MedulaOnKontrol.Domain;
using static MedulaOnKontrol.Infrastructure.Persistence.OracleCommands;
namespace MedulaOnKontrol.Infrastructure.Persistence;
public sealed partial class MedulaRepository
{
    public async Task<Kullanici?> FindAsync(string username, CancellationToken cancellationToken) { await using var connection = await factory.OpenAsync(cancellationToken); var user = await connection.QuerySingleOrDefaultAsync<Kullanici>(CreateCommand("SELECT * FROM KULLANICI WHERE KULLANICI_ADI=:Name", new { Name = username }, cancellationToken)); return user == null ? null : await GetAsync(user.Id, cancellationToken); }
    public async Task<Kullanici?> GetAsync(long id, CancellationToken cancellationToken)
    {
        await using var connection = await factory.OpenAsync(cancellationToken); var user = await connection.QuerySingleOrDefaultAsync<Kullanici>(CreateCommand("SELECT * FROM KULLANICI WHERE ID=:Id", new { Id = id }, cancellationToken)); if (user == null) return null;
        user.Roller = (await connection.QueryAsync<string>(CreateCommand("SELECT R.AD FROM ROL R JOIN KULLANICI_ROL K ON K.ROL_ID=R.ID WHERE K.KULLANICI_ID=:Id", new { Id = id }, cancellationToken))).ToArray();
        user.Klinikler = (await connection.QueryAsync<string>(CreateCommand("SELECT KLINIK_KODU FROM KULLANICI_KLINIK_YETKI WHERE KULLANICI_ID=:Id AND KURUM_ID=:KurumId", new { Id = id, user.KurumId }, cancellationToken))).ToArray(); return user;
    }
    public async Task RecordLoginResultAsync(long id, bool isSuccessful, CancellationToken cancellationToken) { await using var connection = await factory.OpenAsync(cancellationToken); await connection.ExecuteAsync(CreateCommand("UPDATE KULLANICI SET BASARISIZ_GIRIS=CASE WHEN :IsSuccessful=1 THEN 0 ELSE BASARISIZ_GIRIS+1 END,KILIT_BITIS=CASE WHEN :IsSuccessful=1 THEN NULL WHEN BASARISIZ_GIRIS>=4 THEN SYS_EXTRACT_UTC(SYSTIMESTAMP)+NUMTODSINTERVAL(15,'MINUTE') ELSE KILIT_BITIS END WHERE ID=:Id", new { Id = id, IsSuccessful = isSuccessful ? 1 : 0 }, cancellationToken)); }
    public async Task AppendAsync(DenetimIzi auditEntry, CancellationToken cancellationToken) { await using var connection = await factory.OpenAsync(cancellationToken); await connection.ExecuteAsync(CreateCommand(InsertAuditSql, auditEntry, cancellationToken)); }
    public async Task<PagedResult<DenetimIzi>> ListAsync(ErisimKapsami accessScope, DenetimFilter filter, CancellationToken cancellationToken)
    {
        await using var connection = await factory.OpenAsync(cancellationToken);
        const string FilterPredicate = " WHERE I.KURUM_ID=:KurumId AND (I.KLINIK_KODU IS NULL OR EXISTS(SELECT 1 FROM KULLANICI_KLINIK_YETKI Y WHERE Y.KULLANICI_ID=:Viewer AND Y.KURUM_ID=I.KURUM_ID AND Y.KLINIK_KODU=I.KLINIK_KODU)) AND (:StartedAt IS NULL OR I.TARIH>=:StartedAt) AND (:FinishedAt IS NULL OR I.TARIH<:FinishedAt) AND (:UserId IS NULL OR I.KULLANICI_ID=:UserId) AND (:Operation IS NULL OR I.ISLEM_TIPI=:Operation) AND (:Varlik IS NULL OR I.VARLIK_ADI=:Varlik)";
        var parameters = new { accessScope.KurumId, Viewer = accessScope.UserId, StartedAt = filter.StartedAt?.Date.UtcSaatineCevir(), FinishedAt = filter.FinishedAt?.Date.AddDays(1).UtcSaatineCevir(), filter.UserId, filter.Operation, filter.Varlik, PageOffset = (Math.Max(1, filter.PageNumber) - 1) * 50 };
        var count = await connection.ExecuteScalarAsync<int>(CreateCommand("SELECT COUNT(*) FROM DENETIM_IZI I" + FilterPredicate, parameters, cancellationToken));
        var rows = (await connection.QueryAsync<DenetimIzi>(CreateCommand("SELECT I.* FROM DENETIM_IZI I" + FilterPredicate + " ORDER BY I.ID DESC OFFSET :PageOffset ROWS FETCH NEXT 50 ROWS ONLY", parameters, cancellationToken))).ToList();
        await AppendAuditAsync(connection, accessScope, DenetimIslem.View, "DENETIM_IZI", "LISTE", new { filter.PageNumber }, cancellationToken); return new(rows, count, Math.Max(1, filter.PageNumber), 50);
    }
    public async Task<long> EnqueueAsync(ErisimKapsami accessScope, int donem, CancellationToken cancellationToken)
    {
        await using var connection = await factory.OpenAsync(cancellationToken); using var transaction = connection.BeginTransaction();
        var id = await InsertIdAsync(connection, "INSERT INTO DENETIM_ISI(DONEM,KURUM_ID,KULLANICI_ID,OLUSTURAN_KULLANICI_ID) VALUES(:Donem,:KurumId,:UserId,:UserId) RETURNING ID INTO :NewId", new { Donem = donem, accessScope.KurumId, accessScope.UserId }, cancellationToken, transaction);
        await AppendAuditAsync(connection, accessScope, DenetimIslem.Create, "DENETIM_ISI", id.ToString(), new { Donem = donem }, cancellationToken, transaction); transaction.Commit(); return id;
    }
    public async Task<DenetimIsi?> ClaimAsync(CancellationToken cancellationToken)
    {
        await using var connection = await factory.OpenAsync(cancellationToken); using var transaction = connection.BeginTransaction();
        var row = await connection.QueryFirstOrDefaultAsync<DenetimIsi>(CreateCommand("SELECT * FROM DENETIM_ISI WHERE DURUM='BEKLIYOR' OR (DURUM='CALISIYOR' AND KIRA_BITIS<SYS_EXTRACT_UTC(SYSTIMESTAMP)) ORDER BY ID FOR UPDATE SKIP LOCKED", null, cancellationToken, transaction));
        if (row == null) return null;
        await connection.ExecuteAsync(CreateCommand("UPDATE DENETIM_ISI SET DURUM='CALISIYOR',BASLANGIC=NVL(BASLANGIC,SYS_EXTRACT_UTC(SYSTIMESTAMP)),KIRA_BITIS=SYS_EXTRACT_UTC(SYSTIMESTAMP)+NUMTODSINTERVAL(10,'MINUTE') WHERE ID=:Id", row, cancellationToken, transaction)); transaction.Commit(); return row;
    }
    public async Task<IReadOnlyList<long>> GetCandidatesAsync(DenetimIsi validationJob, CancellationToken cancellationToken)
    {
        await using var connection = await factory.OpenAsync(cancellationToken); await connection.ExecuteAsync(CreateCommand("BEGIN PKG_ON_KONTROL.ADAYLARI_HAZIRLA(:Id,:Donem,:UserId,:KurumId); END;", validationJob, cancellationToken));
        return (await connection.QueryAsync<long>(CreateCommand("SELECT FATURA_ID FROM DENETIM_ADAY WHERE IS_ID=:Id", validationJob, cancellationToken))).ToList();
    }
    public async Task ProgressAsync(long id, int total, int completedCount, int failedCount, string? error, bool isComplete, CancellationToken cancellationToken)
    {
        await using var connection = await factory.OpenAsync(cancellationToken); await connection.ExecuteAsync(CreateCommand("UPDATE DENETIM_ISI SET TOPLAM=:TotalCount,TAMAMLANAN=:CompletedCount,BASARISIZ=:FailedCount,HATA=:Error,DURUM=:Status,BITIS=CASE WHEN :IsComplete=1 THEN SYS_EXTRACT_UTC(SYSTIMESTAMP) ELSE NULL END,KIRA_BITIS=SYS_EXTRACT_UTC(SYSTIMESTAMP)+NUMTODSINTERVAL(10,'MINUTE') WHERE ID=:Id", new { Id = id, TotalCount = total, CompletedCount = completedCount, FailedCount = failedCount, Error = error, Status = isComplete ? (failedCount > 0 ? "HATALI" : "TAMAMLANDI") : "CALISIYOR", IsComplete = isComplete ? 1 : 0 }, cancellationToken));
    }
    public async Task<IReadOnlyList<DenetimIsi>> ListAsync(ErisimKapsami accessScope, CancellationToken cancellationToken) { await using var connection = await factory.OpenAsync(cancellationToken); return (await connection.QueryAsync<DenetimIsi>(CreateCommand("SELECT * FROM DENETIM_ISI WHERE KURUM_ID=:KurumId AND KULLANICI_ID=:UserId ORDER BY ID DESC FETCH FIRST 30 ROWS ONLY", accessScope, cancellationToken))).ToList(); }
    public async Task<Result<string>> PrepareAsync(ErisimKapsami accessScope, long id, CancellationToken cancellationToken)
    {
        await using var connection = await factory.OpenAsync(cancellationToken); using var transaction = connection.BeginTransaction(); var fatura = await LockAsync(connection, transaction, accessScope, id, cancellationToken);
        if (fatura == null || fatura.Status != FaturaDurum.Onaylandi || await EngelliAsync(connection, transaction, fatura, cancellationToken)) return Result<string>.Failure("Fatura güncel kontrolle onaylanmış olmalıdır.");
        var key = $"SIM-{fatura.Id}-{fatura.Revision}";
        var exists = await connection.QuerySingleOrDefaultAsync<string>(CreateCommand("SELECT DURUM FROM GONDERIM WHERE ANAHTAR=:SubmissionKey", new { SubmissionKey = key }, cancellationToken, transaction));
        if (exists == "BEKLIYOR" && await connection.ExecuteScalarAsync<int>(CreateCommand("SELECT COUNT(*) FROM GONDERIM WHERE ANAHTAR=:SubmissionKey AND BASLANGIC>SYS_EXTRACT_UTC(SYSTIMESTAMP)-NUMTODSINTERVAL(60,'SECOND')", new { SubmissionKey = key }, cancellationToken, transaction)) > 0) return Result<string>.Failure("Gönderim işleniyor. Sonucu bekleyin; kesinti durumunda bir dakika sonra aynı anahtarla yeniden deneyebilirsiniz.");
        if (exists is "KABUL" or "RED") return Result<string>.Failure("Bu revizyonun gönderimi zaten tamamlandı.");
        if (exists == null) await connection.ExecuteAsync(CreateCommand("INSERT INTO GONDERIM(FATURA_ID,FATURA_REVIZYON,KURAL_CALISTIRMA_ID,ANAHTAR,DURUM,BASLANGIC,OLUSTURAN_KULLANICI_ID) VALUES(:Id,:Revision,:RunId,:SubmissionKey,'BEKLIYOR',SYS_EXTRACT_UTC(SYSTIMESTAMP),:UserId)", new { Id = id, fatura.Revision, RunId = fatura.SonCalistirmaId, SubmissionKey = key, accessScope.UserId }, cancellationToken, transaction));
        else await connection.ExecuteAsync(CreateCommand("UPDATE GONDERIM SET DURUM='BEKLIYOR',BASLANGIC=SYS_EXTRACT_UTC(SYSTIMESTAMP) WHERE ANAHTAR=:SubmissionKey", new { SubmissionKey = key }, cancellationToken, transaction));
        await AppendAuditAsync(connection, accessScope, DenetimIslem.Create, "GONDERIM", id.ToString(), new { SubmissionKey = key }, cancellationToken, transaction); transaction.Commit(); return Result<string>.Success(key);
    }
    public async Task<Result<bool>> CompleteAsync(ErisimKapsami accessScope, long id, string submissionKey, MedulaYaniti result, CancellationToken cancellationToken)
    {
        await using var connection = await factory.OpenAsync(cancellationToken); using var transaction = connection.BeginTransaction(); var fatura = await LockAsync(connection, transaction, accessScope, id, cancellationToken); if (fatura == null) return Result<bool>.Failure("Gönderim kaydı erişilemez.");
        if (submissionKey != $"SIM-{fatura.Id}-{fatura.Revision}") return Result<bool>.Failure("Gönderim sonucu farklı fatura revizyonuna ait.");
        if (fatura.Status is FaturaDurum.Gonderildi or FaturaDurum.Reddedildi) return Result<bool>.Success(true);
        var count = await connection.ExecuteAsync(CreateCommand("UPDATE GONDERIM SET DURUM=:Status,SONUC=:Reference,RED_KODU=:RedKodu,BITIS=SYS_EXTRACT_UTC(SYSTIMESTAMP) WHERE ANAHTAR=:SubmissionKey AND FATURA_ID=:Id AND DURUM='BEKLIYOR'", new { Status = result.IsAccepted ? "KABUL" : "RED", result.Reference, result.RedKodu, SubmissionKey = submissionKey, Id = id }, cancellationToken, transaction)); if (count != 1) return Result<bool>.Failure("Gönderim anahtarı tutarsız.");
        await connection.ExecuteAsync(CreateCommand("UPDATE FATURA SET DURUM=:Status,GONDERIM_TARIHI=SYS_EXTRACT_UTC(SYSTIMESTAMP) WHERE ID=:Id", new { Status = result.IsAccepted ? 4 : 5, Id = id }, cancellationToken, transaction));
        if (!result.IsAccepted) await connection.ExecuteAsync(CreateCommand("INSERT INTO RED_KAYDI(FATURA_ID,SGK_RED_KODU,RED_ACIKLAMA,RED_TUTAR,RED_TARIHI,OLUSTURAN_KULLANICI_ID) VALUES(:Id,:Code,:Description,:Tutar,SYS_EXTRACT_UTC(SYSTIMESTAMP),:UserId)", new { Id = id, Code = result.RedKodu, Description = "MEDULA simülatörü tarafından üretilmiş temsili red; uzman sınıflandırması bekleniyor.", Tutar = fatura.ToplamTutar, accessScope.UserId }, cancellationToken, transaction));
        await AppendAuditAsync(connection, accessScope, DenetimIslem.Update, "GONDERIM_SONUC", id.ToString(), result, cancellationToken, transaction); transaction.Commit(); return Result<bool>.Success(true);
    }
}
