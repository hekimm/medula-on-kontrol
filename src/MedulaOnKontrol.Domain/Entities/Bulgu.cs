namespace MedulaOnKontrol.Domain.Entities;
public sealed class Bulgu : Varlik
{
    public long KuralCalistirmaId { get; set; }
    public long FaturaId { get; set; }
    public long? FaturaKalemiId { get; set; }
    public int Donem { get; set; }
    public string KuralKodu { get; set; } = "";
    public int KuralVersiyon { get; set; }
    public Severity Severity { get; set; }
    public int Agirlik { get; set; }
    public string Message { get; set; } = "";
    public string Gerekce { get; set; } = "";
    public string OnerilenAksiyon { get; set; } = "";
    public decimal EtkilenenTutar { get; set; }
    public BulguDurum Status { get; set; }
}
