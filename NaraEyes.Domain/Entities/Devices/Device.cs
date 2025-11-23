using NaraEyes.Domain.Common;
using NaraEyes.Domain.Entities.Base;
using NaraEyes.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Domain.Entities.Devices
{
    public sealed class Device : BaseEntity
    {
        public bool InService { get; set; }
        public int? Code { get; private set; }
        public string Ip { get; private set; }
        public string? Model { get; private set; }
        public bool AgentStatus { get; set; }
        public DateTime InstallationDate { get; private set; }
        public string? Address { get; private set; }
        public string? SerialNo { get; private set; }
        public string? Tel { get; private set; }
        public string? MobileNo { get; private set; }
        public Branch? Branch { get; private set; }
        public Guid? BranchId { get; private set; }
        public DeviceMode Mode { get; private set; }
        public string? Description { get; private set; }
        public decimal? Latitude { get; private set; }
        public decimal? Longitude { get; private set; }
        public bool IsActive { get; private set; }
        public ContactInfo? Operator { get; set; }
        public Guid? OperatorId { get; set; }
        public DateTime? LastHeartbeat { get; private set; }
        public string? AgentVersion { get; private set; }
        public byte[] RowVersion { get; private set; }
        public ICollection<DeviceEvent> Events { get; private set; } = new List<DeviceEvent>();
        public MetricSnapshot? CurrentMetrics { get; set; }
        public Guid? CurrentMetricsId { get; set; }
        public ICollection<CashUnit> CashUnits { get; private set; } = new List<CashUnit>();
        public ICollection<DeviceModule> Modules { get; private set; } = new List<DeviceModule>();
        public void SetErrorMode()
        {
            Mode = DeviceMode.Error;
        }
        public void SetStatus(DeviceMode mode)
        {
            Mode = mode;
        }
        public void SetWarningMode()
        {
            Mode = DeviceMode.warning;
        }
        public void SetOnlineMode()
        {
            Mode = DeviceMode.Online;
        }
        public void AddModule(DeviceModule module)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module), "ماژول نمی‌تواند خالی باشد.");

            if (Modules.Any(m => m.DeviceId == module.DeviceId && m.Type == module.Type))
                throw new InvalidOperationException("این ماژول قبلاً به دستگاه اضافه شده است.");

            Modules.Add(module);
        }

        private static string NormalizeOrEmpty(string? s) => (s ?? string.Empty).Trim();
        private static string? NormalizeOrNull(string? s)
        {
            var t = s?.Trim();
            return string.IsNullOrWhiteSpace(t) ? null : t;
        }
        public void SetOffline()
        {
            AgentStatus = false;
        }
        public void UpdateIdentity(int? code, string ip, string? model, string? serialNo)
        {
            ip = ip?.Trim() ?? throw new ArgumentNullException(nameof(ip));
            if (ip.Length == 0) throw new InvalidOperationException("IP نمی‌تواند خالی باشد.");

            Code = code;
            Ip = ip;
            Model = NormalizeOrNull(model);
            SerialNo = NormalizeOrNull(serialNo);
        }

        public void UpdateContacts(string? tel, string? mobileNo)
        {
            Tel = NormalizeOrNull(tel);
            MobileNo = NormalizeOrNull(mobileNo);
        }



        public void SetMode(DeviceMode mode) => Mode = mode;

        public void SetDescription(string? description) => Description = NormalizeOrNull(description);

        public void SetCoordinates(decimal? latitude, decimal? longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public void AssignOperator(Guid? operatorId, ContactInfo? @operator = null)
        {
            OperatorId = operatorId;
            Operator = @operator;
        }

        public void SetAgentVersion(string? version) => AgentVersion = NormalizeOrNull(version);

        public void SetInstallationDate(DateTime? installationDate) => InstallationDate = installationDate ?? DateTime.Now;

        public void TouchHeartbeat(DateTime? whenUtc = null) => LastHeartbeat = whenUtc ?? DateTime.UtcNow;

        /// <summary>
        /// آپدیت یک‌جای کل فیلدها از ویومدل
        /// هر فیلدی که در ویومدل آمده، روی موجودیت اعمال می‌شود.
        /// </summary>
        public void ApplyUpdate(
            int? code,
            string ip,
            string? model,
            DateTime? installationDate,
            string? address,
            string? serialNo,
            string? tel,
            string? mobileNo,
            Guid? branchId,
            string? description,
            decimal? latitude,
            decimal? longitude,
            bool isActive,
            Guid? operatorId,
            ContactInfo? contact
        )
        {
            UpdateIdentity(code, ip, model, serialNo);
            SetInstallationDate(installationDate);
            Address = NormalizeOrNull(address);
            UpdateContacts(tel, mobileNo);
            SetDescription(description);
            SetCoordinates(latitude, longitude);
            IsActive = isActive;
            AssignOperator(operatorId, @operator: contact);
            Operator = contact;
            OperatorId = operatorId;
            BranchId = branchId;
        }








        public void SetAgentOffLine()
        {
            AgentStatus = false;
            ModifiedDate = DateTime.Now;
        }
        public static Device RegisterNew(
    int? code,
    string ip,
    string model,
    string? serialNo,
    string? agentVersion,
    DeviceMode mode)
        {
            var d = new Device();
            d.ApplyRegistration(code, ip, model, serialNo, agentVersion);
            d.IsActive = true;
            d.Mode = DeviceMode.Supervisor;
            d.InstallationDate = DateTime.Now;
            d.Description ??= string.Empty;
            d.CreateDate = DateTime.Now;
            return d;
        }
        public static Device RegisterNewDev(
int? code,
string ip,
string model,
string? serialNo,
string? address,
decimal? longitude,
decimal? latitude,
string? Description,
Guid branchId,
string mobileNo

)
        {
            var d = new Device();
            d.Ip = ip;
            d.Code = code;
            d.Model = model;
            d.SerialNo = serialNo;
            d.IsActive = true;
            d.Mode = DeviceMode.Supervisor;
            d.InstallationDate = DateTime.Now;
            d.Description ??= string.Empty;
            d.CreateDate = DateTime.Now;
            d.Address = address;
            d.Longitude = longitude;
            d.Latitude = latitude;
            d.BranchId = branchId;
            d.MobileNo = mobileNo;
            d.IsActive = true;

            return d;
        }








        public void ReRegister(int code,string ip, string model, string? agentVersion)
        {
            Ip = NormalizeIp(ip);
            Code = code;
            AgentVersion = NormalizeVersion(agentVersion);

            if (string.IsNullOrWhiteSpace(Model))
                Model = NormalizeModel(model);

            IsActive = true;
            LastHeartbeat = DateTime.Now;

            if (AgentStatus is false)
                AgentStatus = true;
        }
        private void ApplyRegistration(
        int? code,
        string ip,
        string model,
        string? serialNo,
        string? agentVersion)
        {
            Code = code;
            Ip = NormalizeIp(ip);
            Model = NormalizeModel(model);
            SerialNo = NormalizeSerial(serialNo);
            AgentVersion = NormalizeVersion(agentVersion);
            LastHeartbeat = DateTime.Now;
        }

        /// <summary>به‌روزرسانی هارت‌بیت (Agent زنده است)</summary>
        public void UpdateHeartbeat()
        {
            LastHeartbeat = DateTime.Now;
            if (!IsActive) IsActive = true;


            if (AgentStatus is false) AgentStatus = true;
        }



        /// <summary>غیرفعال/فعال‌سازی منطقی دستگاه</summary>
        public void Activate() { 
            IsActive = true;
            Deleted = false;
        }
        public void Deactivate()
        {
            Deleted = true;
            IsActive = false;
        }

        // ====== Version / Identity Changes ======

        /// <summary>به‌روزرسانی نسخه ایجنت</summary>
        public void UpdateAgentVersion(string version)
        {
            AgentVersion = NormalizeVersion(version);
        }

        /// <summary>به‌روزرسانی IP (مثلاً بعد از DHCP تغییر)</summary>
        public void UpdateIp(string ip)
        {
            Ip = NormalizeIp(ip);
        }

        /// <summary>تنظیم یا تغییر کد/سریال (با احتیاط؛ معمولاً در ثبت اولیه)</summary>
        public void SetIdentity(int? code, string? serialNo)
        {
            Code = code;
            SerialNo = NormalizeSerial(serialNo);
        }

        // ====== Branch / Operator / Location ======

        public void SetBranch(Guid? branchId) => BranchId = branchId;

        public void SetOperator(Guid? operatorId) => OperatorId = operatorId;

        public void SetLocation(decimal? latitude, decimal? longitude)
        {
            
            Latitude = latitude;
            Longitude = longitude;
        }

        public void UpdateDescription(string? description)
        {
            Description = (description ?? string.Empty).Trim();
        }

        // ====== Metrics (Current) ======


        public void SetCurrentMetrics(Guid snapshotId)
        {
            CurrentMetricsId = snapshotId;
        }

        /// <summary>پاک‌کردن متریک جاری (قبل از حذف Snapshot یا در Reset)</summary>
        public void ClearCurrentMetrics()
        {
            CurrentMetricsId = null;
        }

        // ====== Events / Collections ======

        /// <summary>ثبت رویداد روی دستگاه (لطفاً Entity DeviceEvent را قبلاً مقداردهی کنید)</summary>
        public void AddEvent(DeviceEvent evt)
        {
            if (evt is null) throw new ArgumentNullException(nameof(evt));
            if (evt.DeviceId != Id) evt.DeviceId = Id;
            Events.Add(evt);
        }

        /// <summary>افزودن/به‌روزرسانی یک ماژول اگر وجود نداشت (Upsert ساده)</summary>
        public DeviceModule UpsertModule(DeviceModuleType type, string name)
        {
            var existing = Modules.FirstOrDefault(m => m.Type == type);
            if (existing is not null)
            {
                if (!string.IsNullOrWhiteSpace(name) && !name.Equals(existing.Name, StringComparison.Ordinal))
                    existing.Name = name;
                return existing;
            }

            var module =  DeviceModule.Create
       ( Id,
                 type,
                 name
            );
            Modules.Add(module);
            return module;
        }

        /// <summary>افزودن یا جایگزینی یک CashUnit با معیار Denomination/Name</summary>
        //public CashUnit UpsertCashUnit(string name, string currency, string serial, int denomination,
        //                               string currentCount, string totalCount,
        //                               CashUnitStatus status)
        //{
        //    var existing = CashUnits.FirstOrDefault(cu =>
        //        cu.DeviceId==Id&&cu.Name==name);
        //    var type = name == "LCU00" ? CashUnitType.Reject : CashUnitType.Bill; 
        //    if (existing is not null)
        //    {
        //        existing.Currency = currency;
        //        existing.Serial = serial;
        //        existing.CurrentCount = currentCount;
        //        existing.TotalCount = totalCount;
        //        existing.Status = CashUnitStatus.Ok;
        //        existing.Type = type;
        //        return existing;
        //    }

        //    //var cuNew = CashUnit.Create(Id, name, currency,
        //    //   serial, denomination, int.Parse(totalCount), currentCount, CashUnitType.Bill);
        
        //    CashUnits.Add(cuNew);
        //    return cuNew;
        //}

        // ====== Guards / Normalizers ======

        private static string NormalizeIp(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) throw new ArgumentException("IP is required.");
            var trimmed = ip.Trim();
            if (trimmed.Length > 45) throw new ArgumentException("IP length invalid.");
            // Optional: IPAddress.TryParse برای سخت‌گیری بیشتر
            if (!IPAddress.TryParse(trimmed, out _)) throw new ArgumentException("IP format invalid.");
            return trimmed;
        }

        private static string NormalizeModel(string model)
        {
            if (string.IsNullOrWhiteSpace(model)) model="-";
            model = model.Trim();
            if (model.Length > 100) model = model[..100];
            return model;
        }

        private static string? NormalizeSerial(string? serial)
        {
            if (string.IsNullOrWhiteSpace(serial)) return null;
            serial = serial.Trim();
            if (serial.Length > 100) serial = serial[..100];
            return serial;
        }

        private static string? NormalizeVersion(string? ver)
        {
            if (string.IsNullOrWhiteSpace(ver)) return null;
            ver = ver.Trim();
            if (ver.Length > 50) ver = ver[..50];
            return ver;
        }


    }
}
