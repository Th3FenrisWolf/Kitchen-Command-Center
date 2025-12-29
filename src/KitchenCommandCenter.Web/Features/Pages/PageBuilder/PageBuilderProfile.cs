using AutoMapper;
using CMS.Websites;
using KitchenCommandCenter.Web.Features.Pages.Shared;

namespace KitchenCommandCenter.Web.Features.Pages.PageBuilder;

public class PageBuilderProfile : Profile
{
    public PageBuilderProfile()
    {
        CreateMap<PageBuilderPage, PageBuilderViewModel>()
            .IncludeBase<INavigation_Metadata, BasePageViewModel>()
            .IncludeBase<IWebPageFieldsSource, BasePageViewModel>();
    }
}
