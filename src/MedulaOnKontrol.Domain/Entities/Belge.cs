namespace MedulaOnKontrol.Domain.Entities;
public sealed class Belge : Varlik
{
    public long BasvuruId { get; set; }
    public BelgeTip BelgeTip { get; set; }
    public DateTime BelgeTarihi { get; set; }
    public DateTime? GecerlilikBitis { get; set; }
    public ImzaDurum ImzaDurum { get; set; }
    public string DosyaReferansi { get; set; } = "";
}
