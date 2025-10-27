using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaraEyes.Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Infrastructure.Persistence.Configuration.Base
{
    internal class OutBoxDeviceConfige : IEntityTypeConfiguration<OutBoxDeviceMessage>
    {
        public void Configure(EntityTypeBuilder<OutBoxDeviceMessage> b)
        {
            b.ToTable("OutBoxDeviceMessages");

        
            b.ConfigureBaseEntity();

    
            b.Property(x => x.DeviceIp)
                .IsRequired()
                .HasMaxLength(50);

            b.HasIndex(x => new { x.DeviceIp, x.Processed });

            b.Property(x => x.Processed)
                .IsRequired()
                .HasDefaultValue(false);

            b.Property(x => x.ProcessedAt);

            b.Property(x => x.CommandType)
                .IsRequired()
                .HasConversion<int>();

            b.Property(x => x.Payload)
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);
            b.Property(x => x.StartDate)
            .HasMaxLength(20)
            .IsRequired(false);
            b.Property(x => x.EndDate)
          .HasMaxLength(20)
          .IsRequired(false);
        }
    }

    internal class InBoxDeviceConfig : IEntityTypeConfiguration<InBoxDeviceMessage>
    {
        public void Configure(EntityTypeBuilder<InBoxDeviceMessage> b)
        {
       
            b.ToTable("InBoxDeviceMessages");

       
            b.ConfigureBaseEntity();

  
            b.Property(x => x.DeviceIp)
                .IsRequired()
                .HasMaxLength(50);


            b.HasIndex(x => new { x.DeviceIp, x.Processed });


            b.Property(x => x.Processed)
                .IsRequired()
                .HasDefaultValue(false);

            b.Property(x => x.ProcessedAt);


            b.Property(x => x.MessageType)
                .IsRequired()
                .HasConversion<int>();


            b.Property(x => x.Payload)
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);
        }
    }


}
