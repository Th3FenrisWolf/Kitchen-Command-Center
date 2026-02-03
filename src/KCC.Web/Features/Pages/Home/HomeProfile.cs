using AutoMapper;
using CMS.Websites;
using KCC.Web.Features.Pages.Shared;

namespace KCC.Web.Features.Pages.Home;

public class HomeProfile : Profile
{
    public HomeProfile()
    {
        CreateMap<HomePage, HomeViewModel>()
            .IncludeBase<INavigation_Metadata, BasePageViewModel>()
            .IncludeBase<IWebPageFieldsSource, BasePageViewModel>();
    }
}
