namespace MedulaOnKontrol.Domain.Enums;
public enum DenetimIslem
{
    [System.ComponentModel.DataAnnotations.Display(Name = "GORUNTULEME")]
    View = 0,
    [System.ComponentModel.DataAnnotations.Display(Name = "OLUSTURMA")]
    Create = 1,
    [System.ComponentModel.DataAnnotations.Display(Name = "GUNCELLEME")]
    Update = 2,
    [System.ComponentModel.DataAnnotations.Display(Name = "SILME")]
    Delete = 3,
    [System.ComponentModel.DataAnnotations.Display(Name = "DISA AKTARMA")]
    Export = 4
}
