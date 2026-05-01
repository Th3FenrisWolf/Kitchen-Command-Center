using System.Reflection;
using System.Text.RegularExpressions;
using CMS;
using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.Websites;
using Kentico.Xperience.Admin.Base;
using KCC.Admin.UIPages.Home;

[assembly: RegisterImplementation(
    typeof(IAdminApplicationsSource),
    typeof(KenticoAdminApplicationsSource),
    Lifestyle = Lifestyle.Singleton
)]

namespace KCC.Admin.UIPages.Home;

public sealed partial class KenticoAdminApplicationsSource(
    IInfoProvider<ChannelInfo> channelProvider,
    IInfoProvider<WebsiteChannelInfo> websiteChannelProvider,
    ILocalizationService localizationService
) : IAdminApplicationsSource
{
    public const string ChannelsCategory = "Channels";
    public const string DynamicChannelIdentifierPrefix = "KCC.DynamicChannel:";

    private static readonly HashSet<string> HiddenCategoryCodeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "kentico.dashboard",
    };

    private static readonly Lazy<IReadOnlyList<UIApplicationAttribute>> CachedAttributes = new(LoadAttributes);
    private static readonly Lazy<IReadOnlyDictionary<string, UICategoryAttribute>> CachedCategories = new(LoadCategories);

    public async Task<IReadOnlyList<AdminApplicationDescriptor>> GetAccessibleApplicationsAsync(
        CancellationToken cancellationToken)
    {
        var descriptors = new List<AdminApplicationDescriptor>();

        foreach (var attr in CachedAttributes.Value)
        {
            if (attr.Type == typeof(HomeApplication))
            {
                continue;
            }

            // Skip apps that don't have a registered category (would otherwise land in
            // the synthetic "Other" group) or are in an explicitly hidden category.
            if (string.IsNullOrEmpty(attr.Category) || HiddenCategoryCodeNames.Contains(attr.Category))
            {
                continue;
            }

            descriptors.Add(new(
                Identifier: attr.Identifier,
                Name: ResolveLocalization(attr.Name),
                IconName: attr.Icon ?? string.Empty,
                Category: ResolveCategoryName(attr.Category ?? string.Empty),
                Url: $"/admin/{attr.Slug}",
                CategoryOrder: GetCategoryOrder(attr.Category ?? string.Empty)));
        }

        descriptors.AddRange(await GetChannelDescriptorsAsync(cancellationToken));

        return descriptors;
    }

    private async Task<IReadOnlyList<AdminApplicationDescriptor>> GetChannelDescriptorsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var websiteChannels = (await websiteChannelProvider.Get()
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken)
            ).ToList();
            var channels = (await channelProvider.Get()
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken)
            ).ToDictionary(c => c.ChannelID);

            var result = new List<AdminApplicationDescriptor>();

            foreach (var websiteChannel in websiteChannels)
            {
                if (!channels.TryGetValue(websiteChannel.WebsiteChannelChannelID, out var channel))
                {
                    continue;
                }

                result.Add(new AdminApplicationDescriptor(
                    Identifier: $"{DynamicChannelIdentifierPrefix}{channel.ChannelName}",
                    Name: channel.ChannelDisplayName,
                    IconName: Icons.Earth,
                    Category: ChannelsCategory,
                    Url: $"/admin/webpages-{websiteChannel.WebsiteChannelID}",
                    CategoryOrder: 0));
            }

            return result;
        }
        catch
        {
            // If channel enumeration fails (e.g., DB not yet initialized), fall back to no channel tiles.
            return [];
        }
    }

    private string ResolveLocalization(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return ResourceMacro().Replace(value, match =>
        {
            var key = match.Groups[1].Value.Trim();
            return ResolveKey(key) ?? match.Value;
        });
    }

    private static int GetCategoryOrder(string codeName)
    {
        if (CachedCategories.Value.TryGetValue(codeName, out var category))
        {
            return category.Order;
        }

        return int.MaxValue;
    }

    private string ResolveCategoryName(string codeName)
    {
        if (string.IsNullOrEmpty(codeName))
        {
            return "Other";
        }

        // Categories are registered separately via [UICategory(codeName, name, icon, order)].
        // The 'category' parameter on UIApplication is the codeName; we need to look up the
        // human-readable name from the matching UICategoryAttribute.
        if (CachedCategories.Value.TryGetValue(codeName, out var category))
        {
            return ResolveLocalization(category.Name);
        }

        // No matching UICategory registration — try resolving as a plain resource key,
        // then fall back to the codeName itself.
        return ResolveKey(codeName) ?? codeName;
    }

    private string ResolveKey(string key)
    {
        try
        {
            var resolved = localizationService.GetString(key);
            if (string.IsNullOrEmpty(resolved) || resolved == key)
            {
                return null;
            }

            return resolved;
        }
        catch
        {
            return null;
        }
    }

    private static List<UIApplicationAttribute> LoadAttributes()
    {
        var list = new List<UIApplicationAttribute>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                list.AddRange(assembly.GetCustomAttributes<UIApplicationAttribute>());
            }
            catch
            {
                // Some assemblies (e.g. dynamic, ReflectionOnly) may fail to enumerate attributes; skip.
            }
        }

        return list;
    }

    private static Dictionary<string, UICategoryAttribute> LoadCategories()
    {
        var map = new Dictionary<string, UICategoryAttribute>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var attr in assembly.GetCustomAttributes<UICategoryAttribute>())
                {
                    if (!string.IsNullOrEmpty(attr.CodeName))
                    {
                        map[attr.CodeName] = attr;
                    }
                }
            }
            catch
            {
                // skip assemblies that fail to enumerate attributes
            }
        }

        return map;
    }

    [GeneratedRegex(@"\{\$([^$]+)\$\}", RegexOptions.Compiled)]
    private static partial Regex ResourceMacro();

}
