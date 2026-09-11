namespace MedulaOnKontrol.Domain.Entities;
public sealed class Basvuru : Varlik
{
    public long HastaId { get; set; }
    public long KurumId { get; set; }
    public string TakipNo { get; set; } = "";
    public string? ProvizyonNo { get; set; }
    public DateTime ProvizyonTarihi { get; set; }
    public BasvuruTip BasvuruTip { get; set; }
    public string KlinikKodu { get; set; } = "";
    public DateTime? YatisTarihi { get; set; }
    public DateTime? CikisTarihi { get; set; }
    public string HekimKodu { get; set; } = "";
}
