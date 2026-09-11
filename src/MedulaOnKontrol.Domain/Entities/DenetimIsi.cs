namespace MedulaOnKontrol.Domain.Entities;
public sealed class DenetimIsi : Varlik
{
    public int Donem { get; set; }
    public long KurumId { get; set; }
    public long UserId { get; set; }
    public string Status { get; set; } = "BEKLIYOR";
    public int TotalCount { get; set; }
    public int CompletedCount { get; set; }
    public int FailedCount { get; set; }
    public string? Error { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}
