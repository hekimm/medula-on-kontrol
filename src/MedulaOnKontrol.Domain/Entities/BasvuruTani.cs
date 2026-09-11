namespace MedulaOnKontrol.Domain.Entities;
public sealed class BasvuruTani : Varlik
{
    public long BasvuruId { get; set; }
    public string Icd10Kod { get; set; } = "";
    public TaniTip TaniTip { get; set; }
    public DateTime KayitTarihi { get; set; }
}
