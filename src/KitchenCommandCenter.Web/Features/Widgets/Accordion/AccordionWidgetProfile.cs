using AutoMapper;

namespace KitchenCommandCenter.Web.Features.Widgets.Accordion;

public class AccordionWidgetProfile : Profile
{
    public AccordionWidgetProfile() =>
        CreateMap<KitchenCommandCenter.AccordionItem, AccordionItem>();
}
