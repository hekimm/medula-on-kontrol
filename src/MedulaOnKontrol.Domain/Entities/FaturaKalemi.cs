namespace MedulaOnKontrol.Domain.Entities;
public sealed class FaturaKalemi : Varlik
{
    public long FaturaId { get; set; }
    public int Donem { get; set; }
    public string SutIslemKodu { get; set; } = "";
    public DateTime IslemTarihi { get; set; }
    public decimal Adet { get; set; }
    public decimal BirimFiyat { get; set; }
    public decimal Tutar { get; set; }
    public string HekimKodu { get; set; } = "";
    public string KlinikKodu { get; set; } = "";
    public bool PaketKalemMi { get; set; }
    public KalemTip KalemTipi { get; set; }
    public string? Barkod { get; set; }
    public bool OdemeKosuluSaglandiMi { get; set; }
}
