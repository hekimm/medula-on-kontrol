namespace MedulaOnKontrol.Domain.References;
public sealed class BelgeZorunluluk
{
    public string SutIslemKodu { get; set; } = "";
    public BelgeTip BelgeTip { get; set; }
    public bool EImzaZorunluMu { get; set; }
}
