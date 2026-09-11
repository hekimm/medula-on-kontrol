using System.Text.Json;
using MedulaOnKontrol.Domain;

namespace MedulaOnKontrol.Application.Rules;
public sealed class IslemTaniKurali : FaturaKuralBase
{
    public override string Category => "ITU";

    public override IEnumerable<Bulgu> Evaluate(FaturaDenetimBaglami context, Kural kural)
    {
        foreach (var faturaKalemi in context.Kalemler.Where(selectedInvoiceLine => selectedInvoiceLine.KalemTipi == KalemTip.Islem))
        {
            var matris = context.Reference.Matris.Where(procedureDiagnosisMapping => procedureDiagnosisMapping.SutIslemKodu == faturaKalemi.SutIslemKodu && procedureDiagnosisMapping.ZorunluMu).ToList();
            if (kural.KuralKodu == KuralKodlari.Itu001 && matris.Count > 0 && !matris.Any(procedureDiagnosisMapping => context.Tanilar.Any(basvuruTani => basvuruTani.Icd10Kod.StartsWith(procedureDiagnosisMapping.Icd10KodOnegi, StringComparison.Ordinal))))
                yield return Bul(context, kural, faturaKalemi);
            if (kural.KuralKodu == KuralKodlari.Itu002 && context.Operation(faturaKalemi)?.BasvuruTipiKisiti is { } type && type != context.Basvuru.BasvuruTip)
                yield return Bul(context, kural, faturaKalemi);
        }
    }
}
