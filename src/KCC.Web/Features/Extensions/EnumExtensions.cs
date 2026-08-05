using KCC.Web.Features.Tailwind;

namespace KCC.Web.Features.Extensions;

public static class EnumExtensions
{
    public static string GetTailwindStyle(this Enum value)
    {
        var type = value.GetType();
        var name = Enum.GetName(type, value);

        if (name is null)
        {
            return string.Empty;
        }

        var field = type.GetField(name);

        return field is not null
            ? Attribute.GetCustomAttribute(field, typeof(TailwindStyleAttribute))
                is TailwindStyleAttribute attribute
                ? attribute.GetTailwindStyle()
                : string.Empty
            : string.Empty;
    }
}
