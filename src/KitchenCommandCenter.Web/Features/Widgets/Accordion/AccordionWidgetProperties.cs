using System.Collections.Generic;
using CMS.ContentEngine;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using KitchenCommandCenter.Web.Features.Widgets.Base;

namespace KitchenCommandCenter.Web.Features.Widgets.Accordion;

public class AccordionWidgetProperties : BaseWidgetProperties, IWidgetProperties
{
    [ContentItemSelectorComponent(
        KitchenCommandCenter.AccordionItem.CONTENT_TYPE_NAME,
        Order = 0,
        Label = "Accordion Items",
        ExplanationText = "Select the accordion items you want to be displayed"
    )]
    public IEnumerable<ContentItemReference> AccordionItems { get; set; }
}
