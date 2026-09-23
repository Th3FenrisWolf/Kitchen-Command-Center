using CMS.Core;
using KCC.Contributions.Data;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

namespace KCC.Contributions.Admin;

/// <remarks>
/// Mounted twice, for the reason <see cref="ReviewEditPageBase"/> records.
/// </remarks>
public abstract class CookNoteEditPageBase(
    IFormComponentMapper formComponentMapper,
    IFormDataBinder formDataBinder,
    ILocalizationService localizationService,
    ContentItemNameLookup contentItemNameLookup,
    MemberNameLookup memberNameLookup)
    : InfoEditPage<VariantCookNoteInfo>(formComponentMapper, formDataBinder)
{
    private const string NoteError = "Note text is required.";

    /// <inheritdoc cref="ReviewEditPageBase.ShowContentContext"/>
    protected virtual bool ShowContentContext => true;

    protected override async Task<ICollection<IFormItem>> GetFormItems()
    {
        var note = await GetInfoObject();
        var contentItemNames = contentItemNameLookup.DisplayNames();
        var members = memberNameLookup.Displays();

        var noteText = new TextAreaComponent(localizationService);
        ContributionFormItems.SetName(noteText, nameof(VariantCookNoteInfo.NoteText));
        noteText.Properties.Label = "Note";
        noteText.Properties.MinRowsNumber = 4;
        noteText.Properties.MaxRowsNumber = 12;
        noteText.AddValidationRule(new RequiredValidationRule(localizationService));

        var maxLength = new MaxLengthValidationRule(localizationService);
        maxLength.Properties.MaxLength = 4000;
        noteText.AddValidationRule(maxLength);

        List<IFormItem> items = [];

        if (ShowContentContext)
        {
            items.Add(ContributionFormItems.Context("RecipeContext", "Recipe", ContentItemNameLookup.DisplayOrDeleted(contentItemNames, note.RecipeGuid)));
            items.Add(ContributionFormItems.Context("VariantContext", "Variant", ContentItemNameLookup.DisplayOrDeleted(contentItemNames, note.VariantGuid)));
        }

        items.Add(ContributionFormItems.Context("MemberContext", "Member", MemberNameLookup.DisplayWithEmail(members, note.MemberGuid)));
        items.Add(ContributionFormItems.Context("CreatedContext", "Created", ContributionFormItems.FormatUtc(note.NoteCreated)));
        items.Add(ContributionFormItems.Context("ModifiedContext", "Modified", ContributionFormItems.FormatUtc(note.NoteModified)));
        items.Add(noteText);

        return items;
    }

    protected override async Task<ICommandResponse> SubmitInternal(
        FormSubmissionCommandArguments args,
        ICollection<IFormItem> items,
        IFormFieldValueProvider formFieldValueProvider)
    {
        _ = formFieldValueProvider.TryGet<string>(nameof(VariantCookNoteInfo.NoteText), out var noteText);
        if (string.IsNullOrWhiteSpace(noteText))
        {
            return ResponseFrom(await ContributionFormItems.InvalidField(items, nameof(VariantCookNoteInfo.NoteText), NoteError))
                .AddErrorMessage(NoteError);
        }

        return await base.SubmitInternal(args, items, formFieldValueProvider);
    }

    protected override Task FinalizeInfoObject(
        VariantCookNoteInfo infoObject,
        IFormFieldValueProvider fieldValueProvider,
        CancellationToken cancellationToken)
    {
        infoObject.NoteText = ContributionFormItems.NormalizeText(infoObject.NoteText);
        infoObject.NoteModified = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    protected override Task<string> GetObjectDisplayName(VariantCookNoteInfo infoObject) =>
        Task.FromResult(ContributionTitles.Note(
            contentItemNameLookup.DisplayNames(), memberNameLookup.Displays(), infoObject.VariantGuid, infoObject.MemberGuid));
}
