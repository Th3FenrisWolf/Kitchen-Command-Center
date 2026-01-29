using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CMS.Membership;
using Kentico.Membership;

namespace KCC.Web.Models.Common;

public class KCCApplicationUser : ApplicationUser
{
    [DataType(DataType.Text)]
    [Required(ErrorMessage = "Enter your first name")]
    [DisplayName("First Name*")]
    public string FirstName { get; set; }

    [DataType(DataType.Text)]
    [Required(ErrorMessage = "Enter your last name")]
    [DisplayName("Last Name*")]
    public string LastName { get; set; }

    [DataType(DataType.EmailAddress)]
    [Required(ErrorMessage = "Enter your email")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    [DisplayName("Email*")]
    public override string Email { get; set; }

    public Guid MemberGuid { get; set; }

    public override void MapFromMemberInfo(MemberInfo source)
    {
        base.MapFromMemberInfo(source);

        FirstName = source.GetValue("MemberFirstName", string.Empty);
        LastName = source.GetValue("MemberLastName", string.Empty);
        Email = source.GetValue("MemberEmail", string.Empty);
        MemberGuid = source.MemberGuid;
    }

    public override void MapToMemberInfo(MemberInfo target)
    {
        base.MapToMemberInfo(target);

        target.SetValue("MemberFirstName", FirstName);
        target.SetValue("MemberLastName", LastName);
        target.SetValue("MemberEmail", Email);
        target.SetValue("MemberGuid", MemberGuid);
    }

    public bool UpdateFrom(KCCApplicationUser newUser)
    {
        var updated = false;

        if (!Email.Equals(newUser.Email, StringComparison.Ordinal))
        {
            Enabled = false;
            Email = newUser.Email;
            UserName = newUser.Email;
            updated = true;
        }

        if (!FirstName.Equals(newUser.FirstName, StringComparison.Ordinal))
        {
            FirstName = newUser.FirstName;
            updated = true;
        }

        if (!LastName.Equals(newUser.LastName, StringComparison.Ordinal))
        {
            LastName = newUser.LastName;
            updated = true;
        }

        return updated;
    }
}
