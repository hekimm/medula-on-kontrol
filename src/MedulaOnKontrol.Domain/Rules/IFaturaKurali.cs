namespace MedulaOnKontrol.Domain.Rules;
public interface IFaturaKurali
{
    string Category { get; }

    IEnumerable<Bulgu> Evaluate(FaturaDenetimBaglami context, Kural kural);
}
