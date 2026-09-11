namespace MedulaOnKontrol.Application.Models;

public sealed class KalibrasyonRequest
{
    public int Donem { get; set; }
    public string KuralKodu { get; set; } = "";
    public int ExpectedVersion { get; set; }
    public DateTime YururlukBaslangic { get; set; }
    public string Gerekce { get; set; } = "";
}
