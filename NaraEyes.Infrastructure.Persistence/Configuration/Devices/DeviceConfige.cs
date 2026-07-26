using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaraEyes.Domain.Entities.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Infrastructure.Persistence.Configuration.Devices
{
    public sealed partial class DeviceConfige : IEntityTypeConfiguration<Device>
    {
        public void Configure(EntityTypeBuilder<Device> builder)
        {
            builder.ToTable("Devices");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.HasIndex(x => x.Code)
                   .HasDatabaseName("IX_Device_Code")
                   .IsUnique()
                   .HasFilter("[Code] IS NOT NULL");

            builder.HasIndex(x => x.SerialNo)
                   .HasDatabaseName("IX_Device_SerialNo")
                   .IsUnique()
                   .HasFilter("[SerialNo] IS NOT NULL");
            builder.HasIndex(x => x.Ip)
       .HasDatabaseName("IX_Device_Ip")
       .IsUnique() 
       .HasFilter("[Ip] IS NOT NULL"); 

            builder.Property(x => x.Ip)
                   .HasMaxLength(45)
                   .IsUnicode(false);

            builder.Property(x => x.Model)
                   .HasMaxLength(100);

            builder.Property(x => x.SerialNo)
                   .HasMaxLength(100);

            builder.Property(x => x.Tel)
                   .HasMaxLength(20);

            builder.Property(x => x.MobileNo)
                   .HasMaxLength(20);

            builder.Property(x => x.Description)
                   .HasMaxLength(500);

            builder.Property(x => x.AgentVersion)
                   .HasMaxLength(50);

            builder.Property(x => x.RowVersion)
                   .IsRowVersion()
                   .IsConcurrencyToken();


            builder.HasOne(x => x.Branch)
                   .WithMany(b => b.Devices)
                   .HasForeignKey(x => x.BranchId)
                   .OnDelete(DeleteBehavior.Restrict);


            builder.HasOne(x => x.Operator)
                   .WithMany()
                   .HasForeignKey(x => x.OperatorId)
                   .OnDelete(DeleteBehavior.Restrict);


            builder.HasOne(x => x.CurrentMetrics)
                   .WithOne()
                   .HasForeignKey<Device>(x => x.CurrentMetricsId)
                   .OnDelete(DeleteBehavior.NoAction);



            builder.HasMany(x => x.CashUnits)
                   .WithOne(cu => cu.Device)
                   .HasForeignKey(cu => cu.DeviceId)
                           .OnDelete(DeleteBehavior.Restrict); ;

            builder.ConfigureBaseEntity();
        }
    }
}
