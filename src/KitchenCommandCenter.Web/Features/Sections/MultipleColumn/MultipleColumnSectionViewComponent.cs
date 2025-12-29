using Kentico.PageBuilder.Web.Mvc;
using KitchenCommandCenter.Web.Features.Sections.MultipleColumn;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterSection(
    MultipleColumnSectionViewComponent.Identifier,
    typeof(MultipleColumnSectionViewComponent),
    "Multiple Column Section",
    typeof(MultipleColumnSectionProperties),
    IconClass = "icon-l-cols-3"
)]

namespace KitchenCommandCenter.Web.Features.Sections.MultipleColumn;

public class MultipleColumnSectionViewComponent : ViewComponent
{
    public const string Identifier = "KitchenCommandCenter.MultipleColumnSection";

    public IViewComponentResult Invoke(
        ComponentViewModel<MultipleColumnSectionProperties> componentViewModel
    )
    {
        var (columnStyleClass, columnCount) = componentViewModel.Properties.GetColumnStyle();

        var viewModel = new MultipleColumnSectionViewModel
        {
            SectionClass = columnStyleClass,
            ColumnCount = columnCount,
            BackgroundColor = componentViewModel.Properties.BackgroundColor,
            ContentAlignment = componentViewModel.Properties.ContentAlignment,
            ContentWidth = componentViewModel.Properties.ContentWidth,
            PaddingTop = componentViewModel.Properties.GetPaddingTop(),
            PaddingBottom = componentViewModel.Properties.GetPaddingBottom(),
        };

        return View("~/Features/Sections/MultipleColumn/MultipleColumnSection.cshtml", viewModel);
    }
}
