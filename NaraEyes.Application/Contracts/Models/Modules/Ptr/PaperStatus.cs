using System.ComponentModel.DataAnnotations;

namespace NaraEyes.Application.Contracts.Models.Modules.Ptr
{
    public enum PaperStatus
    {
        [Display(Name ="پشتیبانی نمیشود")]
        NotSupported = 0,
        [Display(Name = "ناشناخته")]
        Unknown = 1,
        [Display(Name = "پر")]
        Full = 2,
        [Display(Name = "کم")]
        Low = 3,
        [Display(Name = "خالی")]
        Empty = 4,
        [Display(Name = "کاغذ گیر کرده")]
        Jammed = 5
    }
}