namespace KCC.Web.Features.RecipeImport;

public class RecipeImportOptions
{
    public const string SectionName = "RecipeImport";

    public string Model { get; set; } = "claude-opus-5-5";

    public string Effort { get; set; } = "low";

    public decimal MonthlyCeilingUsd { get; set; } = 2.00m;

    public int MaxImageEdge { get; set; } = 2000;

    public int ReadTimeoutSeconds { get; set; } = 85;

    public bool StandInReader { get; set; }

    public int StandInDelaySeconds { get; set; }
}
