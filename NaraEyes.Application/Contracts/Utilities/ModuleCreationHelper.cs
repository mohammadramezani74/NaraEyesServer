using NaraEyes.Application.Contracts.Models.Modules;
using NaraEyes.Application.Contracts.Models.Modules.CDM;
using NaraEyes.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Utilities
{
    public static class ModuleCreationHelper
    {
        public static string Dispenser = "دیسپنسر";
        public static string IDC = "کارت خوان";
        public static string Pin = "پین‌ پد";
        public static string Ptr = "پرینتر رسید";
        public static string Cam = "دوربین";
        public static string Sensors = "سنسورها";
        public static List<XfsModule> CreateStableModules()
        {
            var offline=HealthStatus.Offline;
           var AllModules = new List<XfsModule>()
        {
            new XfsModule(Dispenser, "تحویل پول",offline,CDMHelper.MapDeviceStatusToPersian(1)),
            new XfsModule(IDC, "کارت‌خوان",  offline,CDMHelper.MapDeviceStatusToPersian(1)),
            new XfsModule(Pin, "پین‌پد", offline,CDMHelper.MapDeviceStatusToPersian(1)),
            new XfsModule(Ptr, "پرینتر رسید", offline,CDMHelper.MapDeviceStatusToPersian(1)),
            new XfsModule(Cam, "دوربین", offline,CDMHelper.MapDeviceStatusToPersian(1)),
            new XfsModule(Sensors, "سنسورها", offline,CDMHelper.MapDeviceStatusToPersian(1)),
        };
            return AllModules;
        }
    }
}
