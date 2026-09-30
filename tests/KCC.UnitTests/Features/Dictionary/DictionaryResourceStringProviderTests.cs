using System.Globalization;
using KCC.Web.Features.Dictionary;
using Moq;
using Umbraco.Cms.Core.Dictionary;

namespace KCC.UnitTests.Features.Dictionary;

public class DictionaryResourceStringProviderTests
{
    [Test]
    public async Task GetOrDefault_KnownKey_ReturnsTheValue()
    {
        var provider = CreateProvider(("Login.SignIn", "Sign in"));

        _ = await Assert.That(provider.GetOrDefault("Login.SignIn")).IsEqualTo("Sign in");
    }

    [Test]
    public async Task GetOrDefault_MissingOrEmptyValue_ReturnsTheKey()
    {
        var provider = CreateProvider(("Login.Empty", string.Empty));

        _ = await Assert.That(provider.GetOrDefault("Login.Empty")).IsEqualTo("Login.Empty");
        _ = await Assert.That(provider.GetOrDefault("Login.Missing")).IsEqualTo("Login.Missing");
    }

    [Test]
    public async Task GetManyOrDefault_MapsEveryKeyWithTheKeyAsFallback()
    {
        var provider = CreateProvider(("Login.SignIn", "Sign in"));

        var strings = provider.GetManyOrDefault("Login.SignIn", "Login.Missing");

        _ = await Assert.That(strings["Login.SignIn"]).IsEqualTo("Sign in");
        _ = await Assert.That(strings["Login.Missing"]).IsEqualTo("Login.Missing");
    }

    [Test]
    public async Task GetManyOrDefault_DuplicateKeys_CollapseToOneEntry()
    {
        var provider = CreateProvider(("Login.SignIn", "Sign in"));

        var strings = provider.GetManyOrDefault("Login.SignIn", "Login.SignIn");

        _ = await Assert.That(strings.Count).IsEqualTo(1);
    }

    private static DictionaryResourceStringProvider CreateProvider(params (string Key, string Value)[] entries)
    {
        var dictionary = new Mock<ICultureDictionary>();
        dictionary.Setup(d => d[It.IsAny<string>()]).Returns(string.Empty);
        foreach (var (key, value) in entries)
        {
            dictionary.Setup(d => d[key]).Returns(value);
        }

        var factory = new Mock<ICultureDictionaryFactory>();
        factory.Setup(f => f.CreateDictionary(It.Is<CultureInfo>(culture => culture.Name == "en-US"))).Returns(dictionary.Object);
        return new DictionaryResourceStringProvider(factory.Object);
    }
}
