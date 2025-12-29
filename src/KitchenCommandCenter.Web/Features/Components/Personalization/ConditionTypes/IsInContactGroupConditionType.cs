using System.Collections.Generic;
using System.Linq;
using CMS.ContactManagement;
using CMS.DataEngine;
using Kentico.PageBuilder.Web.Mvc.Personalization;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using KitchenCommandCenter.Web.Features.Components.Personalization.ConditionTypes;

[assembly: RegisterPersonalizationConditionType(
    "KitchenCommandCenter.Personalization.IsInContactGroup",
    typeof(IsInContactGroupConditionType),
    "Is in contact group",
    Description = "Evaluates if the current contact is in one of the contact groups.",
    IconClass = "icon-app-contact-groups",
    Hint = "Display to visitors who match at least one of the selected contact groups:"
)]

namespace KitchenCommandCenter.Web.Features.Components.Personalization.ConditionTypes;

public class IsInContactGroupConditionType : ConditionType
{
    [ObjectSelectorComponent(
        PredefinedObjectType.CONTACTGROUP,
        Label = "Contact groups",
        Order = 0,
        MaximumItems = 0
    )]
    public IEnumerable<ObjectRelatedItem> SelectedContactGroups { get; set; } = [];

    public override bool Evaluate()
    {
        var contact = ContactManagementContext.GetCurrentContact();
        if (contact == null)
        {
            return false;
        }

        if (SelectedContactGroups == null || !SelectedContactGroups.Any())
        {
            return contact.ContactGroups.Count == 0;
        }

        return contact.IsInAnyContactGroup(
            SelectedContactGroups.Select(c => c.ObjectCodeName).ToArray()
        );
    }
}
