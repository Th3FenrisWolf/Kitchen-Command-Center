using KCC.ResourceStrings.Editing;

namespace KCC.UnitTests.Features.ResourceStringEditing;

public class ResourceStringUpsertHandlerTests
{
    private readonly FakeResourceStringRepository _repo = new();
    private readonly FakeContentLanguageRepository _languages = new("en", "es");

    [Test]
    public async Task Upsert_DefaultLanguage_KeyExists_UpdatesValue()
    {
        _repo.AddString("Login.Login", value: "Log in");
        var sut = new ResourceStringUpsertHandler(_repo, _languages, defaultLanguage: "en");

        var result = sut.Upsert(new ResourceStringUpsertRequest
        {
            Key = "Login.Login",
            Language = "en",
            Value = "Sign in",
        });

        _ = await Assert.That(result.Value).IsEqualTo("Sign in");
        _ = await Assert.That(_repo.GetValue("Login.Login")).IsEqualTo("Sign in");
    }

    [Test]
    public async Task Upsert_DefaultLanguage_KeyMissing_CreatesNewRow()
    {
        var sut = new ResourceStringUpsertHandler(_repo, _languages, defaultLanguage: "en");

        var result = sut.Upsert(new ResourceStringUpsertRequest
        {
            Key = "Login.Login",
            Language = "en",
            Value = "Log in",
        });

        _ = await Assert.That(result.Value).IsEqualTo("Log in");
        _ = await Assert.That(_repo.GetValue("Login.Login")).IsEqualTo("Log in");
    }

    [Test]
    public async Task Upsert_NonDefaultLanguage_TranslationExists_UpdatesTranslation()
    {
        _repo.AddString("Login.Login", value: "Log in", translations: new() { ["es"] = "Iniciar" });
        var sut = new ResourceStringUpsertHandler(_repo, _languages, defaultLanguage: "en");

        var result = sut.Upsert(new ResourceStringUpsertRequest
        {
            Key = "Login.Login",
            Language = "es",
            Value = "Iniciar sesión",
        });

        _ = await Assert.That(result.Value).IsEqualTo("Iniciar sesión");
        _ = await Assert.That(_repo.GetTranslation("Login.Login", "es")).IsEqualTo("Iniciar sesión");
        _ = await Assert.That(_repo.GetValue("Login.Login")).IsEqualTo("Log in"); // default unchanged
    }

    [Test]
    public async Task Upsert_NonDefaultLanguage_BothMissing_CreatesStringAndTranslation()
    {
        var sut = new ResourceStringUpsertHandler(_repo, _languages, defaultLanguage: "en");

        var result = sut.Upsert(new ResourceStringUpsertRequest
        {
            Key = "Login.Login",
            Language = "es",
            Value = "Iniciar sesión",
        });

        _ = await Assert.That(result.Value).IsEqualTo("Iniciar sesión");
        _ = await Assert.That(_repo.GetTranslation("Login.Login", "es")).IsEqualTo("Iniciar sesión");
        _ = await Assert.That(_repo.GetValue("Login.Login")).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Upsert_NonDefaultLanguage_NullValue_DeletesTranslation()
    {
        _repo.AddString("Login.Login", value: "Log in", translations: new() { ["es"] = "Iniciar" });
        var sut = new ResourceStringUpsertHandler(_repo, _languages, defaultLanguage: "en");

        var result = sut.Upsert(new ResourceStringUpsertRequest
        {
            Key = "Login.Login",
            Language = "es",
            Value = null,
        });

        _ = await Assert.That(result.Value).IsEqualTo(string.Empty);
        _ = await Assert.That(_repo.GetTranslation("Login.Login", "es")).IsNull();
    }

    [Test]
    public async Task Upsert_UnknownLanguage_Throws()
    {
        var sut = new ResourceStringUpsertHandler(_repo, _languages, defaultLanguage: "en");

        _ = await Assert.That(() => sut.Upsert(new()
        {
            Key = "Login.Login",
            Language = "fr",
            Value = "Connexion",
        })).Throws<InvalidLanguageException>();
    }

    private sealed class FakeResourceStringRepository : IResourceStringWriteRepository
    {
        private readonly Dictionary<string, string> _values = [];
        private readonly Dictionary<(string Key, string Lang), string> _translations = [];

        public void AddString(string key, string value, Dictionary<string, string> translations = null)
        {
            _values[key] = value;
            if (translations is null)
            {
                return;
            }

            foreach (var (lang, val) in translations)
            {
                _translations[(key, lang)] = val;
            }
        }

        public string GetValue(string key) => _values.TryGetValue(key, out var v) ? v : string.Empty;

        public string GetTranslation(string key, string lang) =>
            _translations.TryGetValue((key, lang), out var v) ? v : null;

        public void UpsertString(string key, string value)
        {
            _values[key] = value;
        }

        public bool StringExists(string key) =>
            _values.ContainsKey(key);

        public void UpsertTranslation(string key, string language, string value)
        {
            if (value is null)
            {
                _translations.Remove((key, language));
            }
            else
            {
                _translations[(key, language)] = value;
            }
        }
    }

    private sealed class FakeContentLanguageRepository(params string[] codes) : IContentLanguageRepository
    {
        public bool Exists(string code) => codes.Contains(code);

        public IReadOnlyList<ContentLanguageOption> ListAll() => Array.Empty<ContentLanguageOption>();
    }
}
