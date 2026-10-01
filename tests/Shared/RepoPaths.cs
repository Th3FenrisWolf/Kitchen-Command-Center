namespace KCC.Tests;

internal static class RepoPaths
{
    public static string Root { get; } = FindRoot();

    public static string WebProject => Path.Combine(Root, "src", "KCC.Web");

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "KitchenCommandCenter.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
