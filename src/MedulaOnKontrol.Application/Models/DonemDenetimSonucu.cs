using System.Diagnostics;
using MedulaOnKontrol.Domain;

namespace MedulaOnKontrol.Application.Models;
public sealed record DonemDenetimSonucu(int FaturaSayisi, int LineCount, int BulguSayisi, int FailedCount)
{
    public IReadOnlyDictionary<string, double> StageDurationsSeconds { get; init; } = new Dictionary<string, double>();
}
