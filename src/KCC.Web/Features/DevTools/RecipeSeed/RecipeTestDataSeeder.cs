using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using KCC.Contributions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace KCC.Web.Features.DevTools.RecipeSeed;

public class RecipeTestDataSeeder(
    IContentService contentService,
    IContentTypeService contentTypeService,
    IContentEditingService contentEditingService,
    IContentPublishingService contentPublishingService,
    IDocumentNavigationQueryService navigation,
    IMemberService memberService,
    IMemberTypeService memberTypeService,
    IMemberEditingService memberEditingService,
    IUserService userService,
    IContributionWrites contributionWrites)
{
    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<SeedSummary> RunAsync(TextWriter log, CancellationToken cancellationToken)
    {
        var summary = new SeedSummary();
        var recipeTypeKey = RequireContentType("recipe");
        var variantTypeKey = RequireContentType("recipeVariant");
        var listingKey = FindRecipeListing();
        var categories = KeysByName("recipeCategory");
        var tags = KeysByName("recipeTag");
        var authors = await EnsureAuthorsAsync(summary, log);
        var today = DateTime.UtcNow.Date;

        foreach (var recipe in RecipeSeedData.Recipes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var recipeKey = SeedKeys.Recipe(recipe.Name);
            if (contentService.GetById(recipeKey) is not null)
            {
                summary.RecipesSkipped++;
                log.WriteLine($"  skip (exists): {recipe.Name}");
                continue;
            }

            Guid? authorKey = recipe.AuthorKey is { } author ? authors[author] : null;
            var published = DateTime.SpecifyKind(today.AddDays(-recipe.PublishedDaysAgo), DateTimeKind.Utc);
            await CreatePublishedAsync(recipeKey, recipe.Name, recipeTypeKey, listingKey, published, RecipeValues(recipe, categories, authorKey));
            summary.RecipesCreated++;

            for (var index = 0; index < recipe.Variants.Length; index++)
            {
                var variant = recipe.Variants[index];
                await CreatePublishedAsync(
                    SeedKeys.Variant(recipe.Name, variant.Name),
                    variant.Name,
                    variantTypeKey,
                    recipeKey,
                    published.AddMinutes(index + 1),
                    VariantValues(variant, tags, authorKey));
                summary.VariantsCreated++;
            }

            // Reviews attach to the first variant, as they always have in this data set.
            if (recipe.Variants.Length > 0)
            {
                var firstVariantKey = SeedKeys.Variant(recipe.Name, recipe.Variants[0].Name);
                for (var index = 0; index < recipe.Reviews.Length; index++)
                {
                    await contributionWrites.UpsertReviewAsync(
                        firstVariantKey,
                        SeedKeys.Reviewer(recipe.Name, index),
                        recipe.Reviews[index].Rating,
                        $"Seeded review #{index + 1}");
                    summary.ReviewsWritten++;
                }
            }

            log.WriteLine($"  created: {recipe.Name}");
        }

        log.WriteLine(summary.ToString());
        return summary;
    }

    private static List<PropertyValueModel> RecipeValues(SeedRecipe recipe, IReadOnlyDictionary<string, Guid> categories, Guid? authorKey)
    {
        var values = new List<PropertyValueModel>
        {
            new() { Alias = "description", Value = recipe.Description },
            new() { Alias = "icon", Value = recipe.Icon },
        };

        if (categories.TryGetValue(recipe.Category, out var categoryKey))
        {
            values.Add(new() { Alias = "category", Value = DocumentReferences(categoryKey) });
        }

        AddAuthor(values, authorKey);
        return values;
    }

    private static List<PropertyValueModel> VariantValues(SeedVariant variant, IReadOnlyDictionary<string, Guid> tags, Guid? authorKey)
    {
        var values = new List<PropertyValueModel>
        {
            new() { Alias = "description", Value = variant.Description },
            new() { Alias = "icon", Value = variant.Icon },
            new() { Alias = "prepTime", Value = variant.PrepMinutes },
            new() { Alias = "cookTime", Value = variant.CookMinutes },
            new() { Alias = "servings", Value = variant.Servings },
            new()
            {
                Alias = "ingredients",
                Value = JsonSerializer.Serialize(
                    variant.Ingredients.Select(ingredient => new { name = ingredient.Name, quantity = ingredient.Quantity, unit = ingredient.Unit, isEyeballed = ingredient.IsEyeballed }),
                    CamelCase),
            },
            new()
            {
                Alias = "instructions",
                Value = JsonSerializer.Serialize(variant.Instructions.Select(step => new { step = step.Step, text = step.Text }), CamelCase),
            },
        };

        var tagKeys = variant.Diets.Where(tags.ContainsKey).Select(diet => tags[diet]).ToArray();
        if (tagKeys.Length > 0)
        {
            values.Add(new() { Alias = "tags", Value = DocumentReferences(tagKeys) });
        }

        AddAuthor(values, authorKey);
        return values;
    }

    // The member picker's editor takes the member key as a string, and stores it as a member UDI.
    private static void AddAuthor(List<PropertyValueModel> values, Guid? authorKey)
    {
        if (authorKey is { } key)
        {
            values.Add(new() { Alias = "author", Value = key.ToString() });
        }
    }

    // The multi-node tree picker's editor accepts only a JsonArray of { type, unique } references.
    private static JsonArray DocumentReferences(params Guid[] keys) =>
        new(keys.Select(key => (JsonNode)new JsonObject { ["type"] = "document", ["unique"] = key.ToString() }).ToArray());

    private Guid RequireContentType(string alias) =>
        contentTypeService.Get(alias)?.Key ?? throw new InvalidOperationException($"Document type {alias} is missing; uSync imports it at startup.");

    private Guid FindRecipeListing()
    {
        if (navigation.TryGetRootKeysOfType("homePage", out var homes))
        {
            foreach (var home in homes)
            {
                if (navigation.TryGetChildrenKeysOfType(home, "recipeListingPage", out var listings) && listings.Any())
                {
                    return listings.First();
                }
            }
        }

        throw new InvalidOperationException("The baseline has no recipe listing page under Home.");
    }

    private Dictionary<string, Guid> KeysByName(string contentTypeAlias)
    {
        var keys = new List<Guid>();
        if (navigation.TryGetRootKeys(out var roots))
        {
            foreach (var root in roots)
            {
                if (navigation.TryGetDescendantsKeysOfType(root, contentTypeAlias, out var found))
                {
                    keys.AddRange(found);
                }
            }
        }

        return contentService.GetByIds(keys).ToDictionary(node => node.Name, node => node.Key, StringComparer.Ordinal);
    }

    private async Task<Dictionary<string, Guid>> EnsureAuthorsAsync(SeedSummary summary, TextWriter log)
    {
        var memberTypeKey = memberTypeService.Get(Constants.Security.DefaultMemberTypeAlias)?.Key
            ?? throw new InvalidOperationException("The default member type is missing.");
        var superUser = await userService.GetAsync(Constants.Security.SuperUserKey)
            ?? throw new InvalidOperationException("The super user is missing.");
        var keys = new Dictionary<string, Guid>(StringComparer.Ordinal);

        foreach (var author in RecipeSeedData.Authors)
        {
            if (memberService.GetByUsername(author.UserName) is { } existing)
            {
                keys[author.Key] = existing.Key;
                continue;
            }

            var created = await memberEditingService.CreateAsync(
                new MemberCreateModel
                {
                    Key = SeedKeys.Author(author.UserName),
                    ContentTypeKey = memberTypeKey,
                    Username = author.UserName,
                    Email = author.Email,

                    // Seeded authors never sign in, so their password is random.
                    Password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)),
                    IsApproved = true,
                    Variants = [new VariantModel { Name = $"{author.FirstName} {author.LastName}" }],
                    Properties =
                    [
                        new PropertyValueModel { Alias = "firstName", Value = author.FirstName },
                        new PropertyValueModel { Alias = "lastName", Value = author.LastName },
                    ],
                },
                superUser);
            if (!created.Success)
            {
                throw new InvalidOperationException(
                    $"Creating author {author.UserName} failed: {created.Status.MemberEditingOperationStatus}, {created.Status.ContentEditingOperationStatus}.");
            }

            keys[author.Key] = created.Result.Content.Key;
            summary.AuthorsCreated++;
            log.WriteLine($"  author created: {author.FirstName} {author.LastName} ({author.UserName})");
        }

        return keys;
    }

    private async Task CreatePublishedAsync(Guid key, string name, Guid contentTypeKey, Guid parentKey, DateTime createDate, IEnumerable<PropertyValueModel> values)
    {
        var created = await contentEditingService.CreateAsync(
            new ContentCreateModel
            {
                Key = key,
                ContentTypeKey = contentTypeKey,
                ParentKey = parentKey,
                Variants = [new VariantModel { Name = name }],
                Properties = values,
            },
            Constants.Security.SuperUserKey);

        // A property that fails validation still saves and reports Success, so the status is what counts.
        if (created.Status != ContentEditingOperationStatus.Success)
        {
            throw new InvalidOperationException($"Creating {name} failed: {created.Status}.");
        }

        // The editing service stamps the time of creation; the "newest" sorts need the data set's spread.
        var content = contentService.GetById(key);
        content.CreateDate = createDate;
        if (!contentService.Save(content).Success)
        {
            throw new InvalidOperationException($"Backdating {name} failed.");
        }

        var published = await contentPublishingService.PublishAsync(
            key,
            [new CulturePublishScheduleModel { Culture = null }],
            Constants.Security.SuperUserKey);
        if (!published.Success)
        {
            throw new InvalidOperationException($"Publishing {name} failed: {published.Status}.");
        }
    }
}

public sealed class SeedSummary
{
    public int AuthorsCreated { get; set; }

    public int RecipesCreated { get; set; }

    public int RecipesSkipped { get; set; }

    public int VariantsCreated { get; set; }

    public int ReviewsWritten { get; set; }

    public override string ToString() =>
        $"Seed complete: recipes +{RecipesCreated} (skipped {RecipesSkipped}), variants +{VariantsCreated}, " +
        $"reviews +{ReviewsWritten}, authors +{AuthorsCreated}.";
}
