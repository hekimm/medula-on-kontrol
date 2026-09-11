namespace MedulaOnKontrol.Domain.Entities;
public sealed class RedKaydi : Varlik
{
    public long FaturaId { get; set; }
    public long? FaturaKalemiId { get; set; }
    public string SgkRedKodu { get; set; } = "";
    public string RedAciklama { get; set; } = "";
    public decimal RedTutar { get; set; }
    public DateTime RedTarihi { get; set; }
    public string? EslesenKuralKodu { get; set; }
}
