using System.ComponentModel.DataAnnotations;

namespace CustomerPortal_MVC_.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "User ID is required.")]
        [Display(Name = "User ID / Account Num")]
        public string UserId { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "The {0} must be at least {2} characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
