using System.Reflection;
using KCC.Contributions;
using KCC.Contributions.Admin;
using KCC.Contributions.Admin.WebPageTabs;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Websites.UIPages;

namespace KCC.UnitTests.Features.Contributions;

/// <remarks>
/// These assert the wiring rather than behaviour, because each fault below is one a passing build and
/// a developer's own session would not show: an administrator satisfies every permission, and a tab
/// that fails to register simply does not appear.
/// </remarks>
public class ContributionTabRegistrationTests
{
    private static readonly UIPageAttribute[] Registrations =
        [..typeof(RecipeContributionsPage).Assembly.GetCustomAttributes<UIPageAttribute>()];

    /// <remarks>
    /// The tab feature cannot see the generated content-type classes: they live in KCC.Web, which
    /// references KCC.Contributions. A rename there would leave the extender matching a name nothing
    /// carries, and both tabs would stop appearing with no error anywhere.
    /// </remarks>
    [Test]
    public async Task ContentTypeNames_MatchTheGeneratedClasses()
    {
        string[] declared = [ContributionContentTypes.Recipe, ContributionContentTypes.RecipeVariant];
        string[] generated = [KCC.Recipe.CONTENT_TYPE_NAME, KCC.RecipeVariant.CONTENT_TYPE_NAME];

        _ = await Assert.That(declared).IsEquivalentTo(generated);
    }

    [Test]
    [Arguments(typeof(RecipeContributionsPage))]
    [Arguments(typeof(VariantContributionsPage))]
    public async Task TabPage_IsRegisteredUnderTheWebPageLayout(Type pageType)
    {
        var registration = Registrations.SingleOrDefault(attribute => attribute.Type == pageType);

        _ = await Assert.That(registration).IsNotNull();
        _ = await Assert.That(registration!.ParentType).IsEqualTo(typeof(WebPageLayout));
    }

    /// <remarks>
    /// Four places have to agree for the bundle to publish where a template looks for it, none of them
    /// can see the others, and a mismatch renders an error boundary with an empty event log.
    /// </remarks>
    [Test]
    [Arguments(typeof(RecipeContributionsPage))]
    [Arguments(typeof(VariantContributionsPage))]
    public async Task TabPage_NamesATemplateFromThisModule(Type pageType)
    {
        var templateName = Registrations.Single(attribute => attribute.Type == pageType).TemplateName;

        _ = await Assert.That(templateName)
            .StartsWith($"@{ContributionsModule.OrgName}/{ContributionsModule.ProjectName}/");
    }

    /// <remarks>
    /// <c>ApplicationStore.GetPermissionsForApplication</c> reads permission attributes from the
    /// application type and from its extenders and from nowhere else. Moving this declaration onto the
    /// page that evaluates it compiles and runs, and leaves a permission no role can be granted —
    /// which every non-administrator then fails.
    /// </remarks>
    [Test]
    public async Task ViewPermission_IsDeclaredOnTheExtender()
    {
        var declared = typeof(ContributionTabsExtender)
            .GetCustomAttributes<UIPermissionAttribute>()
            .Select(attribute => attribute.Name);

        _ = await Assert.That(declared).Contains(ContributionPermissions.ViewContributions);
    }

    [Test]
    [Arguments(typeof(RecipeContributionsPage))]
    [Arguments(typeof(VariantContributionsPage))]
    public async Task TabPage_EvaluatesTheViewPermission(Type pageType)
    {
        var evaluated = pageType
            .GetCustomAttributes<UIEvaluatePermissionAttribute>()
            .Select(attribute => attribute.Permission);

        _ = await Assert.That(evaluated).Contains(ContributionPermissions.ViewContributions);
    }

    /// <remarks>
    /// The READ ACL on the page is what keeps member names and free text written by the public off the
    /// screen of a user who may not read the page. <c>PageInvoker</c> activates only the routed node,
    /// so the layout's own check never runs for a child tab and the base class is the only thing
    /// performing it.
    /// </remarks>
    [Test]
    [Arguments(typeof(RecipeContributionsPage))]
    [Arguments(typeof(VariantContributionsPage))]
    public async Task TabPage_DerivesFromWebPageBase(Type pageType)
    {
        var derivesFromWebPageBase = Ancestors(pageType)
            .Any(ancestor => ancestor.IsGenericType && ancestor.GetGenericTypeDefinition() == typeof(WebPageBase<>));

        _ = await Assert.That(derivesFromWebPageBase).IsTrue();
    }

