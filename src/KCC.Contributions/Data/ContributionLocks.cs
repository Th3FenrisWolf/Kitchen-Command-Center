namespace KCC.Contributions.Data;

public static class ContributionLocks
{
    // A row in Umbraco's umbracoLock table, inserted by the initial migration. Umbraco's own lock ids are all
    // negative, so a positive id cannot collide with one a later Umbraco release adds.
    public const int Contributions = 1_000_001;
}
