using CMS.ContentEngine;
using KCC;
using KCC.Web.Features.Extensions;
using Kentico.Content.Web.Mvc;

namespace KCC.Web.Features.Api;

/// <summary>Resolves a variant content-item GUID to its parent recipe content-item GUID.</summary>
public interface IVariantGuidResolver
{
    /// <summary>Returns the parent recipe's content-item GUID, or null when the variant is unknown.</summary>
    /// <param name="variantGuid">The variant's content-item GUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parent recipe's content-item GUID, or null when the variant is unknown.</returns>
    Task<Guid?> ResolveRecipeGuidAsync(Guid variantGuid, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public class VariantGuidResolver(IContentRetriever contentRetriever) : IVariantGuidResolver
{
    /// <inheritdoc />
    public async Task<Guid?> ResolveRecipeGuidAsync(Guid variantGuid, CancellationToken cancellationToken = default)
    {
        var variant = (await contentRetriever.RetrievePages<RecipeVariant>(
            new(),
            query => query
                .Where(where => where
                    .WhereEquals(nameof(IContentQueryDataContainer.ContentItemGUID), variantGuid))
                .TopN(1),
            new($"{nameof(VariantGuidResolver)}|{variantGuid}"))).FirstOrDefault();

        if (variant is null)
        {
            return null;
        }

        var parentId = variant.SystemFields.WebPageItemParentID;
        var recipe = await contentRetriever.RetrievePage<Recipe>(parentId);

        return recipe?.SystemFields.ContentItemGUID;
    }
}
