using FluentAssertions;
using MedulaOnKontrol.Domain.Enums;
using MedulaOnKontrol.Domain.Extensions;
using Xunit;

namespace MedulaOnKontrol.UnitTests;

public sealed class EnumContractTests
{
    [Theory]
    [InlineData(BasvuruTip.Ayaktan, 0, "AYAKTAN")]
    [InlineData(BasvuruTip.Yatan, 1, "YATAN")]
    [InlineData(BasvuruTip.Acil, 2, "ACIL")]
    [InlineData(BasvuruTip.Gunubirlik, 3, "GUNUBIRLIK")]
    [InlineData(KalemTip.Islem, 0, "ISLEM")]
    [InlineData(KalemTip.Ilac, 1, "ILAC")]
    [InlineData(KalemTip.Malzeme, 2, "MALZEME")]
    public void DatabaseValuesAndDisplayLabelsRemainStable(Enum value, int databaseValue, string displayLabel)
    {
        Convert.ToInt32(value).Should().Be(databaseValue);
        value.ToDisplayName().Should().Be(displayLabel);
    }
}
