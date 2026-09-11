namespace MedulaOnKontrol.Application.Models;

public static class KurumTakvimi
{
    public static bool IsValidPeriod(int donem) => donem is >= 202001 and <= 210012 && donem % 100 is >= 1 and <= 12;

    private static readonly TimeZoneInfo _saatDilimi = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public static DateTime TurkiyeSaatineCevir(this DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _saatDilimi);

    public static DateTime UtcSaatineCevir(this DateTime yerel) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(yerel, DateTimeKind.Unspecified), _saatDilimi);
}
