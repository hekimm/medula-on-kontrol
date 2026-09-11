using System.Text.Json;
using MedulaOnKontrol.Domain;

namespace MedulaOnKontrol.Application.Rules;
public abstract class FaturaKuralBase : IFaturaKurali
{
    public abstract string Category { get; }

    public abstract IEnumerable<Bulgu> Evaluate(FaturaDenetimBaglami context, Kural kural);
    protected static Bulgu Bul(FaturaDenetimBaglami context, Kural kural, FaturaKalemi? faturaKalemi = null, string? detail = null) => new()
    {
        FaturaId = context.Fatura.Id,
        FaturaKalemiId = faturaKalemi?.Id,
        Donem = context.Fatura.Donem,
        KuralKodu = kural.KuralKodu,
        KuralVersiyon = kural.Version,
        Severity = kural.Severity,
        Agirlik = kural.Agirlik,
        Message = kural.Name,
        Gerekce = $"{context.Fatura.FaturaNo} numaralı faturanın {(faturaKalemi == null ? "başvuru kaydı" : $"{faturaKalemi.Id} numaralı kalemi")}, {kural.KuralKodu} v{kural.Version} temsili kuralına göre tutarsızdır. {kural.Gerekce} {detail}".Trim(),
        OnerilenAksiyon = kural.OnerilenAksiyon,
        EtkilenenTutar = Math.Max(0, faturaKalemi?.Tutar ?? context.Fatura.ToplamTutar)
    };
    protected static decimal GetParameter(Kural kural, string name, decimal defaultValue)
    {
        using var doc = JsonDocument.Parse(kural.ParametersJson);
        return doc.RootElement.TryGetProperty(name, out var jsonElement) && jsonElement.TryGetDecimal(out var amount) ? amount : defaultValue;
    }

    protected static bool BelgeVar(FaturaDenetimBaglami context, BelgeTip type, DateTime date, bool signatureVerifier = false) => context.Belgeler.Any(belge => belge.BelgeTip == type && FaturaDenetimBaglami.IsValidOn(belge.BelgeTarihi, belge.GecerlilikBitis, date) && (!signatureVerifier || belge.ImzaDurum == ImzaDurum.EImzali));
}
