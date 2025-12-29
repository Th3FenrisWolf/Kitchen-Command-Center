using AutoMapper;
using CMS.Websites;
using KitchenCommandCenter.Web.Features.Pages.Shared;

namespace KitchenCommandCenter.Web.Features.Pages.Home;

public class HomeProfile : Profile
{
    public HomeProfile()
    {
        CreateMap<HomePage, HomeViewModel>()
            .IncludeBase<INavigation_Metadata, BasePageViewModel>()
            .IncludeBase<IWebPageFieldsSource, BasePageViewModel>();
    }
}
