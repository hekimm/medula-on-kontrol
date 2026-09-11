namespace MedulaOnKontrol.Domain.Models;
public sealed class FaturaDenetimBaglami
{
    public required Fatura Fatura { get; init; }
    public required Basvuru Basvuru { get; init; }
    public required Hasta Hasta { get; init; }
    public required Donem Donem { get; init; }
    public List<FaturaKalemi> Kalemler { get; init; } = [];
    public List<BasvuruTani> Tanilar { get; init; } = [];
    public List<Belge> Belgeler { get; init; } = [];
    public required ReferansVeri Reference { get; init; }
    public List<GecmisKalem> GecmisKalemler { get; init; } = [];
    public List<BulguIstisna> Exceptions { get; init; } = [];
    public int AyniTakipFaturaSayisi { get; init; }
    public DateTime UtcNow { get; init; } = DateTime.UtcNow;
    public DateTime DonemBaslangic => new(Fatura.Donem / 100, Fatura.Donem % 100, 1);

    public SutIslem? Operation(FaturaKalemi faturaKalemi) => Reference.Islemler.FirstOrDefault(islem => islem.Code == faturaKalemi.SutIslemKodu && IsValidOn(islem.GecerlilikBaslangic, islem.GecerlilikBitis, faturaKalemi.IslemTarihi));
    public static bool IsValidOn(DateTime startDate, DateTime? endDate, DateTime date) => startDate.Date <= date.Date && (endDate == null || date.Date <= endDate.Value.Date);
    public int CalculateAge(DateTime date)
    {
        var age = date.Year - Hasta.DogumTarihi.Year;
        return Hasta.DogumTarihi.Date > date.AddYears(-age).Date ? age - 1 : age;
    }
}
