using Kentico.PageBuilder.Web.Mvc;
using KitchenCommandCenter.Web.Features.Sections.Base;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterSection(
    BaseSectionViewComponent.Identifier,
    typeof(BaseSectionViewComponent),
    "Base Section",
    typeof(BaseSectionProperties),
    IconClass = "icon-square"
)]

namespace KitchenCommandCenter.Web.Features.Sections.Base;

public class BaseSectionViewComponent : ViewComponent
{
    public const string Identifier = "KitchenCommandCenter.BaseSection";

    public IViewComponentResult Invoke(
        ComponentViewModel<BaseSectionProperties> componentViewModel
    )
    {
        var viewModel = new BaseSectionViewModel
        {
            BackgroundColor = componentViewModel.Properties.BackgroundColor,
            ContentWidth = componentViewModel.Properties.ContentWidth,
            PaddingTop = componentViewModel.Properties.GetPaddingTop(),
            PaddingBottom = componentViewModel.Properties.GetPaddingBottom(),
        };

        return View("~/Features/Sections/Base/BaseSection.cshtml", viewModel);
    }
}
