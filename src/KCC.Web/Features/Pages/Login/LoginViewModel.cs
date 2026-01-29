using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using KCC.Web.Features.Pages.Shared;

namespace KCC.Web.Features.Pages.Login;

public class LoginViewModel : BasePageViewModel
{
    public string ReturnUrl { get; set; }

    // Login form fields
    [DataType(DataType.EmailAddress)]
    [Required(ErrorMessage = "Enter your email")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    [DisplayName("Email*")]
    public string Email { get; set; }

    [DataType(DataType.Password)]
    [Required(ErrorMessage = "Enter your password")]
    [DisplayName("Password*")]
    public string Password { get; set; }

    [DisplayName("Remember me")]
    public bool RememberMe { get; set; }
}
