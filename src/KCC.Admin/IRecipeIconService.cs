namespace KCC.Admin;

public interface IRecipeIconService
{
    Task<string> PickAsync(string name, string description, IEnumerable<string> ingredients, CancellationToken cancellationToken);
}
