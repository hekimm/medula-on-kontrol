using System.Text.Json;
using MedulaOnKontrol.Domain;

namespace MedulaOnKontrol.Application.Rules;
public sealed class SureKurali : FaturaKuralBase
{
    public override string Category => "SUR";

    public override IEnumerable<Bulgu> Evaluate(FaturaDenetimBaglami context, Kural kural)
    {
        if (kural.KuralKodu == KuralKodlari.Sur001 && (context.Fatura.GonderimTarihi ?? context.UtcNow) > context.Donem.KapanisTarihi.AddDays((int)GetParameter(kural, "ekGun", 0)))
            yield return Bul(context, kural);
        foreach (var faturaKalemi in context.Kalemler)
        {
            if (kural.KuralKodu == KuralKodlari.Sur002 && context.Donem.KapaliMi && faturaKalemi.CreatedAt > context.Donem.KapanisTarihi)
                yield return Bul(context, kural, faturaKalemi);
            if (kural.KuralKodu == KuralKodlari.Sur003 && faturaKalemi.IslemTarihi.Year * 100 + faturaKalemi.IslemTarihi.Month != context.Fatura.Donem)
                yield return Bul(context, kural, faturaKalemi);
            if (kural.KuralKodu == KuralKodlari.Sur004 && context.Basvuru.BasvuruTip == BasvuruTip.Yatan && context.Basvuru.CikisTarihi is { } cikis && faturaKalemi.IslemTarihi.Date > cikis.Date)
                yield return Bul(context, kural, faturaKalemi);
        }
    }
}
