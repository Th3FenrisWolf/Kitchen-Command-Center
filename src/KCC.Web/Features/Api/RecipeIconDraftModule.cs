using CMS;
using CMS.Base;
using CMS.Core;
using CMS.DataEngine;
using CMS.Websites;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: RegisterModule(typeof(KCC.Web.Features.Api.RecipeIconDraftModule))]

namespace KCC.Web.Features.Api;

/// <summary>
/// Registers the handler that auto-assigns an icon to a recipe when its draft is saved without one.
/// </summary>
public class RecipeIconDraftModule : Module
{
    /// <summary>Initializes a new instance of the <see cref="RecipeIconDraftModule"/> class.</summary>
    public RecipeIconDraftModule()
        : base(nameof(RecipeIconDraftModule))
    {
    }

    /// <inheritdoc />
    protected override void OnPreInit(ModulePreInitParameters parameters)
    {
        base.OnPreInit(parameters);
        parameters.Services.AddEventHandler<BeforeUpdateWebPageDraftEvent, RecipeIconDraftHandler>();
    }
}

/// <summary>
/// Sets <c>KCC.Recipe.Icon</c> from <see cref="IRecipeIconService"/> when a recipe draft is saved with no icon yet.
/// </summary>
public class RecipeIconDraftHandler(
    IRecipeIconService recipeIconService,
    ILogger<RecipeIconDraftHandler> logger
) : IAsyncEventHandler<BeforeUpdateWebPageDraftEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(BeforeUpdateWebPageDraftEvent asyncEvent, CancellationToken cancellationToken)
    {
        try
        {
            var data = asyncEvent.Data;
            if (!string.Equals(data.ContentTypeName, KCC.Recipe.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var itemData = data.ContentItemData;

            itemData.TryGetValue<string>(nameof(KCC.Recipe.Icon), out string existingIcon);
            if (!string.IsNullOrEmpty(existingIcon))
            {
                return;
            }

            itemData.TryGetValue<string>(nameof(KCC.Recipe.Name), out string name);
            itemData.TryGetValue<string>(nameof(KCC.Recipe.Description), out string description);

            string icon = await recipeIconService.PickAsync(name, description, Array.Empty<string>(), cancellationToken);
            itemData.SetValue(nameof(KCC.Recipe.Icon), icon);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to auto-assign recipe icon on draft save.");
        }
    }
}
