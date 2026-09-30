namespace KCC.Contributions.Dashboard;

// Display names and the member write lock belong to the site, which implements this for the dashboard.
public interface IDashboardMembers
{
    Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IEnumerable<Guid> memberKeys);

    Task<bool> ApproveAsync(Guid memberKey);
}
