namespace MedulaOnKontrol.Domain.Entities;
public sealed class Donem : Varlik
{
    public int DonemKodu { get; set; }
    public long KurumId { get; set; }
    public DateTime KapanisTarihi { get; set; }
    public bool KapaliMi { get; set; }
}
