using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Identity
{
    public sealed class LoginModel

    {
        [Required(ErrorMessage = "نام کاربری الزامی است.")]
        public string UserName { get; set; } = "";

        [Required(ErrorMessage = "رمز عبور الزامی است.")]
        [MinLength(6, ErrorMessage = "رمز عبور باید حداقل ۶ کاراکتر باشد.")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "کد امنیتی الزامی است.")]
        [MinLength(6, ErrorMessage = "کد امنیتی حداقل ۶ کاراکتر است.")]
        public string CaptchaCode { get; set; } = "";

        public string CaptchaToken { get; set; } = "";

        public bool RememberMe { get; set; } = true;
    }
}
