using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Domain.Entities.BulkOperation.Enums
{
    public enum OperationType {
        [Display(Name ="نا مشخص")]
        None=-1,
        [Display(Name = "ارسال فایل گروهی")]
        FileSend =2,
        [Display(Name = "مشخصات سیستمی")]
        SystemResources =1,
        [Display(Name = "ورژن ایمیج")]
        AgentVersion =3,
        [Display(Name = "راه اندازی گروهی")]
        Reset =4,
        [Display(Name = "دوربین")]
        CameraVersion =5 }
}