    [Test]
    [Arguments(typeof(VariantReviewEditPage), typeof(VariantReviewEditSection))]
    [Arguments(typeof(VariantCookNoteEditPage), typeof(VariantCookNoteEditSection))]
    public async Task EditPage_IsRoutedBeneathItsEntrySection(Type pageType, Type sectionType)
    {
        var registration = Registrations.SingleOrDefault(attribute => attribute.Type == pageType);

        _ = await Assert.That(registration).IsNotNull();
        _ = await Assert.That(registration!.ParentType).IsEqualTo(sectionType);
    }

    /// <remarks>
    /// Two parameterized siblings under the tab would be indistinguishable in the URL, so each entry
    /// type is reached through a static segment first.
    /// </remarks>
    [Test]
    [Arguments(typeof(VariantReviewEditSection), typeof(VariantReviewsSection))]
    [Arguments(typeof(VariantCookNoteEditSection), typeof(VariantCookNotesSection))]
    public async Task EntrySection_IsParameterizedBeneathAStaticSegmentOfTheTab(Type sectionType, Type parentType)
    {
        var section = Registrations.Single(attribute => attribute.Type == sectionType);
        var parent = Registrations.Single(attribute => attribute.Type == parentType);

        _ = await Assert.That(section.ParentType).IsEqualTo(parentType);
        _ = await Assert.That(section.Slug).IsEqualTo(PageParameterConstants.PARAMETERIZED_SLUG);
        _ = await Assert.That(parent.ParentType).IsEqualTo(typeof(VariantContributionsPage));
        _ = await Assert.That(parent.Slug).IsNotEqualTo(PageParameterConstants.PARAMETERIZED_SLUG);
    }

    /// <remarks>
    /// The inherited commands are declared against <c>Update</c>, which the Web pages application
    /// spends on a web page ACL rather than a grantable UI permission — so without the override every
    /// save fails for everyone but an administrator, and a developer's own session would never show it.
    /// </remarks>
    [Test]
    [Arguments(typeof(VariantReviewEditPage), "Submit")]
    [Arguments(typeof(VariantReviewEditPage), "Change")]
    [Arguments(typeof(VariantCookNoteEditPage), "Submit")]
    [Arguments(typeof(VariantCookNoteEditPage), "Change")]
    public async Task EditPage_DeclaresItsFormCommandsAgainstTheManagePermission(Type pageType, string commandName)
    {
        var declared = pageType
            .GetMethod(commandName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            ?.GetCustomAttribute<PageCommandAttribute>()
            ?.Permission;

        _ = await Assert.That(declared).IsEqualTo(ContributionPermissions.ManageContributions);
    }

    [Test]
    public async Task ManagePermission_IsDeclaredOnTheExtender()
    {
        var declared = typeof(ContributionTabsExtender)
            .GetCustomAttributes<UIPermissionAttribute>()
            .Select(attribute => attribute.Name);

        _ = await Assert.That(declared).Contains(ContributionPermissions.ManageContributions);
    }

    /// <remarks>
    /// The form is mounted under the application and under the tab. Both deriving from the one base is
    /// what stops the two copies drifting apart on a validation rule or a normalization.
    /// </remarks>
    [Test]
    [Arguments(typeof(VariantReviewEditPage), typeof(ReviewEditPageBase))]
    [Arguments(typeof(ReviewsEditPage), typeof(ReviewEditPageBase))]
    [Arguments(typeof(VariantCookNoteEditPage), typeof(CookNoteEditPageBase))]
    [Arguments(typeof(CookNotesEditPage), typeof(CookNoteEditPageBase))]
    public async Task EditPage_SharesTheOneFormDefinition(Type pageType, Type formBase)
    {
        _ = await Assert.That(Ancestors(pageType)).Contains(formBase);
    }

    private static IEnumerable<Type> Ancestors(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            yield return current;
        }
    }
}
