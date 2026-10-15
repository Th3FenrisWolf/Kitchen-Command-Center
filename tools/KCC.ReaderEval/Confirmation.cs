namespace KCC.ReaderEval;

public static class Confirmation
{
    public static bool Ask(TextReader input, TextWriter output, string question)
    {
        output.Write($"{question} [y/N] ");
        output.Flush();
        var answer = input.ReadLine()?.Trim();
        return string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase)
            || string.Equals(answer, "yes", StringComparison.OrdinalIgnoreCase);
    }
}
