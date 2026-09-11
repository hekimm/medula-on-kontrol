namespace MedulaOnKontrol.Domain.Models;
public sealed record KontrolSonucu(long RunId, int RuleCount, IReadOnlyList<Bulgu> Bulgular, RiskSonucu Risk);
