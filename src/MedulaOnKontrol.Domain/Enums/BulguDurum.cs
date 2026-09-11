namespace MedulaOnKontrol.Domain.Enums;
public enum BulguDurum
{
    [System.ComponentModel.DataAnnotations.Display(Name = "ACIK")]
    Acik = 0,
    [System.ComponentModel.DataAnnotations.Display(Name = "DUZELTILDI")]
    Duzeltildi = 1,
    [System.ComponentModel.DataAnnotations.Display(Name = "ISTISNA TANIMLANDI")]
    IstisnaTanimlandi = 2,
    [System.ComponentModel.DataAnnotations.Display(Name = "KABUL EDILDI")]
    KabulEdildi = 3
}
