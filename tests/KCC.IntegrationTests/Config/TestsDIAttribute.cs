using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Config;

/// <summary>
/// TUnit data source that resolves test arguments from the shared
/// <see cref="IntegrationTestHost"/> DI container. For per-test fakes, use Moq directly
/// inside the test method rather than reaching for DI overrides.
/// </summary>
public class TestsDIAttribute : DependencyInjectionDataSourceAttribute<IServiceScope>
{
    public override IServiceScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata) =>
        IntegrationTestHost.Services.CreateAsyncScope();

    public override object? Create(IServiceScope scope, Type type) =>
        ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, type);
}
