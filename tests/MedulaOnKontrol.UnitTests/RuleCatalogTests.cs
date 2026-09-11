using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Xunit;

namespace MedulaOnKontrol.UnitTests;

public sealed class RuleCatalogTests
{
    [Fact]
    public void SeedCatalogDeserializesEveryRuleWithEnglishPropertyAndEnumNames()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "MedulaOnKontrol.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull();
        var json = File.ReadAllText(Path.Combine(directory!.FullName, "data", "kural-katalogu.json"));
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        var kurallar = JsonSerializer.Deserialize<List<Kural>>(json, options)!;

        kurallar.Should().HaveCount(40);
        kurallar.Select(kural => kural.KuralKodu).Should().OnlyHaveUniqueItems();
        kurallar.Should().OnlyContain(kural => kural.IsActive && kural.Agirlik > 0 &&
            kural.Name.Length > 0 && kural.Gerekce.Length > 0 &&
            kural.OnerilenAksiyon.Length > 0 && kural.YururlukBaslangic.Year == 2020);
        kurallar.Select(kural => kural.Severity).Should().Contain(Severity.Blocking).And.Contain(Severity.High);
    }
}
