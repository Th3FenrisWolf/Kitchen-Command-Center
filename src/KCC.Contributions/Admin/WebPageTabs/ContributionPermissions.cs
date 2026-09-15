namespace KCC.Contributions.Admin.WebPageTabs;

/// <remarks>
/// Declared on <see cref="ContributionTabsExtender"/> rather than on the tab pages that evaluate it.
/// <c>ApplicationStore.GetPermissionsForApplication</c> reads permission attributes from the
/// application type and from its extenders and from nowhere else, so a permission declared on a
/// descendant page can never be granted to a role and evaluates false for everyone but an
/// administrator.
///
/// The name is KCC's own because the Web pages application already spends <c>Read</c>, <c>Create</c>,
/// <c>Update</c> and <c>Delete</c> on web page ACLs; reusing one would put two unrelated permissions
/// behind a single label in the role editor.
/// </remarks>
public static class ContributionPermissions
{
    public const string ViewContributions = "KCCViewContributions";

    public const string ManageContributions = "KCCManageContributions";
}
