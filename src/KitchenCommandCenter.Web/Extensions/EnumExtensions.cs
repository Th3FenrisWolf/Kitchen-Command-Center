using System;
using KitchenCommandCenter.Web.Features.Tailwind;

namespace KitchenCommandCenter.Web.Extensions;

public static class EnumExtensions
{
    public static string GetTailwindStyle(this Enum value)
    {
        var type = value.GetType();
        var name = Enum.GetName(type, value);

        if (name == null)
        {
            return null;
        }

        var field = type.GetField(name);

        return field != null
            ? Attribute.GetCustomAttribute(field, typeof(TailwindStyleAttribute))
                is TailwindStyleAttribute attribute
                ? attribute.GetTailwindStyle()
                : null
            : null;
    }
}
