using AutoMapper;

namespace KCC.Web.Features.Pages.Error;

public class ErrorProfile : Profile
{
    public ErrorProfile()
    {
        CreateMap<StatusCodePage, ErrorViewModel>()
            .ForMember(dest => dest.Heading, x => x.MapFrom(src => src.StatusCodeHeading))
            .ForMember(dest => dest.Body, x => x.MapFrom(src => src.StatusCodeBody))
            .ForMember(dest => dest.Title, x => x.MapFrom(src => src.StatusCodeHeading));
    }
}
