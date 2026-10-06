using KCC.Web.Features.Sqlite;
using Umbraco.Cms.Core.Services;

namespace KCC.Web.Features.Ramp;

public class MemberRamps(IMemberService memberService, IMemberWriteLock memberWriteLock)
{
    public string SavedFor(string userName) => Ramps.Of(memberService.GetByUsername(userName));

    public Task<bool> SaveAsync(Guid memberKey, string ramp) => memberWriteLock.RunAsync(() =>
    {
        var member = memberService.GetById(memberKey);
        if (member is null)
        {
            return Task.FromResult(false);
        }

        Ramps.Set(member, ramp);
        memberService.Save(member);
        return Task.FromResult(true);
    });
}
