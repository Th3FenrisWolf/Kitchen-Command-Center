using CMS.Membership;
using KCC.Contributions.Admin;
using KCC.Contributions.Data;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(ContributionsApplication),
    slug: "cook-notes",
    uiPageType: typeof(CookNotesListingPage),
    name: "Cook Notes",
    templateName: TemplateNames.LISTING,
    order: 1
)]

namespace KCC.Contributions.Admin;

public class CookNotesListingPage : ListingPage
{
    protected override string ObjectType => VariantCookNoteInfo.OBJECT_TYPE;

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        _ = PageConfiguration.AddEditRowAction<CookNotesEditPage>();

        _ = PageConfiguration
            .ColumnConfigurations
            .AddColumn(nameof(VariantCookNoteInfo.VariantGuid), "Variant", searchable: true)
            .AddColumn(nameof(VariantCookNoteInfo.MemberGuid), "Member", searchable: true)
            .AddColumn(nameof(VariantCookNoteInfo.NoteText), "Note")
            .AddColumn(nameof(VariantCookNoteInfo.NoteCreated), "Date", defaultSortDirection: SortTypeEnum.Desc);

        _ = PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
    }
}
