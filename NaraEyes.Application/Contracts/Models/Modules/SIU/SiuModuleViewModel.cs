using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Modules.SIU
{
    public class SiuModuleViewModel
    {
        public string DeviceStatusFa { get; set; }
        public string LastUpdate { get; set; }
        public List<ItemFa> Doors { get; set; } = new();
        public List<ItemFa> Indicators { get; set; } = new();
        public List<ItemFa> Auxiliaries { get; set; } = new();
        public List<ItemFa> GuidLights { get; set; } = new();
        public DateTime[]? Times { get; set; }
        public string[]? Lables { get; set; }
        public int[]? status { get; set; }
    }
    public sealed class ItemFa
    {
        public int Index { get; set; }          // ایندکس 0..15
        public string Key { get; set; } = "";   // نام انگلیسی کوتاه/کلید
        public string TitleFa { get; set; } = "";// عنوان فارسی
        public ushort Raw { get; set; }         // مقدار خام
        public string ValueFa { get; set; } = "";// مقدار فارسی‌شده
    }
}
