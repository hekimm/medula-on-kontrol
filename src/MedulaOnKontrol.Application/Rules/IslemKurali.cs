using System.Text.Json;
using MedulaOnKontrol.Domain;

namespace MedulaOnKontrol.Application.Rules;
public sealed class IslemKurali : FaturaKuralBase
{
    public override string Category => "ISL";

    public override IEnumerable<Bulgu> Evaluate(FaturaDenetimBaglami context, Kural kural)
    {
        foreach (var faturaKalemi in context.Kalemler)
        {
            var islem = context.Operation(faturaKalemi);
            if (kural.KuralKodu == KuralKodlari.Isl001 && context.Reference.BirlikteFaturalanmaz.Any(birlikteFaturalanmaz => (birlikteFaturalanmaz.SutIslemKoduA == faturaKalemi.SutIslemKodu || birlikteFaturalanmaz.SutIslemKoduB == faturaKalemi.SutIslemKodu) && context.GecmisKalemler.Any(gecmisKalem => gecmisKalem.Id != faturaKalemi.Id && gecmisKalem.HastaId == context.Hasta.Id && gecmisKalem.SutIslemKodu == (birlikteFaturalanmaz.SutIslemKoduA == faturaKalemi.SutIslemKodu ? birlikteFaturalanmaz.SutIslemKoduB : birlikteFaturalanmaz.SutIslemKoduA) && (birlikteFaturalanmaz.Scope == "AYNI_BASVURU" ? gecmisKalem.BasvuruId == context.Basvuru.Id : gecmisKalem.IslemTarihi.Date == faturaKalemi.IslemTarihi.Date))))
                yield return Bul(context, kural, faturaKalemi);
            if (kural.KuralKodu == KuralKodlari.Isl002 && !faturaKalemi.PaketKalemMi && faturaKalemi.Tutar > 0 && context.Reference.Paketler.Any(paketIcerik => paketIcerik.KapsananIslemKodu == faturaKalemi.SutIslemKodu && context.GecmisKalemler.Any(gecmisKalem => gecmisKalem.BasvuruId == context.Basvuru.Id && gecmisKalem.SutIslemKodu == paketIcerik.PaketKodu)))
                yield return Bul(context, kural, faturaKalemi);
            if (kural.KuralKodu == KuralKodlari.Isl003)
            {
                var limit = context.Reference.TekrarLimitleri.FirstOrDefault(islemTekrarLimit => islemTekrarLimit.SutIslemKodu == faturaKalemi.SutIslemKodu);
                if (limit != null)
                {
                    var days = (int)GetParameter(kural, "periyotGun", limit.PeriyotGun);
                    var quantity = GetParameter(kural, "azamiAdet", limit.AzamiAdet);
                    var total = context.GecmisKalemler.Where(gecmisKalem => gecmisKalem.HastaId == context.Hasta.Id && gecmisKalem.SutIslemKodu == faturaKalemi.SutIslemKodu && gecmisKalem.IslemTarihi.Date > faturaKalemi.IslemTarihi.Date.AddDays(-days) && (gecmisKalem.IslemTarihi.Date < faturaKalemi.IslemTarihi.Date || (gecmisKalem.IslemTarihi.Date == faturaKalemi.IslemTarihi.Date && gecmisKalem.Id <= faturaKalemi.Id))).Sum(gecmisKalem => gecmisKalem.Adet);
                    if (total > quantity)
                        yield return Bul(context, kural, faturaKalemi, $"{days} günlük aralıkta {total} adet kayıt vardır; üst sınır {quantity} adettir.");
                }
            }

            if (kural.KuralKodu == KuralKodlari.Isl004 && islem?.CinsiyetKisiti is { } cinsiyet && cinsiyet != context.Hasta.Cinsiyet)
                yield return Bul(context, kural, faturaKalemi);
            if (kural.KuralKodu == KuralKodlari.Isl005 && islem != null && (context.CalculateAge(faturaKalemi.IslemTarihi) < islem.YasMin || context.CalculateAge(faturaKalemi.IslemTarihi) > islem.YasMax))
                yield return Bul(context, kural, faturaKalemi);
            if (kural.KuralKodu == KuralKodlari.Isl006 && faturaKalemi.KalemTipi == KalemTip.Islem && islem == null)
                yield return Bul(context, kural, faturaKalemi);
            if (kural.KuralKodu == KuralKodlari.Isl007 && context.GecmisKalemler.Any(gecmisKalem => gecmisKalem.Id != faturaKalemi.Id && gecmisKalem.HastaId == context.Hasta.Id && gecmisKalem.SutIslemKodu == faturaKalemi.SutIslemKodu && gecmisKalem.IslemTarihi.Date == faturaKalemi.IslemTarihi.Date))
                yield return Bul(context, kural, faturaKalemi);
            if (kural.KuralKodu == KuralKodlari.Isl008 && (faturaKalemi.Adet <= 0 || faturaKalemi.Adet != decimal.Truncate(faturaKalemi.Adet)))
                yield return Bul(context, kural, faturaKalemi);
            if (kural.KuralKodu == KuralKodlari.Isl009 && faturaKalemi.IslemTarihi.Date > context.UtcNow.Date)
                yield return Bul(context, kural, faturaKalemi);
        }
    }
}
