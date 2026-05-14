using System.Text.RegularExpressions;
using KCC.Web.Features.Extensions;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace KCC.Web.Features.Providers;

public partial class EnumDropDownOptionsProvider<T> : IDropDownOptionsProvider
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
                    Text = TitleCase().Replace(Enum.GetName(type, item), "$1 $2"),
                    Value = item.GetTailwindStyle() ?? item.ToString(),
                })
        );
    }

    [GeneratedRegex("([a-z])([A-Z])")]
    private static partial Regex TitleCase();
}
