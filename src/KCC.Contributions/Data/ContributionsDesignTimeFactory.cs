using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KCC.Contributions.Data;

// dotnet ef reads the model through this factory, so generating a migration never boots Umbraco.
public class ContributionsDesignTimeFactory : IDesignTimeDbContextFactory<ContributionsDbContext>
{
    public ContributionsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ContributionsDbContext>().UseSqlite("Data Source=contributions-design.db").Options);
}
