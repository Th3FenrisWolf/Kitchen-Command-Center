using AutoMapper;
using CMS.Websites;
using KCC.Web.Features.Pages.Shared;

namespace KCC.Web.Features.Pages.PageBuilder;

public class PageBuilderProfile : Profile
{
    public PageBuilderProfile()
    {
        CreateMap<PageBuilderPage, PageBuilderViewModel>()
            .IncludeBase<INavigation_Metadata, BasePageViewModel>()
            .IncludeBase<IWebPageFieldsSource, BasePageViewModel>();
    }
}
