using KCC.Web.Features.DevTools.RecipeSeed;

namespace KCC.UnitTests.Features.DevTools;

public class SeedKeysTests
{
    [Test]
    public async Task Recipe_IsTheSameOnEveryRun()
    {
        _ = await Assert.That(SeedKeys.Recipe("Shakshuka")).IsEqualTo(SeedKeys.Recipe("Shakshuka"));
    }

    [Test]
    public async Task Keys_DifferBetweenKinds()
    {
        var keys = new[]
        {
            SeedKeys.Recipe("Shakshuka"),
            SeedKeys.Variant("Shakshuka", "Shakshuka"),
            SeedKeys.Author("Shakshuka"),
            SeedKeys.Reviewer("Shakshuka", 0),
        };

        _ = await Assert.That(keys.Distinct().Count()).IsEqualTo(keys.Length);
    }

    [Test]
    public async Task Variant_DependsOnItsRecipe()
    {
        _ = await Assert.That(SeedKeys.Variant("Tacos", "Classic")).IsNotEqualTo(SeedKeys.Variant("Nachos", "Classic"));
    }

    [Test]
    public async Task Keys_AreVersion8RfcUuids()
    {
        var keys = new[]
        {
            SeedKeys.Recipe("Shakshuka"),
            SeedKeys.Variant("Shakshuka", "Harissa Shakshuka"),
            SeedKeys.Author("diego.salazar"),
            SeedKeys.Reviewer("Shakshuka", 0),
        };

        _ = await Assert.That(keys.All(key => key.Version == 8)).IsTrue();
        _ = await Assert.That(keys.All(key => (key.Variant & 0b1100) == 0b1000)).IsTrue();
    }
}
