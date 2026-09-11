using System.Text.Json;
using MedulaOnKontrol.Domain;

namespace MedulaOnKontrol.Application.Rules;
public sealed class BelgeKurali : FaturaKuralBase
{
    public override string Category => "BLG";

    public override IEnumerable<Bulgu> Evaluate(FaturaDenetimBaglami context, Kural kural)
    {
        if (kural.KuralKodu == KuralKodlari.Blg004 && context.Basvuru.BasvuruTip == BasvuruTip.Yatan && !context.Belgeler.Any(belge => belge.BelgeTip == BelgeTip.Epikriz))
            yield return Bul(context, kural);
        if (kural.KuralKodu == KuralKodlari.Blg006 && context.Belgeler.Any(belge => belge.BelgeTarihi.Date > context.UtcNow.Date))
            yield return Bul(context, kural);
        foreach (var faturaKalemi in context.Kalemler)
        {
            var requirement = context.Reference.BelgeZorunluluklari.Where(documentRequirement => documentRequirement.SutIslemKodu == faturaKalemi.SutIslemKodu).ToList();
            if (kural.KuralKodu == KuralKodlari.Blg001 && requirement.Any(documentRequirement => !BelgeVar(context, documentRequirement.BelgeTip, faturaKalemi.IslemTarihi)))
                yield return Bul(context, kural, faturaKalemi);
            if (kural.KuralKodu == KuralKodlari.Blg002 && requirement.Any(documentRequirement => documentRequirement.EImzaZorunluMu && BelgeVar(context, documentRequirement.BelgeTip, faturaKalemi.IslemTarihi) && !BelgeVar(context, documentRequirement.BelgeTip, faturaKalemi.IslemTarihi, true)))
                yield return Bul(context, kural, faturaKalemi);
            if (kural.KuralKodu == KuralKodlari.Blg003 && requirement.Any(documentRequirement => documentRequirement.BelgeTip == BelgeTip.SaglikKuruluRaporu) && context.Belgeler.Any(belge => belge.BelgeTip == BelgeTip.SaglikKuruluRaporu && belge.GecerlilikBitis < faturaKalemi.IslemTarihi.Date) && !BelgeVar(context, BelgeTip.SaglikKuruluRaporu, faturaKalemi.IslemTarihi))
                yield return Bul(context, kural, faturaKalemi);
            if (kural.KuralKodu == KuralKodlari.Blg005 && context.Operation(faturaKalemi)?.AmeliyatMi == true && (!BelgeVar(context, BelgeTip.AmeliyatNotu, faturaKalemi.IslemTarihi) || !BelgeVar(context, BelgeTip.AnesteziFormu, faturaKalemi.IslemTarihi)))
                yield return Bul(context, kural, faturaKalemi);
        }
    }
}
