using System.Text.RegularExpressions;
using KCC.Web.Extensions;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace KCC.Web.Features.Providers;

public class EnumDropDownOptionsProvider<T> : IDropDownOptionsProvider
    where T : Enum
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var type = typeof(T);

        return Task.FromResult(
            Enum.GetValues(type)
                .Cast<T>()
                .Select(item => new DropDownOptionItem
                {
                    Text = ToTitle(Enum.GetName(type, item)),
                    Value = item.GetTailwindStyle() ?? item.ToString(),
                })
        );
    }

    private static string ToTitle(string text) => Regex.Replace(text, "([a-z])([A-Z])", "$1 $2");
}
