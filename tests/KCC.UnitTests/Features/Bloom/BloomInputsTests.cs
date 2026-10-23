using KCC.Web.Features.Bloom;
using Microsoft.AspNetCore.Http;

namespace KCC.UnitTests.Features.Bloom;

public class BloomInputsTests
{
    [Test]
    [Arguments("/Recipes/Brown-Butter-Gnocchi", "/recipes/brown-butter-gnocchi")]
    [Arguments("/recipes/", "/recipes")]
    [Arguments("/recipes//", "/recipes")]
    [Arguments("/", "/")]
    [Arguments("", "/")]
    public async Task PageKey_LowerCasesThePathWithoutATrailingSlash(string path, string expected)
    {
        _ = await Assert.That(BloomInputs.PageKey(new PathString(path))).IsEqualTo(expected);
    }

    [Test]
    public async Task For_TakesTheVisitSeedAndTheRequestPage()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/Recipes/";
        var now = DateTimeOffset.FromUnixTimeSeconds(1_792_000_000);

        var inputs = BloomInputs.For(context, new Visit(2_671_381_617, now, now));

        _ = await Assert.That(inputs).IsEqualTo(new BloomInputs(2_671_381_617, "/recipes"));
    }
}
