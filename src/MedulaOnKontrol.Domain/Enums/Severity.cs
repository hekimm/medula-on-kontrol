namespace MedulaOnKontrol.Domain.Enums;
public enum Severity
{
    [System.ComponentModel.DataAnnotations.Display(Name = "ENGELLEYICI")]
    Blocking = 0,
    [System.ComponentModel.DataAnnotations.Display(Name = "YUKSEK")]
    High = 1,
    [System.ComponentModel.DataAnnotations.Display(Name = "ORTA")]
    Medium = 2,
    [System.ComponentModel.DataAnnotations.Display(Name = "BILGI")]
    Information = 3
}
