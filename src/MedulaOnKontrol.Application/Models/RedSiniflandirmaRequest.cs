namespace MedulaOnKontrol.Application.Models;

public sealed class RedSiniflandirmaRequest
{
    public long RedId { get; set; }
    public long FaturaId { get; set; }
    public long? KalemId { get; set; }
    public long? ExpectedKalemId { get; set; }
    public string? ExpectedKuralKodu { get; set; }
    public string KuralKodu { get; set; } = "";
    public string Gerekce { get; set; } = "";
}
