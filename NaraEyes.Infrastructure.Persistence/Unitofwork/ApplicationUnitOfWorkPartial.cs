using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Domain.Entities.Base;
using NaraEyes.Domain.Entities.BulkOperation;
using NaraEyes.Domain.Entities.Devices;
using NaraEyes.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Infrastructure.Persistence.Unitofwork
{
    public partial class ApplicationUnitOfWork
    {
        public DbSet<User> Users => Context.Set<User>();
        public DbSet<Branch> Branches => Context.Set<Branch>();
        public DbSet<SupervisionState> SupervisionStates => Context.Set<SupervisionState>();

        public DbSet<IdentityUserRole<Guid>> UserRoles => Context.Set<IdentityUserRole<Guid>>();
        public DbSet<Role> Roles => Context.Set<Role>();
        public DbSet<Device> Devices => Context.Set<Device>();
        public DbSet<ContactInfo> ContactInfos => Context.Set<ContactInfo>();
        public DbSet<DeviceEvent> DeviceEvents => Context.Set<DeviceEvent>();
        public DbSet<MetricSnapshot> MetricSnapshots => Context.Set<MetricSnapshot>();
        public DbSet<CashUnit> CashUnits => Context.Set<CashUnit>();
        public DbSet<DeviceModule> DeviceModules => Context.Set<DeviceModule>();
        public DbSet<DeviceModuleStatus> DeviceModuleStatuses => Context.Set<DeviceModuleStatus>();
        public DbSet<DeviceModuleStatusSnapshot> DeviceModuleStatusSnapshots => Context.Set<DeviceModuleStatusSnapshot>();
        public DbSet<DeviceSupply> DeviceSupplies => Context.Set<DeviceSupply>();
        public DbSet<OutBoxDeviceMessage> OutBoxDeviceMessages => Context.Set<OutBoxDeviceMessage>();
        public DbSet<InBoxDeviceMessage> InBoxDeviceMessages => Context.Set<InBoxDeviceMessage>();
        public DbSet<Campaign> Campaigns => Context.Set<Campaign>();
        public DbSet<CampaignTarget> CampaignTargets => Context.Set<CampaignTarget>();
        public DbSet<ArchivedDevice> ArchivedDevice => Context.Set<ArchivedDevice>();
    }
}
