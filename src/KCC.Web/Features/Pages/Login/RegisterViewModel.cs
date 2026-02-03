using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using KCC.Web.Features.Pages.Shared;

namespace KCC.Web.Features.Pages.Login;

public class RegisterViewModel : BasePageViewModel
{
    // Register form fields
    [DataType(DataType.Text)]
    [Required(ErrorMessage = "Enter a username")]
    [DisplayName("Username")]
    public string UserName { get; set; }

    [DataType(DataType.EmailAddress)]
    [Required(ErrorMessage = "Enter your email")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    [DisplayName("Email*")]
    public string Email { get; set; }

    [DataType(DataType.Password)]
    [Required(ErrorMessage = "Enter a password")]
    [DisplayName("Password*")]
    public string Password { get; set; }

    [DataType(DataType.Password)]
    [Required(ErrorMessage = "Enter password confirmation")]
    [Compare("Password", ErrorMessage = "The entered passwords do not match")]
    [DisplayName("Confirm your password*")]
    public string PasswordConfirmation { get; set; }
}
