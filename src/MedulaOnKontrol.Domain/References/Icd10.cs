namespace MedulaOnKontrol.Domain.References;
public sealed class Icd10 : Varlik
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public Cinsiyet? CinsiyetKisiti { get; set; }
    public int? YasMin { get; set; }
    public int? YasMax { get; set; }
    public DateTime GecerlilikBaslangic { get; set; }
    public DateTime? GecerlilikBitis { get; set; }
}
