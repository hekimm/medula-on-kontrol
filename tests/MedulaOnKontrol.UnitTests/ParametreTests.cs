using FluentAssertions;
using MedulaOnKontrol.Application;
using Xunit;

namespace MedulaOnKontrol.UnitTests;
public sealed class ParametreTests
{
    [Theory]
    [InlineData("{\"ekGun\":\"1\"}")]
    [InlineData("{\"ekGun\":null}")]
    [InlineData("{\"ekGun\":true}")]
    [InlineData("{\"ekGun\":{}}")]
    [InlineData("{\"ekGun\":[]}")]
    [InlineData("{\"ekGun\":1,\"ekGun\":2}")]
    [InlineData("")]
    [InlineData(null)]
    public void InvalidParameterTypesAreRejectedWithoutThrowing(string? json) => KuralVersionRequestValidator.IsValidJson(json!).Should().BeFalse();
}
