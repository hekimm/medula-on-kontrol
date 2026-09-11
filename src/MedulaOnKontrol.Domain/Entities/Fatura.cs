namespace MedulaOnKontrol.Domain.Entities;
public sealed class Fatura : Varlik
{
    public int Donem { get; set; }
    public long BasvuruId { get; set; }
    public string FaturaNo { get; set; } = "";
    public FaturaDurum Status { get; set; }
    public decimal ToplamTutar { get; set; }
    public decimal RiskSkoru { get; set; }
    public decimal RisktekiTutar { get; set; }
    public DateTime? SonKontrolTarihi { get; set; }
    public long Revision { get; set; }
    public long? KontrolRevizyon { get; set; }
    public long? SonCalistirmaId { get; set; }
    public DateTime? GonderimTarihi { get; set; }
    public string KlinikKodu { get; set; } = "";
    public BasvuruTip BasvuruTip { get; set; }
    public string HastaNo { get; set; } = "";
    public int EngelleyiciSayisi { get; set; }
}
