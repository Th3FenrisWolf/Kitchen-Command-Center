using KCC.Admin;
using KCC.Web.Features.Api;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Recipes;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Extensions;

namespace KCC.Web.Features.Submissions;

// A submission is saved and never published; the owner publishes it once it has been checked.
public class RecipeSubmissions(
    IContentEditingService contentEditingService,
    IContentTypeService contentTypeService,
    ICoreScopeProvider scopeProvider,
    IPublishedContentQuery contentQuery,
    IRecipeQueries recipes,
    IRecipeIconService recipeIconService)
{
    public async Task<Guid> SubmitRecipeAsync(CreateRecipeRequest request, Guid authorKey, CancellationToken cancellationToken)
    {
        var listing = contentQuery.ContentAtRoot().OfType<HomePage>().SelectMany(home => home.Children<RecipeListingPage>()).FirstOrDefault()
            ?? throw new InvalidOperationException("The site has no recipe listing page.");
        var ingredients = SubmissionValues.IngredientNames(request.FirstVariant).ToList();

        // Picked before the write lock is taken: the icon service may call out to the Anthropic API.
        var recipeIcon = await recipeIconService.PickAsync(request.RecipeName, request.RecipeDescription, ingredients, cancellationToken);
        var variantIcon = await recipeIconService.PickAsync(request.FirstVariant.VariantName, request.FirstVariant.VariantDescription, ingredients, cancellationToken);

        // One transaction, so a recipe is never saved without its first variant.
        using var scope = scopeProvider.CreateCoreScope();
        scope.WriteLock(Constants.Locks.ContentTree);
        var recipeKey = await SaveAsync("recipe", request.RecipeName, listing.Key, SubmissionValues.Recipe(request, recipeIcon, authorKey));
        await SaveAsync("recipeVariant", request.FirstVariant.VariantName, recipeKey, SubmissionValues.Variant(request.FirstVariant, variantIcon, authorKey));
        scope.Complete();
        return recipeKey;
    }

    public async Task<Guid?> SubmitVariantAsync(Guid recipeKey, CreateVariantRequest request, Guid authorKey, CancellationToken cancellationToken)
    {
        if (recipes.FindPublishedRecipe(recipeKey) is null)
        {
            return null;
        }

        var icon = await recipeIconService.PickAsync(request.VariantName, request.VariantDescription, SubmissionValues.IngredientNames(request), cancellationToken);
        return await SaveAsync("recipeVariant", request.VariantName, recipeKey, SubmissionValues.Variant(request, icon, authorKey));
    }

    private async Task<Guid> SaveAsync(string contentTypeAlias, string name, Guid parentKey, IEnumerable<PropertyValueModel> values)
    {
        var key = Guid.NewGuid();
        var created = await contentEditingService.CreateAsync(
            new ContentCreateModel
            {
                Key = key,
                ContentTypeKey = contentTypeService.Get(contentTypeAlias)!.Key,
                ParentKey = parentKey,
                Variants = [new VariantModel { Name = name.Trim() }],
                Properties = values,
            },
            Constants.Security.SuperUserKey);

        // A draft missing a mandatory value is still saved, and reports PropertyValidationError; see SubmissionValues.
        if (created.Status != ContentEditingOperationStatus.Success && created.Status != ContentEditingOperationStatus.PropertyValidationError)
        {
            throw new InvalidOperationException($"Saving the submitted {contentTypeAlias} '{name}' failed: {created.Status}.");
        }

        return key;
    }
}
