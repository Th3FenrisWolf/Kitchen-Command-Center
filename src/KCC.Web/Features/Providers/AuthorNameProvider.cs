using Microsoft.Extensions.Caching.Memory;
using Umbraco.Cms.Core.Services;

namespace KCC.Web.Features.Providers;

public interface IAuthorNameProvider
{
    Task<string> Resolve(Guid memberKey);

    Task<IReadOnlyDictionary<Guid, string>> ResolveMany(IEnumerable<Guid> memberKeys);

    void Forget(IEnumerable<Guid> memberKeys);
}

public class AuthorNameProvider(IMemberService memberService, IMemoryCache cache) : IAuthorNameProvider
{
    public const string DeletedMemberName = "(deleted)";

    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(1);

    public static string FormatDisplayName(string firstName, string lastName, string userName)
    {
        var fullName = $"{firstName?.Trim()} {lastName?.Trim()}".Trim();
        if (fullName.Length > 0)
        {
            return fullName;
        }

        var fallback = userName?.Trim();
        return string.IsNullOrEmpty(fallback) ? null : fallback;
    }

    public static string NameFor(IReadOnlyDictionary<Guid, string> names, Guid? memberKey) =>
        memberKey is { } key ? names.GetValueOrDefault(key) : null;

    public async Task<string> Resolve(Guid memberKey) => NameFor(await ResolveMany([memberKey]), memberKey);

    public async Task<IReadOnlyDictionary<Guid, string>> ResolveMany(IEnumerable<Guid> memberKeys)
    {
        var names = new Dictionary<Guid, string>();
        var uncached = new List<Guid>();
        foreach (var key in memberKeys.Where(key => key != Guid.Empty).Distinct())
        {
            if (!cache.TryGetValue(CacheKey(key), out string cached))
            {
                uncached.Add(key);
            }
            else if (cached.Length > 0)
            {
                names[key] = cached;
            }
        }

        if (uncached.Count == 0)
        {
            return names;
        }

        var members = (await memberService.GetByKeysAsync([.. uncached])).ToDictionary(member => member.Key);
        foreach (var key in uncached)
        {
            var name = members.TryGetValue(key, out var member)
                ? FormatDisplayName(member.GetValue<string>("firstName"), member.GetValue<string>("lastName"), member.Username)
                : null;

            // A missing name is cached as empty too, so reviews by deleted members cost one lookup, not one a page view.
            cache.Set(CacheKey(key), name ?? string.Empty, CacheLifetime);
            if (name is not null)
            {
                names[key] = name;
            }
        }

        return names;
    }

    public void Forget(IEnumerable<Guid> memberKeys)
    {
        foreach (var key in memberKeys)
        {
            cache.Remove(CacheKey(key));
        }
    }

    private static string CacheKey(Guid memberKey) => $"kcc:author-name:{memberKey}";
}
