using KCC.ResourceStrings.Editing;
using Xunit;

namespace KCC.Web.Tests.Features.ResourceStringEditing;

public class ResourceStringUpsertHandlerTests
{
    private readonly FakeResourceStringRepository _repo = new();
    private readonly FakeContentLanguageRepository _languages = new("en", "es");

    [Fact]
    public void Upsert_DefaultLanguage_KeyExists_UpdatesValue()
    {
        _repo.AddString("Login.Login", value: "Log in");
        var sut = new ResourceStringUpsertHandler(_repo, _languages, defaultLanguage: "en");

        var result = sut.Upsert(new ResourceStringUpsertRequest
        {
            Key = "Login.Login",
            Language = "en",
            Value = "Sign in",
        });

        Assert.Equal("Sign in", result.Value);
        Assert.Equal("Sign in", _repo.GetValue("Login.Login"));
    }

    [Fact]
    public void Upsert_DefaultLanguage_KeyMissing_CreatesNewRow()
    {
        var sut = new ResourceStringUpsertHandler(_repo, _languages, defaultLanguage: "en");

        var result = sut.Upsert(new ResourceStringUpsertRequest
        {
            Key = "Login.Login",
            Language = "en",
            Value = "Log in",
        });

        Assert.Equal("Log in", result.Value);
        Assert.Equal("Log in", _repo.GetValue("Login.Login"));
    }

    [Fact]
    public void Upsert_NonDefaultLanguage_TranslationExists_UpdatesTranslation()
    {
        _repo.AddString("Login.Login", value: "Log in", translations: new() { ["es"] = "Iniciar" });
        var sut = new ResourceStringUpsertHandler(_repo, _languages, defaultLanguage: "en");

        var result = sut.Upsert(new ResourceStringUpsertRequest
        {
            Key = "Login.Login",
            Language = "es",
            Value = "Iniciar sesión",
        });

        Assert.Equal("Iniciar sesión", result.Value);
        Assert.Equal("Iniciar sesión", _repo.GetTranslation("Login.Login", "es"));
        Assert.Equal("Log in", _repo.GetValue("Login.Login")); // default unchanged
    }

    [Fact]
    public void Upsert_NonDefaultLanguage_BothMissing_CreatesStringAndTranslation()
    {
        var sut = new ResourceStringUpsertHandler(_repo, _languages, defaultLanguage: "en");

        var result = sut.Upsert(new ResourceStringUpsertRequest
        {
            Key = "Login.Login",
            Language = "es",
            Value = "Iniciar sesión",
        });

        Assert.Equal("Iniciar sesión", result.Value);
        Assert.Equal("Iniciar sesión", _repo.GetTranslation("Login.Login", "es"));
        Assert.Equal(string.Empty, _repo.GetValue("Login.Login"));
    }

    [Fact]
    public void Upsert_NonDefaultLanguage_NullValue_DeletesTranslation()
    {
        _repo.AddString("Login.Login", value: "Log in", translations: new() { ["es"] = "Iniciar" });
        var sut = new ResourceStringUpsertHandler(_repo, _languages, defaultLanguage: "en");

        var result = sut.Upsert(new ResourceStringUpsertRequest
        {
            Key = "Login.Login",
            Language = "es",
            Value = null,
        });

        Assert.Equal(string.Empty, result.Value);
        Assert.Null(_repo.GetTranslation("Login.Login", "es"));
    }

    [Fact]
    public void Upsert_UnknownLanguage_Throws()
    {
        var sut = new ResourceStringUpsertHandler(_repo, _languages, defaultLanguage: "en");

        Assert.Throws<InvalidLanguageException>(() => sut.Upsert(new()
        {
            Key = "Login.Login",
            Language = "fr",
            Value = "Connexion",
        }));
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
