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
    public sealed class BranchConfig : IEntityTypeConfiguration<Branch>
    {
        public void Configure(EntityTypeBuilder<Branch> b)
        {

            b.ToTable("Branches");

            b.Property(x => x.Name)
             .IsRequired()
             .HasMaxLength(100);

            b.Property(x => x.Code)
             .IsRequired()
             .HasMaxLength(10)
             .IsUnicode(false);

            b.Property(x => x.ShortName).HasMaxLength(50);


            b.Property(x => x.Address).HasMaxLength(250);
            b.Property(x => x.PostalCode).HasMaxLength(20).IsUnicode(false);
            b.Property(x => x.Phone).HasMaxLength(30).IsUnicode(false);



            b.Property(x => x.Latitude).HasColumnType("decimal(10,7)");
            b.Property(x => x.Longitude).HasColumnType("decimal(10,7)");

            b.HasOne(x => x.Supervision)
             .WithMany(s => s.Branches)
             .HasForeignKey(x => x.SupervisionId)
             .OnDelete(DeleteBehavior.Restrict);


            b.HasIndex(x => x.Code).IsUnique();



            b.HasIndex(x => x.SupervisionId);
            b.HasIndex(x => x.IsActive);

            b.ConfigureBaseEntity();

        }
    }
}
