using KCC.Web.Features.Sections.MultipleColumn;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterSection(
    MultipleColumnSectionViewComponent.Identifier,
    typeof(MultipleColumnSectionViewComponent),
    "Multiple Column Section",
    typeof(MultipleColumnSectionProperties),
    IconClass = "icon-l-cols-3"
)]

namespace KCC.Web.Features.Sections.MultipleColumn;

public class MultipleColumnSectionViewComponent : ViewComponent
{
    public const string Identifier = "KCC.MultipleColumnSection";

    public IViewComponentResult Invoke(
        ComponentViewModel<MultipleColumnSectionProperties> componentViewModel
    )
    {
        var (columnStyleClass, columnCount) = componentViewModel.Properties.GetColumnStyle();

        var viewModel = new MultipleColumnSectionViewModel
        {
            SectionClass = columnStyleClass,
            ColumnCount = columnCount,
            Heading = componentViewModel.Properties.Heading,
            BackgroundColor = componentViewModel.Properties.BackgroundColor,
            TextColor = componentViewModel.Properties.TextColor,
            ContentWidth = componentViewModel.Properties.ContentWidth,
            PaddingTop = componentViewModel.Properties.GetPaddingTop(),
            PaddingBottom = componentViewModel.Properties.GetPaddingBottom(),
            HorizontalPadding = componentViewModel.Properties.GetHorizontalPadding(),
        };

        return View("~/Features/Sections/MultipleColumn/MultipleColumnSection.cshtml", viewModel);
    }
}
