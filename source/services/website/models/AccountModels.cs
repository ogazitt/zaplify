namespace BuiltSteady.Zaplify.Website.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.Web.Mvc;

    public class ChangePasswordModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "Confirmation does not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class SignInModel
    {
        //[Required]
        [Display(Name = "User name")]
        public string UserName 
        {
            get { return Email; }
            set { Email = value; }
        }

        [Required]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Email address")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterModel
    {
        //[Required]
        [Display(Name = "User name")]
        public string UserName
        {
            get { return Email; }
            set { Email = value; }
        }

        [Required]
        [DataType(DataType.EmailAddress)]
        [RegularExpression("^[a-z0-9_\\+-]+([\\.[a-z0-9_\\+-]+)*@[a-z0-9-]+(\\.[a-z0-9-]+)*\\.([a-z]{2,4})$", ErrorMessage = "Not a valid email address")]
        [Display(Name = "Email address")]

        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "Confirmation does not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "Access code")]
        public string AccessCode { get; set; }
    }
}
