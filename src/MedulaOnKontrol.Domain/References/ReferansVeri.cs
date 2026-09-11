namespace MedulaOnKontrol.Domain.References;
public sealed class ReferansVeri
{
    public List<SutIslem> Islemler { get; set; } = [];
    public List<Icd10> Tanilar { get; set; } = [];
    public List<IslemTaniMatris> Matris { get; set; } = [];
    public List<BirlikteFaturalanmaz> BirlikteFaturalanmaz { get; set; } = [];
    public List<PaketIcerik> Paketler { get; set; } = [];
    public List<IslemTekrarLimit> TekrarLimitleri { get; set; } = [];
    public List<BelgeZorunluluk> BelgeZorunluluklari { get; set; } = [];
    public List<IlacMalzeme> Ilaclar { get; set; } = [];
}
