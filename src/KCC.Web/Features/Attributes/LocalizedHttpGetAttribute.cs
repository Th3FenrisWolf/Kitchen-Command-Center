using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;

namespace KCC.Web.Features.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class LocalizedRouteAttribute(string template) : RouteAttribute(template) { }

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class LocalizedHttpGetAttribute : HttpMethodAttribute
{
    private static readonly string[] SupportedMethods = ["GET"];

    public LocalizedHttpGetAttribute(string template)
        : base(SupportedMethods, template) { }

    public LocalizedHttpGetAttribute()
        : base(SupportedMethods) { }
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
            if (selector.AttributeRouteModel?.Template is not string template)
            {
                continue;
            }

            var model = new AttributeRouteModel()
            {
                Template = $"{{lang}}/{template}",
                Order = selector.AttributeRouteModel.Order,
                Name = selector.AttributeRouteModel.Name is string name
                    ? $"Localized_{name}"
                    : null
            };

            selectors.Add(new(selector) { AttributeRouteModel = model });
        }
    }
}
