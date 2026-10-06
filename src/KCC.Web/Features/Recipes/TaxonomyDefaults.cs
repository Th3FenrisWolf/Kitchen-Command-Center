using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;

namespace KCC.Web.Features.Recipes;

public class TaxonomyDefaults(ICoreScopeProvider scopeProvider, IContentService contentService, IContentPublishingService publishingService)
{
    public static IReadOnlyList<TaxonomyDefault> Values { get; } =
    [
        Tag("8a01e89e-f286-872e-8ff2-a506ae4b12d3", "Vegetarian", TagKinds.Diet),
        Tag("b8563daf-0df0-8200-98ac-6753e87e59cc", "Vegan", TagKinds.Diet),
        Tag("bd3a5a4d-c8a5-8fad-a9a3-1f552a65380a", "Gluten-Free", TagKinds.Diet),
        Tag("8f23c142-34e1-8542-ac42-fc48ad8d37ba", "Dairy-Free", TagKinds.Diet),
        Tag("eaf6b83d-7e53-8dd9-a042-845b847be8a1", "Keto", TagKinds.Diet),
        Tag("7a4c7fde-136d-8e4c-be09-c7eaa31b2ba3", "Low-Carb", TagKinds.Diet),
        Tag("79ef71b8-cb66-8b58-a1b3-9cf363c15389", "High-Protein", TagKinds.Diet),
        Tag("184f3466-66bf-8947-9fad-a5853d39a3d8", "Cheesy", TagKinds.Style),
        Tag("9c164b66-296c-8468-a4a1-4deee3879fba", "Easy", TagKinds.Style),
        Tag("3c80a96a-8582-8bca-b906-ec6be07deb69", "Fast", TagKinds.Style),
        Tag("fefc672b-221c-8cc7-8b7f-50cbee11da2a", "Spicy", TagKinds.Style),
        Category("164e3680-09c0-8a2d-b70c-7ffc738aed1c", "Breakfast", "fa-duotone fa-egg"),
        Category("775aec80-9bde-87c1-9884-fdc14fff9efa", "Lunch", "fa-duotone fa-sandwich"),
        Category("f08799ce-a590-8601-b7e3-409d91c099f9", "Dinner", "fa-duotone fa-pot-food"),
        Category("2d969f5c-e14b-805a-b5ff-2a489a9f59ea", "Dessert", "fa-duotone fa-cake-candles"),
        Category("eb3e4a39-5919-8a0a-a2d1-9abb3b98ca43", "Snack", "fa-duotone fa-cookie"),
        Category("330c6947-216e-83af-b452-2c158e8cf67c", "Beverage", "fa-duotone fa-mug-hot"),
    ];

    public async Task<int> ApplyAsync()
    {
        using var scope = scopeProvider.CreateCoreScope();
        scope.WriteLock(Constants.Locks.ContentTree);
        var applied = 0;
        foreach (var value in Values)
        {
            var content = contentService.GetById(value.Key);
            if (content is null || !string.IsNullOrEmpty(content.GetValue<string>(value.Alias)))
            {
                continue;
            }

            content.SetValue(value.Alias, value.Stored);
            var saved = contentService.Save(content);
            if (!saved.Success)
            {
                throw new InvalidOperationException($"Saving the default {value.Alias} of {value.Name} failed: {saved.Result}.");
            }

            if (content.Published)
            {
                var published = await publishingService.PublishAsync(
                    value.Key,
                    [new CulturePublishScheduleModel { Culture = null }],
                    Constants.Security.SuperUserKey);
                if (!published.Success)
                {
                    throw new InvalidOperationException($"Publishing the default {value.Alias} of {value.Name} failed: {published.Status}.");
                }
            }

            applied++;
        }

        scope.Complete();
        return applied;
    }

    private static TaxonomyDefault Tag(string key, string name, string kind) => new(new Guid(key), name, "kind", $"[\"{kind}\"]");

    private static TaxonomyDefault Category(string key, string name, string icon) => new(new Guid(key), name, "icon", icon);
}

public sealed record TaxonomyDefault(Guid Key, string Name, string Alias, string Stored);
