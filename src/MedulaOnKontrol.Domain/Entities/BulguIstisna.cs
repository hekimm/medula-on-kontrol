namespace MedulaOnKontrol.Domain.Entities;
public sealed class BulguIstisna : Varlik
{
    public long BulguId { get; set; }
    public string Gerekce { get; set; } = "";
    public long OnaylayanKullaniciId { get; set; }
    public DateTime GecerlilikBitis { get; set; }
    public string KuralKodu { get; set; } = "";
    public int KuralVersiyon { get; set; }
    public long? FaturaKalemiId { get; set; }
    public long FaturaRevizyon { get; set; }
}
