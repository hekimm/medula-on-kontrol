using Dapper;
using MedulaOnKontrol.Application;
using MedulaOnKontrol.Domain;
using static MedulaOnKontrol.Infrastructure.Persistence.OracleCommands;
namespace MedulaOnKontrol.Infrastructure.Persistence;

public sealed partial class MedulaRepository
{
    public async Task<GostergePaneli> GetSummaryAsync(ErisimKapsami accessScope, int donem, CancellationToken cancellationToken)
    {
        await using var connection = await factory.OpenAsync(cancellationToken); var parameters = new { accessScope.UserId, accessScope.KurumId, Donem = donem };
        var summary = await connection.QuerySingleAsync<GostergePaneli>(CreateCommand("SELECT COUNT(*) FATURA_SAYISI,NVL(SUM(F.TOPLAM_TUTAR),0) TOPLAM_TUTAR,NVL(SUM(F.RISKTEKI_TUTAR),0) RISKTEKI_TUTAR,NVL(SUM(CASE WHEN EXISTS (SELECT 1 FROM BULGU U WHERE U.KURAL_CALISTIRMA_ID=F.SON_CALISTIRMA_ID AND U.SEVERITY=0 AND U.DURUM IN(0,3)) THEN 1 ELSE 0 END),0) ENGELLEYICI_FATURA FROM FATURA F JOIN BASVURU B ON B.ID=F.BASVURU_ID WHERE " + AccessPredicate + " AND F.DONEM=:Donem", parameters, cancellationToken));
        summary.Klinikler = (await connection.QueryAsync<OzetSatiri>(CreateCommand("SELECT B.KLINIK_KODU AD,SUM(F.RISKTEKI_TUTAR) TUTAR,COUNT(*) ADET FROM FATURA F JOIN BASVURU B ON B.ID=F.BASVURU_ID WHERE " + AccessPredicate + " AND F.DONEM=:Donem GROUP BY B.KLINIK_KODU ORDER BY TUTAR DESC", parameters, cancellationToken))).ToList();
        summary.Bulgular = (await connection.QueryAsync<OzetSatiri>(CreateCommand("SELECT U.KURAL_KODU AD,SUM(U.ETKILENEN_TUTAR) TUTAR,COUNT(*) ADET FROM BULGU U JOIN FATURA F ON F.SON_CALISTIRMA_ID=U.KURAL_CALISTIRMA_ID JOIN BASVURU B ON B.ID=F.BASVURU_ID WHERE " + AccessPredicate + " AND F.DONEM=:Donem AND U.DURUM IN(0,3) GROUP BY U.KURAL_KODU ORDER BY ADET DESC FETCH FIRST 10 ROWS ONLY", parameters, cancellationToken))).ToList();
        summary.Trend = (await connection.QueryAsync<DonemTrend>(CreateCommand("SELECT F.DONEM,SUM(CASE WHEN F.DURUM IN(4,5) THEN 1 ELSE 0 END) GONDERILEN,SUM(CASE WHEN F.DURUM=5 THEN 1 ELSE 0 END) REDDEDILEN FROM FATURA F JOIN BASVURU B ON B.ID=F.BASVURU_ID WHERE " + AccessPredicate + " AND F.DONEM<=:Donem GROUP BY F.DONEM ORDER BY F.DONEM DESC FETCH FIRST 6 ROWS ONLY", parameters, cancellationToken))).Reverse().ToList();
        if (donem is >= 202001 and <= 210012 && donem % 100 is >= 1 and <= 12)
        {
            var endDate = new DateTime(donem / 100, donem % 100, 1);
            var existing = summary.Trend.ToDictionary(periodTrend => periodTrend.Donem);
            summary.Trend = Enumerable.Range(-5, 6).Select(i => endDate.AddMonths(i)).Select(date => date.Year * 100 + date.Month).Select(count => existing.GetValueOrDefault(count) ?? new DonemTrend(count, 0, 0)).ToList();
        }
        summary.OnlenenTahminiTutar = await connection.ExecuteScalarAsync<decimal>(CreateCommand("SELECT NVL(SUM(GREATEST(0,(SELECT MAX(C.RISKTEKI_TUTAR) KEEP(DENSE_RANK FIRST ORDER BY C.ID) FROM KURAL_CALISTIRMA C WHERE C.FATURA_ID=F.ID)-F.RISKTEKI_TUTAR)),0) FROM FATURA F JOIN BASVURU B ON B.ID=F.BASVURU_ID WHERE " + AccessPredicate + " AND F.DONEM=:Donem", parameters, cancellationToken));
        await AppendAuditAsync(connection, accessScope, DenetimIslem.View, "GOSTERGE_PANELI", donem.ToString(), null, cancellationToken); return summary;
    }
    public async Task<RaporVerisi> GetReportAsync(ErisimKapsami accessScope, int donem, CancellationToken cancellationToken)
    {
        var summary = await GetSummaryAsync(accessScope, donem, cancellationToken); await using var connection = await factory.OpenAsync(cancellationToken); var parameters = new { accessScope.UserId, accessScope.KurumId, Donem = donem };
        var faturalar = (await connection.QueryAsync<Fatura>(CreateCommand(FaturaSelectSql + " WHERE " + AccessPredicate + " AND F.DONEM=:Donem ORDER BY F.RISK_SKORU DESC,F.ID", parameters, cancellationToken))).ToList();
        var bulgular = (await connection.QueryAsync<Bulgu>(CreateCommand("SELECT U.* FROM BULGU U JOIN FATURA F ON F.SON_CALISTIRMA_ID=U.KURAL_CALISTIRMA_ID JOIN BASVURU B ON B.ID=F.BASVURU_ID WHERE " + AccessPredicate + " AND F.DONEM=:Donem ORDER BY U.FATURA_ID,U.SEVERITY", parameters, cancellationToken))).ToList();
        await AppendAuditAsync(connection, accessScope, DenetimIslem.Export, "DONEM_RAPORU", donem.ToString(), new { Fatura = faturalar.Count, Bulgu = bulgular.Count }, cancellationToken); return new(summary, faturalar, bulgular);
    }
    public async Task<Result<bool>> SaveAsync(ErisimKapsami accessScope, RedRequest redRequest, CancellationToken cancellationToken)
    {
        if (redRequest.Tutar <= 0 || redRequest.Description.Trim().Length < 10 || redRequest.Description.Length > 1000 || redRequest.SgkRedKodu.Length is < 1 or > 60 || redRequest.KayitTarihi > DateTime.UtcNow || redRequest.KayitTarihi < new DateTime(2020, 1, 1)) return Result<bool>.Failure("Red tutarı, açıklaması, kodu ve tarihi geçerli olmalıdır.");
        await using var connection = await factory.OpenAsync(cancellationToken); using var transaction = connection.BeginTransaction(); var fatura = await LockAsync(connection, transaction, accessScope, redRequest.FaturaId, cancellationToken);
        if (fatura == null || fatura.Status is not (FaturaDurum.Gonderildi or FaturaDurum.Reddedildi) || redRequest.KayitTarihi < fatura.GonderimTarihi) return Result<bool>.Failure("Gönderilmiş bir fatura ve gönderimden sonraki red tarihi gereklidir.");
        decimal maximumAmount = fatura.ToplamTutar;
        if (redRequest.KalemId.HasValue)
        {
            var faturaKalemi = await connection.QuerySingleOrDefaultAsync<FaturaKalemi>(CreateCommand("SELECT * FROM FATURA_KALEMI WHERE ID=:KalemId AND FATURA_ID=:FaturaId", redRequest, cancellationToken, transaction));
            if (faturaKalemi == null) return Result<bool>.Failure("Kalem bu faturaya ait değil."); maximumAmount = faturaKalemi.Tutar;
            var lineRejectedAmount = await connection.ExecuteScalarAsync<decimal>(CreateCommand("SELECT NVL(SUM(RED_TUTAR),0) FROM RED_KAYDI WHERE FATURA_ID=:FaturaId AND FATURA_KALEMI_ID=:KalemId", redRequest, cancellationToken, transaction));
            if (lineRejectedAmount + redRequest.Tutar > maximumAmount) return Result<bool>.Failure("Toplam red tutarı kalem tutarını aşamaz.");
        }
        var totalRejectedAmount = await connection.ExecuteScalarAsync<decimal>(CreateCommand("SELECT NVL(SUM(RED_TUTAR),0) FROM RED_KAYDI WHERE FATURA_ID=:FaturaId", redRequest, cancellationToken, transaction));
        if (redRequest.Tutar > maximumAmount || totalRejectedAmount + redRequest.Tutar > fatura.ToplamTutar) return Result<bool>.Failure("Toplam red tutarı fatura tutarını aşamaz.");
        if (!string.IsNullOrWhiteSpace(redRequest.KuralKodu) && await connection.ExecuteScalarAsync<int>(CreateCommand("SELECT COUNT(*) FROM KURAL WHERE KURAL_KODU=:KuralKodu", redRequest, cancellationToken, transaction)) == 0) return Result<bool>.Failure("Kural kodu katalogda bulunamadı.");
        await connection.ExecuteAsync(CreateCommand("INSERT INTO RED_KAYDI(FATURA_ID,FATURA_KALEMI_ID,SGK_RED_KODU,RED_ACIKLAMA,RED_TUTAR,RED_TARIHI,ESLESEN_KURAL_KODU,OLUSTURAN_KULLANICI_ID) VALUES(:FaturaId,:KalemId,:SgkRedKodu,:Description,:Tutar,:KayitTarihi,:KuralKodu,:UserId)", new { redRequest.FaturaId, redRequest.KalemId, redRequest.SgkRedKodu, redRequest.Description, redRequest.Tutar, redRequest.KayitTarihi, KuralKodu = string.IsNullOrWhiteSpace(redRequest.KuralKodu) ? null : redRequest.KuralKodu, accessScope.UserId }, cancellationToken, transaction));
        await connection.ExecuteAsync(CreateCommand("UPDATE FATURA SET DURUM=5 WHERE ID=:Id", new { Id = fatura.Id }, cancellationToken, transaction));
        await connection.ExecuteAsync(CreateCommand("UPDATE GONDERIM SET DURUM='RED' WHERE FATURA_ID=:Id AND FATURA_REVIZYON=:Revision", new { Id = fatura.Id, fatura.Revision }, cancellationToken, transaction));
        await AppendAuditAsync(connection, accessScope, DenetimIslem.Create, "RED_KAYDI", fatura.Id.ToString(), redRequest, cancellationToken, transaction); transaction.Commit(); return Result<bool>.Success(true);
    }
    public async Task<RedAnalizi> AnalyzeAsync(ErisimKapsami accessScope, int donem, CancellationToken cancellationToken)
    {
        await using var connection = await factory.OpenAsync(cancellationToken); var parameters = new { accessScope.UserId, accessScope.KurumId, Donem = donem };
        var redler = (await connection.QueryAsync<RedKaydi>(CreateCommand("SELECT R.* FROM RED_KAYDI R JOIN FATURA F ON F.ID=R.FATURA_ID JOIN BASVURU B ON B.ID=F.BASVURU_ID WHERE " + AccessPredicate + " AND F.DONEM=:Donem ORDER BY R.RED_TARIHI DESC", parameters, cancellationToken))).ToList();
        var predictions = (await connection.QueryAsync<Bulgu>(CreateCommand("SELECT U.* FROM BULGU U JOIN GONDERIM G ON G.KURAL_CALISTIRMA_ID=U.KURAL_CALISTIRMA_ID JOIN FATURA F ON F.ID=G.FATURA_ID JOIN BASVURU B ON B.ID=F.BASVURU_ID WHERE " + AccessPredicate + " AND F.DONEM=:Donem AND G.DURUM IN('KABUL','RED') AND U.OLUSTURMA_TARIHI<=G.BASLANGIC AND U.DURUM IN(0,3)", parameters, cancellationToken))).ToList();
        var calculatedMetrics = (await KuralMetrikService.CalculateAsync(predictions, redler, cancellationToken)).ToDictionary(kuralMetrik => kuralMetrik.KuralKodu);
        var codes = await connection.QueryAsync<string>(CreateCommand("SELECT DISTINCT KURAL_KODU FROM KURAL ORDER BY KURAL_KODU", null, cancellationToken));
        var metrics = codes.Select(kuralKodu => calculatedMetrics.GetValueOrDefault(kuralKodu) ?? new KuralMetrik { KuralKodu = kuralKodu }).ToList();
        var suggestions = redler.Where(red => !predictions.Any(bulgu => RedEslesmesi.Matches(bulgu, red))).GroupBy(red => red.SgkRedKodu).Select(group => new OzetSatiri(group.Key, group.Sum(red => red.RedTutar), group.Count())).OrderByDescending(summaryRow => summaryRow.Tutar).ToList();
        await AppendAuditAsync(connection, accessScope, DenetimIslem.View, "RED_ANALIZI", donem.ToString(), null, cancellationToken); return new(redler, metrics, suggestions);
    }
}
