using KCC.Contributions.Dashboard;
using KCC.Web.Features.Sqlite;
using Umbraco.Cms.Core.Services;

namespace KCC.Web.Features.Providers;

public class DashboardMembers(IAuthorNameProvider authorNames, IMemberService memberService, IMemberWriteLock memberWriteLock) : IDashboardMembers
{
    public Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IEnumerable<Guid> memberKeys) => authorNames.ResolveMany(memberKeys);

    // Inside the member write lock, as every member save is: a sign-in attempt saves the same member, and must not
    // overwrite the approval with the copy it read before.
    public Task<bool> ApproveAsync(Guid memberKey) => memberWriteLock.RunAsync(() =>
    {
        var member = memberService.GetById(memberKey);
        if (member is null)
        {
            return Task.FromResult(false);
        }

        if (!member.IsApproved)
        {
            member.IsApproved = true;
            memberService.Save(member);
        }

        return Task.FromResult(true);
    });
}
