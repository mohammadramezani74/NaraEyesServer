using System.ComponentModel.DataAnnotations;

namespace NaraEyes.Application.Contracts.Models.Identity
{
    public class UpdateUserModel
    {
        public Guid Id { get; set; }
        [Required(ErrorMessage = "وارد کردن نام الزامی است")]
        public string Name { get; set; } = string.Empty;
        [Required(ErrorMessage = "وارد کردن نام خانوادگی الزامی است")]
        public string LName { get; set; } = string.Empty;

        [Required(ErrorMessage = "وارد کردن نام کاربری الزامی است")]
        public string UserName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }

    }

}
