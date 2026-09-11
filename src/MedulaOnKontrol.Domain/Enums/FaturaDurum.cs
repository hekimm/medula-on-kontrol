namespace MedulaOnKontrol.Domain.Enums;
public enum FaturaDurum
{
    [System.ComponentModel.DataAnnotations.Display(Name = "TASLAK")]
    Taslak = 0,
    [System.ComponentModel.DataAnnotations.Display(Name = "KONTROL EDILDI")]
    KontrolEdildi = 1,
    [System.ComponentModel.DataAnnotations.Display(Name = "DUZELTILIYOR")]
    Duzeltiliyor = 2,
    [System.ComponentModel.DataAnnotations.Display(Name = "ONAYLANDI")]
    Onaylandi = 3,
    [System.ComponentModel.DataAnnotations.Display(Name = "GONDERILDI")]
    Gonderildi = 4,
    [System.ComponentModel.DataAnnotations.Display(Name = "REDDEDILDI")]
    Reddedildi = 5
}
