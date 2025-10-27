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
        public DbSet<User> Users => _context.Set<User>();
        public DbSet<Branch> Branches => _context.Set<Branch>();
        public DbSet<SupervisionState> SupervisionStates => _context.Set<SupervisionState>();

        public DbSet<IdentityUserRole<Guid>> UserRoles => _context.Set<IdentityUserRole<Guid>>();
        public DbSet<Role> Roles => _context.Set<Role>();
        public DbSet<Device> Devices => _context.Set<Device>();
        public DbSet<ContactInfo> ContactInfos => _context.Set<ContactInfo>();
        public DbSet<DeviceEvent> DeviceEvents => _context.Set<DeviceEvent>();
        public DbSet<MetricSnapshot> MetricSnapshots => _context.Set<MetricSnapshot>();
        public DbSet<CashUnit> CashUnits => _context.Set<CashUnit>();
        public DbSet<DeviceModule> DeviceModules => _context.Set<DeviceModule>();
        public DbSet<DeviceModuleStatus> DeviceModuleStatuses => _context.Set<DeviceModuleStatus>();
        public DbSet<DeviceModuleStatusSnapshot> DeviceModuleStatusSnapshots => _context.Set<DeviceModuleStatusSnapshot>();
        public DbSet<DeviceSupply> DeviceSupplies => _context.Set<DeviceSupply>();
        public DbSet<OutBoxDeviceMessage> OutBoxDeviceMessages => _context.Set<OutBoxDeviceMessage>();
        public DbSet<InBoxDeviceMessage> InBoxDeviceMessages => _context.Set<InBoxDeviceMessage>();
        public DbSet<Campaign> Campaigns => _context.Set<Campaign>();
        public DbSet<CampaignTarget> CampaignTargets => _context.Set<CampaignTarget>();
    }
}
