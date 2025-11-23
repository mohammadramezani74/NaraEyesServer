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
    internal class ArchivedDeviceConfige : IEntityTypeConfiguration<ArchivedDevice>
    {
        public void Configure(EntityTypeBuilder<ArchivedDevice> builder)
        {
            builder.ToTable("ArchivedDevice");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.DeviceId)
              .IsRequired();

           
            builder.HasOne(x => x.Device)
                   .WithMany()                    
                   .HasForeignKey(x => x.DeviceId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.ArchiveReason)
         .IsRequired()
         .HasMaxLength(2500);
            builder.HasIndex(x => x.DeviceId);
            builder.HasIndex(x => x.Deleted);

            builder.ConfigureBaseEntity();
  
        }
    }
}
