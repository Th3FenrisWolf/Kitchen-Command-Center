using Kentico.PageBuilder.Web.Mvc;
using KitchenCommandCenter.Web.Features.Sections.Base;
using KitchenCommandCenter.Web.Features.Sections.BentoBox;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterSection(
    BentoBoxSectionViewComponent.Identifier,
    typeof(BentoBoxSectionViewComponent),
    "Bento Box Section",
    typeof(BentoBoxSectionProperties),
    IconClass = "icon-l-grid-2-2"
)]

namespace KitchenCommandCenter.Web.Features.Sections.BentoBox;

public class BentoBoxSectionViewComponent : ViewComponent
{
    public const string Identifier = "KitchenCommandCenter.BentoBoxSection";

    public IViewComponentResult Invoke(
        ComponentViewModel<BentoBoxSectionProperties> componentViewModel
    )
    {
        var viewModel = new BentoBoxSectionViewModel
        {
            BackgroundColor = componentViewModel.Properties.BackgroundColor,
            ContentWidth = componentViewModel.Properties.ContentWidth,
            PaddingTop = componentViewModel.Properties.GetPaddingTop(),
            PaddingBottom = componentViewModel.Properties.GetPaddingBottom(),
        };

        return View("~/Features/Sections/BentoBox/BentoBox.cshtml", viewModel);
    }
}
