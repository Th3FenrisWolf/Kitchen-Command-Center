using CMS.Core;
using KCC.Contributions.Data;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

namespace KCC.Contributions.Admin;

/// <remarks>
/// Mounted twice: once under the Community Contributions application and once as a child of a recipe
/// variant's Contributions tab. Only the page parameter the object id binds from differs, so
/// everything else lives here and neither copy can drift from the other.
/// </remarks>
public abstract class ReviewEditPageBase(
    IFormComponentMapper formComponentMapper,
    IFormDataBinder formDataBinder,
    ILocalizationService localizationService,
    ContentItemNameLookup contentItemNameLookup,
    MemberNameLookup memberNameLookup)
    : InfoEditPage<VariantReviewInfo>(formComponentMapper, formDataBinder)
{
    private const string RatingError = "Rating must be between 0.5 and 5 in half-star steps.";

    /// <summary>
    /// Whether the recipe and variant the review belongs to need naming on the form. The tab is
    /// already standing on that variant's own page, so there they only repeat the breadcrumbs.
    /// </summary>
    protected virtual bool ShowContentContext => true;

    protected override async Task<ICollection<IFormItem>> GetFormItems()
    {
        var review = await GetInfoObject();
        var contentItemNames = contentItemNameLookup.DisplayNames();
        var members = memberNameLookup.Displays();

        var rating = new DecimalNumberInputComponent(localizationService);
        ContributionFormItems.SetName(rating, nameof(VariantReviewInfo.Rating));
        rating.Properties.Label = "Rating";
        rating.Properties.ExplanationText = "0.5 to 5 in half-star steps.";
        rating.AddValidationRule(new RequiredValidationRule(localizationService));

        var minimum = new MinimumDecimalValueValidationRule(localizationService);
        minimum.Properties.MinValue = 0.5m;
        minimum.Properties.ErrorMessage = RatingError;
        rating.AddValidationRule(minimum);

        var maximum = new MaximumDecimalValueValidationRule(localizationService);
        maximum.Properties.MaxValue = 5m;
        maximum.Properties.ErrorMessage = RatingError;
        rating.AddValidationRule(maximum);

        var reviewText = new TextAreaComponent(localizationService);
        ContributionFormItems.SetName(reviewText, nameof(VariantReviewInfo.ReviewText));
        reviewText.Properties.Label = "Review";
        reviewText.Properties.MinRowsNumber = 4;
        reviewText.Properties.MaxRowsNumber = 12;

        var maxLength = new MaxLengthValidationRule(localizationService);
        maxLength.Properties.MaxLength = 4000;
        reviewText.AddValidationRule(maxLength);

        List<IFormItem> items = [];

        if (ShowContentContext)
        {
            items.Add(ContributionFormItems.Context("RecipeContext", "Recipe", ContentItemNameLookup.DisplayOrDeleted(contentItemNames, review.RecipeGuid)));
            items.Add(ContributionFormItems.Context("VariantContext", "Variant", ContentItemNameLookup.DisplayOrDeleted(contentItemNames, review.VariantGuid)));
        }

        items.Add(ContributionFormItems.Context("MemberContext", "Member", MemberNameLookup.DisplayWithEmail(members, review.MemberGuid)));
        items.Add(ContributionFormItems.Context("CreatedContext", "Created", ContributionFormItems.FormatUtc(review.ReviewCreated)));
        items.Add(ContributionFormItems.Context("ModifiedContext", "Modified", ContributionFormItems.FormatUtc(review.ReviewModified)));
        items.Add(rating);
        items.Add(reviewText);

        return items;
    }

    protected override async Task<ICommandResponse> SubmitInternal(
        FormSubmissionCommandArguments args,
        ICollection<IFormItem> items,
        IFormFieldValueProvider formFieldValueProvider)
    {
        _ = formFieldValueProvider.TryGet<decimal?>(nameof(VariantReviewInfo.Rating), out var rating);
        if (rating is not { } value || !VariantReviewInfoProvider.IsValidRating(value))
        {
            return ResponseFrom(await ContributionFormItems.InvalidField(items, nameof(VariantReviewInfo.Rating), RatingError))
                .AddErrorMessage(RatingError);
        }

        return await base.SubmitInternal(args, items, formFieldValueProvider);
    }

    protected override Task FinalizeInfoObject(
        VariantReviewInfo infoObject,
        IFormFieldValueProvider fieldValueProvider,
        CancellationToken cancellationToken)
    {
        infoObject.ReviewText = ContributionFormItems.NormalizeText(infoObject.ReviewText);
        infoObject.ReviewModified = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    protected override Task<string> GetObjectDisplayName(VariantReviewInfo infoObject) =>
        Task.FromResult(ContributionTitles.Review(
            contentItemNameLookup.DisplayNames(), memberNameLookup.Displays(), infoObject.VariantGuid, infoObject.MemberGuid));
}
