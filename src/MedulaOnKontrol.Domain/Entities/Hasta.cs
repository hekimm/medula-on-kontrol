namespace MedulaOnKontrol.Domain.Entities;
public sealed class Hasta : Varlik
{
    public string HastaNo { get; set; } = "";
    public string AdSoyad { get; set; } = "";
    public string KimlikNo { get; set; } = "";
    public DateTime DogumTarihi { get; set; }
    public Cinsiyet Cinsiyet { get; set; }
    public bool AnonimMi { get; set; }
}
