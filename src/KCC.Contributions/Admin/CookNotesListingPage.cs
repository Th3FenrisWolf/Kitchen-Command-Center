using CMS.Helpers;
using CMS.Membership;
using KCC.Contributions.Admin;
using KCC.Contributions.Admin.Filters;
using KCC.Contributions.Data;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(ContributionsApplication),
    slug: "cook-notes",
    uiPageType: typeof(CookNotesListingPage),
    name: "Cook Notes",
    templateName: TemplateNames.LISTING,
    order: 2
)]

namespace KCC.Contributions.Admin;

public class CookNotesListingPage(ContentItemNameLookup contentItemNameLookup, MemberNameLookup memberNameLookup) : ListingPage
{
    private IReadOnlyDictionary<Guid, string> contentItemNames = new Dictionary<Guid, string>();
    private IReadOnlyDictionary<Guid, MemberDisplay> members = new Dictionary<Guid, MemberDisplay>();

    protected override string ObjectType => VariantCookNoteInfo.OBJECT_TYPE;

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        contentItemNames = contentItemNameLookup.DisplayNames();
        members = memberNameLookup.Displays();

        PageConfiguration.FilterConfiguration.FormModel = new CookNotesFilterModel();

        _ = PageConfiguration.AddEditRowAction<CookNotesEditPage>();

        _ = PageConfiguration
            .ColumnConfigurations
            .AddColumn(nameof(VariantCookNoteInfo.RecipeGuid), "Recipe", sortable: false,
                formatter: (value, _) => ContentItemNameLookup.DisplayOrDeleted(contentItemNames, ValidationHelper.GetGuid(value, Guid.Empty)))
            .AddColumn(nameof(VariantCookNoteInfo.VariantGuid), "Variant", sortable: false,
                formatter: (value, _) => ContentItemNameLookup.DisplayOrDeleted(contentItemNames, ValidationHelper.GetGuid(value, Guid.Empty)))
            .AddColumn(nameof(VariantCookNoteInfo.MemberGuid), "Member", sortable: false,
                formatter: (value, _) => MemberNameLookup.DisplayOrDeleted(members, ValidationHelper.GetGuid(value, Guid.Empty)))
            .AddColumn(nameof(VariantCookNoteInfo.NoteText), "Note", searchable: true)
            .AddColumn(nameof(VariantCookNoteInfo.NoteCreated), "Date", defaultSortDirection: SortTypeEnum.Desc);

        _ = PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
    }
}
