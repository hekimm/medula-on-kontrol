using System.Text.Json;
using MedulaOnKontrol.Domain;

namespace MedulaOnKontrol.Application.Rules;
public sealed class TaniKurali : FaturaKuralBase
{
    public override string Category => "TANI";

    public override IEnumerable<Bulgu> Evaluate(FaturaDenetimBaglami context, Kural kural)
    {
        if (kural.KuralKodu == KuralKodlari.Tani001 && context.Tanilar.All(basvuruTani => basvuruTani.TaniTip != TaniTip.Ana))
            yield return Bul(context, kural);
        if (kural.KuralKodu == KuralKodlari.Tani005 && context.Tanilar.Count(basvuruTani => basvuruTani.TaniTip == TaniTip.Ana) > 1)
            yield return Bul(context, kural);
        if (kural.KuralKodu == KuralKodlari.Tani006 && context.Tanilar.Any(basvuruTani => context.Reference.Tanilar.All(icd10Tani => icd10Tani.Code != basvuruTani.Icd10Kod)))
            yield return Bul(context, kural);
        foreach (var faturaKalemi in context.Kalemler)
        {
            var tanilar = context.Tanilar.Select(basvuruTani => context.Reference.Tanilar.FirstOrDefault(icd10Tani => icd10Tani.Code == basvuruTani.Icd10Kod && FaturaDenetimBaglami.IsValidOn(icd10Tani.GecerlilikBaslangic, icd10Tani.GecerlilikBitis, faturaKalemi.IslemTarihi))).ToList();
            if (kural.KuralKodu == KuralKodlari.Tani002 && tanilar.Any(icd10Tani => icd10Tani == null))
                yield return Bul(context, kural, faturaKalemi);
            if (kural.KuralKodu == KuralKodlari.Tani003 && tanilar.Any(icd10Tani => icd10Tani?.CinsiyetKisiti != null && icd10Tani.CinsiyetKisiti != context.Hasta.Cinsiyet))
                yield return Bul(context, kural, faturaKalemi);
            if (kural.KuralKodu == KuralKodlari.Tani004 && tanilar.Any(icd10Tani => icd10Tani != null && (context.CalculateAge(faturaKalemi.IslemTarihi) < icd10Tani.YasMin || context.CalculateAge(faturaKalemi.IslemTarihi) > icd10Tani.YasMax)))
                yield return Bul(context, kural, faturaKalemi);
        }
    }
}
