using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Models.TemporaryFile;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace KCC.IntegrationTests.Config;

// Builds content for one test through the services the backoffice uses, so a test that changes content
// never touches the seeded data set other tests read.
public static class TestContent
{
    // A 1×1 PNG: enough for ImageSharp to resize and re-encode.
    private const string Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

    public static Guid RecipeListing(IServiceProvider services)
    {
        var navigation = services.GetRequiredService<IDocumentNavigationQueryService>();
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

        throw new InvalidOperationException("The baseline has no recipe listing page.");
    }

    public static Task<Guid> RecipeAsync(IServiceProvider services, string name, params PropertyValueModel[] more) =>
        PublishedAsync(
            services,
            "recipe",
            name,
            RecipeListing(services),
            [
                new PropertyValueModel { Alias = "description", Value = $"{name}, made for one test." },
                new PropertyValueModel { Alias = "icon", Value = "fa-duotone fa-egg" },
                .. more,
            ]);

    public static Task<Guid> VariantAsync(IServiceProvider services, Guid recipeKey, string name, params PropertyValueModel[] more) =>
        PublishedAsync(
            services,
            "recipeVariant",
            name,
            recipeKey,
            [
                new PropertyValueModel { Alias = "description", Value = $"{name}, made for one test." },
                new PropertyValueModel { Alias = "icon", Value = "fa-duotone fa-egg" },
                new PropertyValueModel { Alias = "prepTime", Value = 5 },
                new PropertyValueModel { Alias = "cookTime", Value = 10 },
                new PropertyValueModel { Alias = "servings", Value = 2 },
                new PropertyValueModel { Alias = "ingredients", Value = """[{"name":"Eggs","quantity":2,"unit":"whole","isEyeballed":false}]""" },
                new PropertyValueModel { Alias = "instructions", Value = """[{"step":1,"text":"Scramble."}]""" },
                .. more,
            ]);

    public static PropertyValueModel Image(string alias, Guid mediaKey) => new()
    {
        Alias = alias,
        Value = $"[{{\"key\":\"{Guid.NewGuid()}\",\"mediaKey\":\"{mediaKey}\"}}]",
    };

    public static async Task<Guid> ImageAsync(IServiceProvider services, string name)
    {
        using var scope = services.CreateScope();
        var temporaryKey = Guid.NewGuid();
        var upload = await scope.ServiceProvider.GetRequiredService<ITemporaryFileService>().CreateAsync(new CreateTemporaryFileModel
        {
            Key = temporaryKey,
            FileName = "photo.png",
            OpenReadStream = () => new MemoryStream(Convert.FromBase64String(Png)),
        });
        if (!upload.Success)
        {
            throw new InvalidOperationException($"Uploading {name} failed: {upload.Status}.");
        }

        var mediaKey = Guid.NewGuid();
        var created = await scope.ServiceProvider.GetRequiredService<IMediaEditingService>().CreateAsync(
            new MediaCreateModel
            {
                Key = mediaKey,
                ContentTypeKey = Constants.MediaTypes.Guids.ImageGuid,
                Variants = [new VariantModel { Name = name }],
                Properties = [new PropertyValueModel { Alias = "umbracoFile", Value = $"{{\"temporaryFileId\":\"{temporaryKey}\"}}" }],
            },
            Constants.Security.SuperUserKey);
        if (created.Status != ContentEditingOperationStatus.Success)
        {
            throw new InvalidOperationException($"Creating image {name} failed: {created.Status}.");
        }

        return mediaKey;
    }

    public static PropertyValueModel Pick(string alias, Guid documentKey) => new()
    {
        Alias = alias,
        Value = new JsonArray(new JsonObject { ["type"] = "document", ["unique"] = documentKey.ToString() }),
    };

    public static PropertyValueModel Author(Guid memberKey) => new() { Alias = "author", Value = memberKey.ToString() };

    public static Task<Guid> CategoryAsync(IServiceProvider services, string name) =>
        PublishedAsync(services, "recipeCategory", name, Folder(services, "Recipe Categories"), []);

    public static Task<Guid> TagAsync(IServiceProvider services, string name) =>
        PublishedAsync(services, "recipeTag", name, Folder(services, "Recipe Tags"), []);

