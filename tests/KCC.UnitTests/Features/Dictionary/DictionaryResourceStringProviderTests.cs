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
    public async Task GetGroup_MapsEveryChildWithTheKeyAsFallback()
    {
        var provider = CreateProvider(("Login.SignIn", "Sign in"), ("Login.Empty", string.Empty));

        var strings = provider.GetGroup("Login");

        _ = await Assert.That(strings.Count).IsEqualTo(2);
        _ = await Assert.That(strings["Login.SignIn"]).IsEqualTo("Sign in");
        _ = await Assert.That(strings["Login.Empty"]).IsEqualTo("Login.Empty");
    }

    private static DictionaryResourceStringProvider CreateProvider(params (string Key, string Value)[] entries)
    {
        var dictionary = new Mock<ICultureDictionary>();
        dictionary.Setup(d => d[It.IsAny<string>()]).Returns(string.Empty);
        foreach (var (key, value) in entries)
        {
            dictionary.Setup(d => d[key]).Returns(value);
        }

        dictionary.Setup(d => d.GetChildren("Login")).Returns(entries.ToDictionary(entry => entry.Key, entry => entry.Value));

        var factory = new Mock<ICultureDictionaryFactory>();
        factory.Setup(f => f.CreateDictionary(It.Is<CultureInfo>(culture => culture.Name == "en-US"))).Returns(dictionary.Object);
        return new DictionaryResourceStringProvider(factory.Object);
    }
}
