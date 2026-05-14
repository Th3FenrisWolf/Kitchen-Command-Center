using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;

namespace KCC.Web.Features.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class LocalizedHttpGetAttribute : HttpMethodAttribute
{
    private static readonly string[] SupportedMethods = ["GET"];

    public LocalizedHttpGetAttribute(string template)
        : base(SupportedMethods, template)
    {
    }

    public LocalizedHttpGetAttribute()
        : base(SupportedMethods)
    {
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class LocalizedRouteAttribute(string template) : RouteAttribute(template)
{
}

public class LocalizedRouteConvention : IControllerModelConvention, IActionModelConvention
{
    public void Apply(ControllerModel controller)
    {
        if (controller.Attributes.OfType<LocalizedRouteAttribute>().Any())
        {
            AddLanguagePrefixedSelectors(controller.Selectors);
        }
    }

    public void Apply(ActionModel action)
    {
        if (action.Attributes.OfType<LocalizedHttpGetAttribute>().Any())
        {
            AddLanguagePrefixedSelectors(action.Selectors);
        }
    }

    private static void AddLanguagePrefixedSelectors(IList<SelectorModel> selectors)
    {
        foreach (var selector in selectors.ToList())
        {
            if (selector.AttributeRouteModel?.Template is not { } template)
            {
                continue;
            }

            selectors.Add(new SelectorModel(selector)
            {
                AttributeRouteModel = new AttributeRouteModel
                {
                    Template = $"{{lang}}/{template}",
                    Order = selector.AttributeRouteModel.Order,
                    Name = selector.AttributeRouteModel.Name is { } name
                        ? $"{name}_lang"
                        : null
                }
            });
        }
    }
}
