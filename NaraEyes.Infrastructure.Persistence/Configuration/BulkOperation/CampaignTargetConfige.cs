using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaraEyes.Domain.Entities.BulkOperation;
using NaraEyes.Infrastructure.Persistence.Configuration;

public class CampaignTargetConfige : IEntityTypeConfiguration<CampaignTarget>
{

    public void Configure(EntityTypeBuilder<CampaignTarget> builder)
    {


        builder.ToTable("CampaignTarget");
        builder.HasKey(c => c.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.DeviceIp)
               .HasMaxLength(45)
               .IsRequired()
               .IsUnicode(false);

        builder.ConfigureBaseEntity();




    }
}

    


