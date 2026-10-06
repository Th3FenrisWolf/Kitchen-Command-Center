using System.Text.Json;
using Umbraco.Cms.Core.Models;

namespace KCC.Web.Features.Ramp;

public static class Ramps
{
    public const string Alias = "ramp";
    public const string Device = "Device";
    public const string Light = "Light";
    public const string Dark = "Dark";

    public static bool IsKnown(string ramp) => ramp is Device or Light or Dark;

    public static string Of(IMember member)
    {
        var stored = member?.GetValue<string>(Alias);
        if (string.IsNullOrEmpty(stored))
        {
            return Device;
        }

        try
        {
            var ramp = JsonSerializer.Deserialize<string[]>(stored)?.FirstOrDefault();
            return IsKnown(ramp) ? ramp : Device;
        }
        catch (JsonException)
        {
            return Device;
        }
    }

    public static void Set(IMember member, string ramp) => member.SetValue(Alias, JsonSerializer.Serialize(new[] { ramp }));
}
