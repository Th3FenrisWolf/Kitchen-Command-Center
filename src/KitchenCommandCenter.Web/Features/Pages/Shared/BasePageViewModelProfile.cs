using AutoMapper;
using CMS.Websites;

namespace KitchenCommandCenter.Web.Features.Pages.Shared;

public class BasePageViewModelProfile : Profile
{
    public BasePageViewModelProfile()
    {
        // TODO: See if there's a better way to handle the situation where consumers need to .IncludeBase() both of these individually
        // Doing them within each other didn't seem to work (i.e. adding .IncludeBase() to the Navigation map to include web page fields source)
        CreateMap<INavigation_Metadata, BasePageViewModel>()
            .ForMember(dest => dest.Title, x => x.MapFrom(src => src.MetadataTitle))
            .ForMember(dest => dest.Description, x => x.MapFrom(src => src.MetadataDescription))
            .ForMember(dest => dest.Keywords, x => x.MapFrom(src => src.MetadataKeywords))
            .ForMember(dest => dest.PublishDate, x => x.MapFrom(src => src.MetadataPublishDate))
            .AfterMap<SeoImageAction>();

        CreateMap<IWebPageFieldsSource, BasePageViewModel>()
            .ForMember(dest => dest.WebPageItemID, x => x.MapFrom(src => src.SystemFields.WebPageItemID));
    }
}
