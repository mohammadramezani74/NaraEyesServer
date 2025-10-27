using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaraEyes.Domain.Entities.Devices;

namespace NaraEyes.Infrastructure.Persistence.Configuration.Devices
{
    public sealed partial class DeviceConfige
    {
        public sealed class DeviceModuleStatusSnapshotConfig : IEntityTypeConfiguration<DeviceModuleStatusSnapshot>
        {
            public void Configure(EntityTypeBuilder<DeviceModuleStatusSnapshot> builder)
            {
                builder.ToTable("DeviceModuleStatusSnapshots");

                builder.HasKey(x => x.Id);

                builder.Property(x => x.CapturedAt)
                       .IsRequired();

                builder.Property(x => x.Status)
                       .HasConversion<int>()
                       .IsRequired();

                builder.Property(x => x.StateJson)
                       .HasColumnType("nvarchar(max)")
                       .IsRequired();

                builder.Property(x => x.Severity)
                       .IsRequired();

                builder.HasOne(x => x.Module)
                       .WithMany() 
                       .HasForeignKey(x => x.DeviceModuleId)
                       .OnDelete(DeleteBehavior.Cascade);

          
                builder.HasIndex(x => new { x.DeviceModuleId, x.CapturedAt })
                       .HasDatabaseName("IX_ModuleSnapshot_Module_Time");

               
                builder.HasIndex(x => x.Severity)
                       .HasDatabaseName("IX_ModuleSnapshot_Severity");
            }
        }
    }
    }
