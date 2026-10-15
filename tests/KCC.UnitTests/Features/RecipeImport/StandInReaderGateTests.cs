using Anthropic;
using KCC.Web.Features.DevTools.RecipeImport;
using KCC.Web.Features.Models.Options;
using KCC.Web.Features.Providers;
using KCC.Web.Features.RecipeImport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace KCC.UnitTests.Features.RecipeImport;

public class StandInReaderGateTests
{
    [Test]
    [Arguments("Development")]
    [Arguments("Testing")]
    public async Task InDevelopmentOrTesting_WithTheStandInReaderOn_TheStandInIsChosen(string environment)
    {
        var reader = RegisteredReader(environment, standInReader: "true");

        _ = await Assert.That(reader.GetType()).IsEqualTo(typeof(StandInRecipePhotoReader));
    }

    [Test]
    [Arguments("Development", "false")]
    [Arguments("Testing", "false")]
    [Arguments("Development", null)]
    [Arguments("Testing", null)]
    public async Task InDevelopmentOrTesting_WithTheStandInReaderOff_TheAnthropicReaderIsChosen(
        string environment,
        string standInReader)
    {
        var reader = RegisteredReader(environment, standInReader);

        _ = await Assert.That(reader.GetType()).IsEqualTo(typeof(AnthropicRecipePhotoReader));
    }

    [Test]
    [Arguments("Production")]
    [Arguments("Staging")]
    public async Task OutsideDevelopmentAndTesting_TheStandInIsNeverChosen(string environment)
    {
        var reader = RegisteredReader(environment, standInReader: "true");

        _ = await Assert.That(reader.GetType()).IsEqualTo(typeof(AnthropicRecipePhotoReader));
    }

    private static IRecipePhotoReader RegisteredReader(string environmentName, string standInReader)
    {
        var services = new ServiceCollection();
        var settings = standInReader is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string> { ["RecipeImport:StandInReader"] = standInReader };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        new RecipeImportComposer().Compose(
            Mock.Of<IUmbracoBuilder>(builder => builder.Services == services && builder.Config == configuration));

        var anthropic = new AnthropicOptions();
        services.AddSingleton(Mock.Of<IHostEnvironment>(host => host.EnvironmentName == environmentName));
        services.AddSingleton(anthropic);
        services.AddSingleton(new AnthropicClient(AnthropicClientOptionsFactory.Create(anthropic)));
        services.AddLogging();

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IRecipePhotoReader>();
    }
}
