namespace MedulaOnKontrol.Domain.Entities;
public sealed class DenetimIzi : Varlik
{
    public DateTime KayitTarihi { get; set; }
    public long UserId { get; set; }
    public long KurumId { get; set; }
    public string? KlinikKodu { get; set; }
    public DenetimIslem OperationType { get; set; }
    public string EntityName { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string? PreviousValueJson { get; set; }
    public string? NewValueJson { get; set; }
    public string IpAddress { get; set; } = "";
}
