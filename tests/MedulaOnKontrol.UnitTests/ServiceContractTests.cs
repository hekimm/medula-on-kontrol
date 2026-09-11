using System.Reflection;
using FluentAssertions;
using Xunit;

namespace MedulaOnKontrol.UnitTests;

public sealed class ServiceContractTests
{
    [Fact]
    public void PublicServiceContractsAreAsyncAndRequireCancellation()
    {
        var assemblies = new[] { typeof(KuralEngine).Assembly, typeof(RaporService).Assembly };
        var services = assemblies.SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.Name.EndsWith("Service", StringComparison.Ordinal) || type == typeof(KuralEngine));
        var methods = services.SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)).ToList();
        methods.Should().NotBeEmpty();
        foreach (var method in methods)
        {
            var resultType = method.ReturnType;
            var isAsync = resultType == typeof(Task) || resultType == typeof(ValueTask) ||
                resultType.IsGenericType && (resultType.GetGenericTypeDefinition() == typeof(Task<>) || resultType.GetGenericTypeDefinition() == typeof(ValueTask<>));
            isAsync.Should().BeTrue($"{method.DeclaringType!.Name}.{method.Name} must have an asynchronous contract");
            method.Name.Should().EndWith("Async");
            method.GetParameters().Should().Contain(parameter => parameter.ParameterType == typeof(CancellationToken) && !parameter.IsOptional);
        }
    }

    [Theory]
    [InlineData(202600)]
    [InlineData(202613)]
    [InlineData(0)]
    public async Task InvalidReportPeriodsReturnBusinessFailuresAsync(int donem)
    {
        var service = new RaporService();
        var data = new RaporVerisi(new(), [], []);
        (await service.CreatePdfAsync(data, donem, default)).IsSuccess.Should().BeFalse();
        (await service.CreateExcelAsync(data, donem, default)).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CancellationStopsCalculationsExportsAndEncryptionAsync()
    {
        var token = new CancellationToken(true);
        var data = new RaporVerisi(new(), [], []);
        var cipher = new SifrelemeService(new byte[32]);
        Func<Task>[] actions =
        [
            async () => await KuralTests.Engine().EvaluateAsync(KuralTests.Clean(), [], token),
            async () => await new RiskSkorlamaService(Microsoft.Extensions.Options.Options.Create(new SkorlamaOptions())).CalculateAsync(KuralTests.Clean(), [], token),
            async () => await KuralMetrikService.CalculateAsync([], [], token),
            async () => await KuralKalibrasyonService.CalculateOnerilenAgirlikAsync(10, new(), token),
            async () => await new RaporService().CreatePdfAsync(data, 202609, token),
            async () => await new RaporService().CreateExcelAsync(data, 202609, token),
            async () => await cipher.EncryptAsync("SENTETİK", token),
            async () => await cipher.DecryptAsync("invalid", token)
        ];
        foreach (var action in actions)
            await action.Should().ThrowAsync<OperationCanceledException>();
    }
}
