namespace MedulaOnKontrol.Application.Models;

public static class RedEslesmesi
{
    public static bool Matches(Bulgu bulgu, RedKaydi red) => bulgu.FaturaId == red.FaturaId && bulgu.KuralKodu == red.EslesenKuralKodu && (bulgu.FaturaKalemiId == null || bulgu.FaturaKalemiId == red.FaturaKalemiId) && bulgu.CreatedAt <= red.RedTarihi;
}
