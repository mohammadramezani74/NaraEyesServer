using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Domain.Enumerations
{
    public enum CommandType
    {
        [Display(Name = "-")]
        None = 0,
        [Display(Name ="راه اندازی مجدد دستگاه")]
        Reset = 1,
        [Display(Name = "اسکرین لحظه ای")]
        Screenshot = 2,       
        CashUnitStatus = 3,  
        DeviceStatus = 4,   
        UpdateConfig = 5,
        [Display(Name = "خاموشی دستگاه")]
        Shutdown = 6,    
        SendLogs = 7,
        [Display(Name = "دریافت ژورنال")]
        EJournal =8,
        [Display(Name = "ریست دیسپنسر")]
        ResetCdm = 9,
        [Display(Name = "ریست کارت خوان")]
        resetIdc = 10,
        [Display(Name = "ریست پرینتر")]
        testprinter = 11,
        [Display(Name = "بارگزاری فایل")]
        UploadFile =12,
        Metrics = 13,
        [Display(Name = " بارگزاری فایل گروهی")]
        UploadGroupFile =14,
        [Display(Name = " راه اندازی گروهی دستگاه ها")]
        ResetGroup =15,

    }
}
