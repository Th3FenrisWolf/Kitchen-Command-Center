using System.Text.Json;

namespace KCC.Web.Features.Islands;

public sealed record IslandEntry(string Name, JsonElement Props, IReadOnlyList<string> Keys);
