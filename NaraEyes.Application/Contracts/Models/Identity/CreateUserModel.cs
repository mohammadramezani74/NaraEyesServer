using System.ComponentModel.DataAnnotations;

namespace NaraEyes.Application.Contracts.Models.Identity
{
    public class CreateUserModel
    {
        [Required(ErrorMessage = "وارد کردن نام الزامی است")]
        public string Name { get; set; } = string.Empty;
        [Required(ErrorMessage = "وارد کردن نام خانوادگی الزامی است")]
        public string LName { get; set; } = string.Empty;

        [Required(ErrorMessage = "وارد کردن نام کاربری الزامی است")]
        public string UserName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "وارد کردن رمز عبور الزامی است")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "رمز عبور باید حداقل ۶ کاراکتر باشد")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "لطفاً رمز عبور را تأیید کنید")]
        [Compare("Password", ErrorMessage = "رمز عبور و تأیید آن یکسان نیست")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

}
