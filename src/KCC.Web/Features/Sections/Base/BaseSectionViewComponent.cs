using KCC.Web.Features.Sections.Base;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterSection(
    BaseSectionViewComponent.Identifier,
    typeof(BaseSectionViewComponent),
    "Base Section",
    typeof(BaseSectionProperties),
    IconClass = "icon-square"
)]

namespace KCC.Web.Features.Sections.Base;

public class BaseSectionViewComponent : ViewComponent
{
    public const string Identifier = "KCC.BaseSection";

    public IViewComponentResult Invoke(
        ComponentViewModel<BaseSectionProperties> componentViewModel
    )
    {
        var viewModel = new BaseSectionViewModel
        {
            Heading = componentViewModel.Properties.Heading,
            BackgroundColor = componentViewModel.Properties.BackgroundColor,
            TextColor = componentViewModel.Properties.TextColor,
            ContentWidth = componentViewModel.Properties.ContentWidth,
            PaddingTop = componentViewModel.Properties.GetPaddingTop(),
            PaddingBottom = componentViewModel.Properties.GetPaddingBottom(),
            HorizontalPadding = componentViewModel.Properties.GetHorizontalPadding(),
        };

        return View("~/Features/Sections/Base/BaseSection.cshtml", viewModel);
    }
}
