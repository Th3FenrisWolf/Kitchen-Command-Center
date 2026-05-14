using KCC.Web.Features.Sections.BentoBox;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterSection(
    BentoBoxSectionViewComponent.Identifier,
    typeof(BentoBoxSectionViewComponent),
    "Bento Box Section",
    typeof(BentoBoxSectionProperties),
    IconClass = "icon-l-grid-2-2"
)]

namespace KCC.Web.Features.Sections.BentoBox;

public class BentoBoxSectionViewComponent : ViewComponent
{
    public const string Identifier = "KCC.BentoBoxSection";

    public IViewComponentResult Invoke(
        ComponentViewModel<BentoBoxSectionProperties> componentViewModel)
    {
        var isEditMode = HttpContext.Kentico().PageBuilder().EditMode;
        var layout = BentoBoxGridLayout.Parse(componentViewModel.Properties.GridLayout);

        var viewModel = new BentoBoxSectionViewModel
        {
            BackgroundColor = componentViewModel.Properties.BackgroundColor,
            ContentWidth = componentViewModel.Properties.ContentWidth,
            PaddingTop = componentViewModel.Properties.GetPaddingTop(),
            PaddingBottom = componentViewModel.Properties.GetPaddingBottom(),
            Rows = layout.Rows,
            Columns = layout.Columns,
            Cells = layout.BuildCells(),
            GridLayoutJson = layout.ToJson(),
            IsEditMode = isEditMode,
            PageId = componentViewModel.Page.WebPageItemID,
            LanguageName = componentViewModel.Page.LanguageName,
        };

        return View("~/Features/Sections/BentoBox/BentoBox.cshtml", viewModel);
    }
}
