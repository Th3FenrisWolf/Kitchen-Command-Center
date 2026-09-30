namespace KCC.Web.Features.Dictionary;

public interface IResourceStringProvider
{
    string GetOrDefault(string key);

    Dictionary<string, string> GetManyOrDefault(params string[] keys);
}
