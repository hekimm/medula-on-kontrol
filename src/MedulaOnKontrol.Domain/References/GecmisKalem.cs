namespace MedulaOnKontrol.Domain.References;
public sealed class GecmisKalem
{
    public long Id { get; set; }
    public long FaturaId { get; set; }
    public long HastaId { get; set; }
    public long BasvuruId { get; set; }
    public int Donem { get; set; }
    public string TakipNo { get; set; } = "";
    public string SutIslemKodu { get; set; } = "";
    public DateTime IslemTarihi { get; set; }
    public decimal Adet { get; set; }
}
