using System.Text.Json;
using MedulaOnKontrol.Domain;

namespace MedulaOnKontrol.Application.Rules;
public sealed class ProvizyonKurali : FaturaKuralBase
{
    public override string Category => "PRV";

    public override IEnumerable<Bulgu> Evaluate(FaturaDenetimBaglami context, Kural kural)
    {
        if (kural.KuralKodu == KuralKodlari.Prv001 && string.IsNullOrWhiteSpace(context.Basvuru.ProvizyonNo))
            yield return Bul(context, kural);
        if (kural.KuralKodu == KuralKodlari.Prv002)
            foreach (var faturaKalemi in context.Kalemler.Where(selectedInvoiceLine => selectedInvoiceLine.IslemTarihi.Date < context.Basvuru.ProvizyonTarihi.Date))
                yield return Bul(context, kural, faturaKalemi);
        if (kural.KuralKodu == KuralKodlari.Prv003 && context.Basvuru.BasvuruTip == BasvuruTip.Yatan && context.Basvuru.CikisTarihi < context.Basvuru.YatisTarihi)
            yield return Bul(context, kural);
        if (kural.KuralKodu == KuralKodlari.Prv004 && context.AyniTakipFaturaSayisi > 1)
            yield return Bul(context, kural);
        if (kural.KuralKodu == KuralKodlari.Prv005 && string.IsNullOrWhiteSpace(context.Basvuru.TakipNo))
            yield return Bul(context, kural);
    }
}
