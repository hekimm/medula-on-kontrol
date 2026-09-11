namespace MedulaOnKontrol.Domain.References;
public sealed class SutIslem : Varlik
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Puan { get; set; }
    public decimal BirimFiyat { get; set; }
    public DateTime GecerlilikBaslangic { get; set; }
    public DateTime? GecerlilikBitis { get; set; }
    public Cinsiyet? CinsiyetKisiti { get; set; }
    public int? YasMin { get; set; }
    public int? YasMax { get; set; }
    public bool PaketMi { get; set; }
    public bool AmeliyatMi { get; set; }
    public BasvuruTip? BasvuruTipiKisiti { get; set; }
}
