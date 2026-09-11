using Dapper;
using static MedulaOnKontrol.Infrastructure.Persistence.OracleCommands;

namespace MedulaOnKontrol.Infrastructure.Persistence;

public sealed partial class MedulaRepository
{
    public async Task<Result<bool>> ClassifyAsync(ErisimKapsami scope, RedSiniflandirmaRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(request.KuralKodu) || request.KuralKodu.Length > 20 ||
            string.IsNullOrWhiteSpace(request.Gerekce) || request.Gerekce.Trim().Length is < 10 or > 700)
            return Result<bool>.Failure("Katalogdan bir kural seçin ve 10–700 karakter sınıflandırma gerekçesi girin.");

        await using var connection = await factory.OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        var fatura = await LockAsync(connection, transaction, scope, request.FaturaId, cancellationToken);
        if (fatura == null || fatura.Status != FaturaDurum.Reddedildi)
            return Result<bool>.Failure("Yetkili olduğunuz reddedilmiş bir fatura gereklidir.");
        var red = await connection.QuerySingleOrDefaultAsync<RedKaydi>(CreateCommand(
            "SELECT * FROM RED_KAYDI WHERE ID=:RedId AND FATURA_ID=:FaturaId", request, cancellationToken, transaction));
        if (red == null || red.EslesenKuralKodu != request.ExpectedKuralKodu || red.FaturaKalemiId != request.ExpectedKalemId)
            return Result<bool>.Failure("Red kaydı bulunamadı veya sınıflandırması değişti. Listeyi yenileyin.");
        if (await connection.ExecuteScalarAsync<int>(CreateCommand(
            "SELECT COUNT(*) FROM KURAL WHERE KURAL_KODU=:KuralKodu", request, cancellationToken, transaction)) == 0)
            return Result<bool>.Failure("Kural kodu katalogda bulunamadı.");
        if (request.KalemId.HasValue)
        {
            var kalem = await connection.QuerySingleOrDefaultAsync<FaturaKalemi>(CreateCommand(
                "SELECT * FROM FATURA_KALEMI WHERE ID=:KalemId AND FATURA_ID=:FaturaId", request, cancellationToken, transaction));
            if (kalem == null)
                return Result<bool>.Failure("Kalem bu faturaya ait değil.");
            var digerRedTutari = await connection.ExecuteScalarAsync<decimal>(CreateCommand(
                "SELECT NVL(SUM(RED_TUTAR),0) FROM RED_KAYDI WHERE FATURA_ID=:FaturaId AND FATURA_KALEMI_ID=:KalemId AND ID<>:RedId",
                request, cancellationToken, transaction));
            if (digerRedTutari + red.RedTutar > kalem.Tutar)
                return Result<bool>.Failure("Red tutarı seçilen kalemin tutarını aşıyor. Fatura genelinde sınıflandırın.");
        }
        await connection.ExecuteAsync(CreateCommand(
            "UPDATE RED_KAYDI SET FATURA_KALEMI_ID=:KalemId,ESLESEN_KURAL_KODU=:KuralKodu,GUNCELLEME_TARIHI=SYS_EXTRACT_UTC(SYSTIMESTAMP),GUNCELLEYEN_KULLANICI_ID=:UserId WHERE ID=:RedId AND FATURA_ID=:FaturaId",
            new { request.KalemId, request.KuralKodu, scope.UserId, request.RedId, request.FaturaId }, cancellationToken, transaction));
        await AppendAuditAsync(connection, scope, DenetimIslem.Update, "RED_SINIFLANDIRMA", red.Id.ToString(),
            new { request.KalemId, request.KuralKodu, Gerekce = request.Gerekce.Trim() }, cancellationToken, transaction,
            previousValue: new { KalemId = red.FaturaKalemiId, KuralKodu = red.EslesenKuralKodu });
        transaction.Commit();
        return Result<bool>.Success(true);
    }
}
