// KCC.Web.Tests does not enable nullable globally; this file opts in so the
// fake repository can express the semantically meaningful nullability the
// handler relies on (string? translation values where null means "delete").
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KCC.Web.Features.ResourceStringEditing;
using Xunit;

namespace KCC.Web.Tests.Features.ResourceStringEditing;

public class ResourceStringUpsertHandlerTests
{
    private readonly FakeResourceStringRepository _repo = new();
    private readonly FakeContentLanguageRepository _languages = new("en", "es");

    [Fact]
    public async Task UpsertAsync_DefaultLanguage_KeyExists_UpdatesValue()
    {
        _repo.AddString("Login.Login", value: "Log in");
        var sut = new ResourceStringUpsertHandler(_repo, _languages, defaultLanguage: "en");

        var result = await sut.UpsertAsync(new ResourceStringUpsertRequest
        {
            Key = "Login.Login",
            Language = "en",
            Value = "Sign in",
        });

        Assert.Equal("Sign in", result.Value);
        Assert.Equal("Sign in", _repo.GetValue("Login.Login"));
    }

    [Fact]
    public async Task UpsertAsync_DefaultLanguage_KeyMissing_CreatesNewRow()
    {
        var sut = new ResourceStringUpsertHandler(_repo, _languages, defaultLanguage: "en");

        var result = await sut.UpsertAsync(new ResourceStringUpsertRequest
        {
            Key = "Login.Login",
            Language = "en",
            Value = "Log in",
        });

        Assert.Equal("Log in", result.Value);
        Assert.Equal("Log in", _repo.GetValue("Login.Login"));
    }

    [Fact]
    public async Task UpsertAsync_NonDefaultLanguage_TranslationExists_UpdatesTranslation()
    {
        _repo.AddString("Login.Login", value: "Log in", translations: new() { ["es"] = "Iniciar" });
        var sut = new ResourceStringUpsertHandler(_repo, _languages, defaultLanguage: "en");

        var result = await sut.UpsertAsync(new ResourceStringUpsertRequest
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
    public async Task UpsertAsync_NonDefaultLanguage_BothMissing_CreatesStringAndTranslation()
    {
        var sut = new ResourceStringUpsertHandler(_repo, _languages, defaultLanguage: "en");

        var result = await sut.UpsertAsync(new ResourceStringUpsertRequest
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
    public async Task UpsertAsync_NonDefaultLanguage_NullValue_DeletesTranslation()
    {
        _repo.AddString("Login.Login", value: "Log in", translations: new() { ["es"] = "Iniciar" });
        var sut = new ResourceStringUpsertHandler(_repo, _languages, defaultLanguage: "en");

        var result = await sut.UpsertAsync(new ResourceStringUpsertRequest
        {
            Key = "Login.Login",
            Language = "es",
            Value = null,
        });

        Assert.Equal(string.Empty, result.Value);
        Assert.Null(_repo.GetTranslation("Login.Login", "es"));
    }

    [Fact]
    public async Task UpsertAsync_UnknownLanguage_Throws()
    {
        var sut = new ResourceStringUpsertHandler(_repo, _languages, defaultLanguage: "en");

        await Assert.ThrowsAsync<InvalidLanguageException>(() => sut.UpsertAsync(new()
        {
            Key = "Login.Login",
            Language = "fr",
            Value = "Connexion",
        }));
    }

    private sealed class FakeResourceStringRepository : IResourceStringWriteRepository
    {
        private readonly Dictionary<string, string> _values = new();
        private readonly Dictionary<(string Key, string Lang), string?> _translations = new();

        public void AddString(string key, string value, Dictionary<string, string>? translations = null)
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

        public string? GetTranslation(string key, string lang) =>
            _translations.TryGetValue((key, lang), out var v) ? v : null;

        public Task UpsertStringAsync(string key, string value)
        {
            _values[key] = value;
            return Task.CompletedTask;
        }

        public Task<bool> StringExistsAsync(string key) =>
            Task.FromResult(_values.ContainsKey(key));

        public Task UpsertTranslationAsync(string key, string language, string? value)
        {
            if (value is null)
            {
                _translations.Remove((key, language));
            }
            else
            {
                _translations[(key, language)] = value;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeContentLanguageRepository(params string[] codes) : IContentLanguageRepository
    {
        public Task<bool> ExistsAsync(string code) => Task.FromResult(codes.Contains(code));

        public IReadOnlyList<ContentLanguageOption> ListAll() => Array.Empty<ContentLanguageOption>();
    }
}
