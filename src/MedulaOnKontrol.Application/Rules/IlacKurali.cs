using System.Text.Json;
using MedulaOnKontrol.Domain;

namespace MedulaOnKontrol.Application.Rules;
public sealed class IlacKurali : FaturaKuralBase
{
    public override string Category => "ILC";

    public override IEnumerable<Bulgu> Evaluate(FaturaDenetimBaglami context, Kural kural)
    {
        foreach (var faturaKalemi in context.Kalemler.Where(selectedInvoiceLine => selectedInvoiceLine.KalemTipi != KalemTip.Islem))
        {
            var ilac = context.Reference.Ilaclar.FirstOrDefault(medicationSupply => medicationSupply.Barkod == faturaKalemi.Barkod);
            if (kural.KuralKodu == KuralKodlari.Ilc001 && ilac == null)
                yield return Bul(context, kural, faturaKalemi);
            if (kural.KuralKodu == KuralKodlari.Ilc002 && ilac?.RaporZorunluMu == true && !BelgeVar(context, BelgeTip.SaglikKuruluRaporu, faturaKalemi.IslemTarihi, true))
                yield return Bul(context, kural, faturaKalemi);
            if (kural.KuralKodu == KuralKodlari.Ilc003 && !string.IsNullOrWhiteSpace(ilac?.OdemeKisiti) && !faturaKalemi.OdemeKosuluSaglandiMi)
                yield return Bul(context, kural, faturaKalemi);
            if (kural.KuralKodu == KuralKodlari.Ilc004 && ilac != null && !FaturaDenetimBaglami.IsValidOn(ilac.GecerlilikBaslangic, ilac.GecerlilikBitis, faturaKalemi.IslemTarihi))
                yield return Bul(context, kural, faturaKalemi);
        }
    }
}
