using FluentAssertions;
using Xunit;

namespace MedulaOnKontrol.UnitTests;

public sealed class KurumTakvimiTests
{
    [Fact]
    public void OracleUtcTimestampRendersTheNextDayInTurkey()
    {
        new DateTime(2026, 9, 10, 21, 30, 0).TurkiyeSaatineCevir()
            .Should().Be(new DateTime(2026, 9, 11, 0, 30, 0));
    }

    [Fact]
    public void LocalAuditDateBoundariesConvertToUtc()
    {
        var gun = new DateTime(2026, 9, 11);
        gun.UtcSaatineCevir().Should().Be(new DateTime(2026, 9, 10, 21, 0, 0, DateTimeKind.Utc));
        gun.AddDays(1).UtcSaatineCevir().Should().Be(new DateTime(2026, 9, 11, 21, 0, 0, DateTimeKind.Utc));
    }
}
