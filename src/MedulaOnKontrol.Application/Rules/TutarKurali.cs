using System.Text.Json;
using MedulaOnKontrol.Domain;

namespace MedulaOnKontrol.Application.Rules;
public sealed class TutarKurali : FaturaKuralBase
{
    public override string Category => "TUT";

    public override IEnumerable<Bulgu> Evaluate(FaturaDenetimBaglami context, Kural kural)
    {
        var tolerance = GetParameter(kural, "tolerans", 0.01m);
        if (kural.KuralKodu == KuralKodlari.Tut003 && Math.Abs(context.Fatura.ToplamTutar - context.Kalemler.Sum(faturaKalemi => faturaKalemi.Tutar)) > tolerance)
            yield return Bul(context, kural);
        foreach (var faturaKalemi in context.Kalemler)
        {
            var islem = context.Operation(faturaKalemi);
            if (kural.KuralKodu == KuralKodlari.Tut001 && islem != null && !faturaKalemi.PaketKalemMi && Math.Abs(faturaKalemi.Tutar - decimal.Round(faturaKalemi.Adet * islem.BirimFiyat, 2, MidpointRounding.AwayFromZero)) > tolerance)
                yield return Bul(context, kural, faturaKalemi);
            if (kural.KuralKodu == KuralKodlari.Tut002 && islem != null && !faturaKalemi.PaketKalemMi && Math.Abs(faturaKalemi.BirimFiyat - islem.BirimFiyat) > tolerance)
                yield return Bul(context, kural, faturaKalemi);
            if (kural.KuralKodu == KuralKodlari.Tut004 && (faturaKalemi.Tutar < 0 || faturaKalemi.BirimFiyat < 0))
                yield return Bul(context, kural, faturaKalemi);
        }
    }
}
