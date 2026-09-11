namespace MedulaOnKontrol.Domain.References;
public sealed class IlacMalzeme
{
    public string Barkod { get; set; } = "";
    public string Name { get; set; } = "";
    public string? OdemeKisiti { get; set; }
    public bool RaporZorunluMu { get; set; }
    public DateTime GecerlilikBaslangic { get; set; }
    public DateTime? GecerlilikBitis { get; set; }
}
