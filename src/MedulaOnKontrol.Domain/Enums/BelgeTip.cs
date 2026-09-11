namespace MedulaOnKontrol.Domain.Enums;
public enum BelgeTip
{
    [System.ComponentModel.DataAnnotations.Display(Name = "EPIKRIZ")]
    Epikriz = 0,
    [System.ComponentModel.DataAnnotations.Display(Name = "AMELIYAT NOTU")]
    AmeliyatNotu = 1,
    [System.ComponentModel.DataAnnotations.Display(Name = "PATOLOJI RAPORU")]
    PatolojiRaporu = 2,
    [System.ComponentModel.DataAnnotations.Display(Name = "ANESTEZI FORMU")]
    AnesteziFormu = 3,
    [System.ComponentModel.DataAnnotations.Display(Name = "ONAM FORMU")]
    OnamFormu = 4,
    [System.ComponentModel.DataAnnotations.Display(Name = "SAGLIK KURULU RAPORU")]
    SaglikKuruluRaporu = 5,
    [System.ComponentModel.DataAnnotations.Display(Name = "GORUNTULEME RAPORU")]
    GoruntulemeRaporu = 6
}
