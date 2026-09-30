using KCC.Contributions.Dashboard;
using KCC.Contributions.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace KCC.Contributions;

public class ContributionsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddUmbracoDbContext<ContributionsDbContext>(
            (IServiceProvider services, DbContextOptionsBuilder options, string connectionString, string providerName) =>
            {
                if (!string.IsNullOrEmpty(providerName) && !string.IsNullOrEmpty(connectionString))
                {
                    options.UseDatabaseProvider(providerName, connectionString);
                }
            },
            shareUmbracoConnection: true);

        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, RunContributionsMigrations>();
        builder
            .AddNotificationAsyncHandler<ContentDeletedNotification, ContributionCascades>()
            .AddNotificationAsyncHandler<MemberDeletedNotification, ContributionCascades>();

        builder.Services.AddSingleton<IContributionStats, ContributionStatsSource>();
        builder.Services.AddSingleton<IContributionReads, ContributionReads>();
        builder.Services.AddSingleton<IContributionWrites, ContributionWrites>();
        builder.Services.AddScoped<IDashboardQueries, DashboardQueries>();
    }
}
