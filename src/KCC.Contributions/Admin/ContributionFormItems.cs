using System.Reflection;
using CMS.Helpers;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

namespace KCC.Contributions.Admin;

internal static class ContributionFormItems
{
    internal static void SetName(IFormItem item, string name) =>
        item.GetType()
            .GetProperty("Name", BindingFlags.Public | BindingFlags.Instance)?
            .SetValue(item, name);

    /// <summary>Read-only context field; its name must not match a database column.</summary>
    internal static TextWithLabelComponent Context(string name, string label, string value)
    {
        var component = new TextWithLabelComponent();
        SetName(component, name);
        component.Properties.Label = label;
        component.Properties.EditMode = FormEditMode.ReadOnly;
        component.SetValue(value);
        return component;
    }

    internal static string NormalizeText(string text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    internal static string FormatUtc(DateTime value) =>
        value == DateTimeHelper.ZERO_TIME ? string.Empty : $"{value:yyyy-MM-dd HH:mm} UTC";

    /// <summary>Attaches the error to the field and rebuilds the client form state for a validation-failure response.</summary>
    internal static async Task<FormSubmissionResult> InvalidField(ICollection<IFormItem> items, string fieldName, string message)
    {
        var component = items.OfType<IFormComponent>()
            .FirstOrDefault(c => string.Equals(c.Name, fieldName, StringComparison.OrdinalIgnoreCase));
        component?.AddValidationRule(new ServerFailedValidationRule(message));

        return new FormSubmissionResult(FormSubmissionStatus.ValidationFailure)
        {
            Items = await items.OnlyVisible().GetClientProperties(),
        };
    }
}

internal static class ContributionTitles
{
    internal static string Review(
        IReadOnlyDictionary<Guid, string> contentItemNames,
        IReadOnlyDictionary<Guid, MemberDisplay> members,
        Guid variantGuid,
        Guid memberGuid) =>
        $"Review of {ContentItemNameLookup.DisplayOrDeleted(contentItemNames, variantGuid)} — {MemberNameLookup.DisplayOrDeleted(members, memberGuid)}";

    internal static string Note(
        IReadOnlyDictionary<Guid, string> contentItemNames,
        IReadOnlyDictionary<Guid, MemberDisplay> members,
        Guid variantGuid,
        Guid memberGuid) =>
        $"Note on {ContentItemNameLookup.DisplayOrDeleted(contentItemNames, variantGuid)} — {MemberNameLookup.DisplayOrDeleted(members, memberGuid)}";
}
