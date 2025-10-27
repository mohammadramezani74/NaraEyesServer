using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaraEyes.Domain.Common;
using NaraEyes.SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Infrastructure.Persistence.Configuration
{
    public static class EntityTypeBuilderExtensions
    {
        public static EntityTypeBuilder<T> ConfigureBaseEntity<T>(this EntityTypeBuilder<T> b)
            where T : BaseEntity
        {
            // Key/Id
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();

            // Times
            b.Property(x => x.CreateDate).HasColumnType("datetime2").HasPrecision(3);
            b.Property(x => x.ModifiedDate).HasColumnType("datetime2").HasPrecision(3);

            // shadows
            b.Property<string>(AuditableShadowProperties.CreatedByBrowserName).HasMaxLength(1000);
            b.Property<string>(AuditableShadowProperties.ModifiedByBrowserName).HasMaxLength(1000);
            b.Property<string>(AuditableShadowProperties.CreatedByIp).HasMaxLength(255);
            b.Property<string>(AuditableShadowProperties.ModifiedByIp).HasMaxLength(255);

            // Navigations to User
            b.HasOne(x => x.CreatedByUser)
             .WithMany()
             .HasForeignKey(x => x.CreatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.ModifiedBy)
             .WithMany()
             .HasForeignKey(x => x.ModifiedById)
             .OnDelete(DeleteBehavior.Restrict);



            // Indexes
            b.HasIndex(x => x.Deleted);
            b.HasIndex(x => x.CreateDate);
            b.HasIndex(x => new { x.CreatedByUserId, x.ModifiedById, x.Deleted });

            return b;
        }
    }
}
