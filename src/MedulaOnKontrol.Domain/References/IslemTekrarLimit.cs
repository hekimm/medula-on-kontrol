namespace MedulaOnKontrol.Domain.References;
public sealed class IslemTekrarLimit
{
    public string SutIslemKodu { get; set; } = "";
    public int PeriyotGun { get; set; }
    public int AzamiAdet { get; set; }
}
