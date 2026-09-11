namespace MedulaOnKontrol.Domain.Enums;
public enum BasvuruTip
{
    [System.ComponentModel.DataAnnotations.Display(Name = "AYAKTAN")]
    Ayaktan = 0,
    [System.ComponentModel.DataAnnotations.Display(Name = "YATAN")]
    Yatan = 1,
    [System.ComponentModel.DataAnnotations.Display(Name = "ACIL")]
    Acil = 2,
    [System.ComponentModel.DataAnnotations.Display(Name = "GUNUBIRLIK")]
    Gunubirlik = 3
}
