using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NaraEyes.Domain.Entities.Base;
using NaraEyes.Domain.Entities.BulkOperation;
using NaraEyes.Domain.Entities.Devices;
using NaraEyes.Domain.Entities.Identity;

namespace NaraEyes.Application.Abstraction.Unitofwork
{
    public interface IApplicationUnitOfWork : IUnitOfWork
    {
        public DbSet<User> Users { get; }
        public DbSet<SupervisionState> SupervisionStates { get;  }
        public DbSet<Branch> Branches { get;}
        public DbSet<IdentityUserRole<Guid>> UserRoles { get; }
        public DbSet<Role> Roles { get; }
        DbSet<Device> Devices { get; }
        DbSet<ContactInfo> ContactInfos { get; }

        DbSet<DeviceEvent> DeviceEvents { get; }

        DbSet<MetricSnapshot> MetricSnapshots { get; }

        DbSet<CashUnit> CashUnits { get; }

        DbSet<DeviceModule> DeviceModules { get; }
        DbSet<DeviceModuleStatus> DeviceModuleStatuses { get; }
        DbSet<DeviceModuleStatusSnapshot> DeviceModuleStatusSnapshots { get; }

        DbSet<DeviceSupply> DeviceSupplies { get; }
        public DbSet<OutBoxDeviceMessage> OutBoxDeviceMessages { get; }
        public DbSet<InBoxDeviceMessage> InBoxDeviceMessages { get; }
        public DbSet<Campaign> Campaigns { get; }
        public DbSet<CampaignTarget> CampaignTargets { get; }
       public DbSet<ArchivedDevice> ArchivedDevice { get; }
        public DbSet<ModuleFaultLog> ModuleFaultLogs { get; }
        DbSet<DeviceStateLog> DeviceStateLogs { get; }
        DbSet<ServerUptimeLog> ServerUptimeLogs { get; }

    }
}