    public static async Task TrashAsync(IServiceProvider services, Guid key)
    {
        using var scope = services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IContentEditingService>()
            .MoveToRecycleBinAsync(key, Constants.Security.SuperUserKey);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Trashing {key} failed: {result.Status}.");
        }
    }

    public static async Task<Guid> AuthorAsync(IServiceProvider services, string userName, string firstName, string lastName)
    {
        using var scope = services.CreateScope();
        var scoped = scope.ServiceProvider;
        var superUser = await scoped.GetRequiredService<IUserService>().GetAsync(Constants.Security.SuperUserKey)
            ?? throw new InvalidOperationException("The super user is missing.");
        var created = await scoped.GetRequiredService<IMemberEditingService>().CreateAsync(
            new MemberCreateModel
            {
                Key = Guid.NewGuid(),
                ContentTypeKey = scoped.GetRequiredService<IMemberTypeService>().Get(Constants.Security.DefaultMemberTypeAlias)!.Key,
                Username = userName,
                Email = $"{userName}@example.test",
                Password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)),
                IsApproved = true,
                Variants = [new VariantModel { Name = $"{firstName} {lastName}" }],
                Properties =
                [
                    new PropertyValueModel { Alias = "firstName", Value = firstName },
                    new PropertyValueModel { Alias = "lastName", Value = lastName },
                ],
            },
            superUser);
        if (!created.Success)
        {
            throw new InvalidOperationException($"Creating member {userName} failed: {created.Status.MemberEditingOperationStatus}.");
        }

        return created.Result.Content!.Key;
    }

    public static void RenameAuthor(IServiceProvider services, Guid memberKey, string firstName, string lastName)
    {
        using var scope = services.CreateScope();
        var memberService = scope.ServiceProvider.GetRequiredService<IMemberService>();
        var member = memberService.GetById(memberKey) ?? throw new InvalidOperationException($"No member {memberKey}.");
        member.SetValue("firstName", firstName);
        member.SetValue("lastName", lastName);
        memberService.Save(member);
    }

    public static async Task RenameAsync(IServiceProvider services, Guid key, string name)
    {
        using var scope = services.CreateScope();
        var contentService = scope.ServiceProvider.GetRequiredService<IContentService>();
        var content = contentService.GetById(key) ?? throw new InvalidOperationException($"No content {key}.");
        content.Name = name;
        if (!contentService.Save(content).Success)
        {
            throw new InvalidOperationException($"Renaming {key} failed.");
        }

        var published = await scope.ServiceProvider.GetRequiredService<IContentPublishingService>()
            .PublishAsync(key, [new CulturePublishScheduleModel { Culture = null }], Constants.Security.SuperUserKey);
        if (!published.Success)
        {
            throw new InvalidOperationException($"Publishing {name} failed: {published.Status}.");
        }
    }

    public static async Task UnpublishAsync(IServiceProvider services, Guid key)
    {
        using var scope = services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IContentPublishingService>()
            .UnpublishAsync(key, null, Constants.Security.SuperUserKey);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Unpublishing {key} failed: {result.Result}.");
        }
    }

    private static Guid Folder(IServiceProvider services, string name)
    {
        services.GetRequiredService<IDocumentNavigationQueryService>().TryGetRootKeysOfType("contentFolder", out var folders);
        return services.GetRequiredService<IContentService>().GetByIds(folders).Single(folder => folder.Name == name).Key;
    }

    private static async Task<Guid> PublishedAsync(IServiceProvider services, string contentTypeAlias, string name, Guid parentKey, IEnumerable<PropertyValueModel> values)
    {
        using var scope = services.CreateScope();
        var scoped = scope.ServiceProvider;
        var key = Guid.NewGuid();
        var created = await scoped.GetRequiredService<IContentEditingService>().CreateAsync(
            new ContentCreateModel
            {
                Key = key,
                ContentTypeKey = scoped.GetRequiredService<IContentTypeService>().Get(contentTypeAlias)!.Key,
                ParentKey = parentKey,
                Variants = [new VariantModel { Name = name }],
                Properties = values,
            },
            Constants.Security.SuperUserKey);
        if (created.Status != ContentEditingOperationStatus.Success)
        {
            throw new InvalidOperationException($"Creating {name} failed: {created.Status}.");
        }

        var published = await scoped.GetRequiredService<IContentPublishingService>()
            .PublishAsync(key, [new CulturePublishScheduleModel { Culture = null }], Constants.Security.SuperUserKey);
        if (!published.Success)
        {
            throw new InvalidOperationException($"Publishing {name} failed: {published.Status}.");
        }

        return key;
    }
}
