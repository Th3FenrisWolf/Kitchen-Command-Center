using System.Reflection;
using System.Runtime.CompilerServices;

namespace KCC.IntegrationTests.Config;

internal static class ModuleInit
{
    /// <summary>
    /// Runs before any other code in the test assembly. Clears all arguments from the cached
    /// <see cref="Environment.GetCommandLineArgs"/> result so Kentico's startup code doesn't
    /// try to parse them. Kentico reads <see cref="Environment.GetCommandLineArgs"/> directly
    /// (via its internal <c>CommandLineArgsProvider</c>) expecting its own <c>--kxp-*</c>
    /// verbs; under <c>dotnet test</c> the host process receives MTP flags like
    /// <c>--server dotnettestcli</c> and Kentico's CommandLineParser rejects them, which makes
    /// the test host exit before any test is reported.
    /// </summary>
    [ModuleInitializer]
    public static void Init() => ClearCommandLineArgsCache();

    private static void ClearCommandLineArgsCache()
    {
        var args = Environment.GetCommandLineArgs();
        if (args.Length <= 1)
        {
            return;
        }

        // .NET caches the result of GetCommandLineArgs in a private static field on Environment.
        // Overwrite it with just the process path so Kentico sees no arguments to parse.
        // If the runtime field layout changes in a future .NET version, fail loudly.
        var field = typeof(Environment).GetField(
            "s_commandLineArgs",
            BindingFlags.Static | BindingFlags.NonPublic
        );

        if (field is null)
        {
            throw new InvalidOperationException(
                "Environment.s_commandLineArgs field not found; .NET runtime internals may have changed."
            );
        }

        field.SetValue(null, new[] { args[0] });
    }
}
