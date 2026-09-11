namespace MedulaOnKontrol.Domain.Entities;
public sealed class Kural : Varlik
{
    public string KuralKodu { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public Severity Severity { get; set; }
    public int Agirlik { get; set; }
    public string ParametersJson { get; set; } = "{}";
    public int Version { get; set; } = 1;
    public DateTime YururlukBaslangic { get; set; }
    public DateTime? YururlukBitis { get; set; }
    public bool IsActive { get; set; } = true;
    public string Gerekce { get; set; } = "";
    public string OnerilenAksiyon { get; set; } = "";
}
