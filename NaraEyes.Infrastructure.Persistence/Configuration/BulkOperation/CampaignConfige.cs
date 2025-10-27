using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaraEyes.Domain.Entities.BulkOperation;
using NaraEyes.Domain.Entities.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Infrastructure.Persistence.Configuration.BulkOperation
{
    public class CampaignConfige : IEntityTypeConfiguration<Campaign>
    {
   
            public void Configure(EntityTypeBuilder<Campaign> builder)
            {

            builder.ToTable("Campaign");

            builder.HasKey(c => c.Id);

             
                builder.Property(c => c.Title)
                    .IsRequired() 
                    .HasMaxLength(150);


                builder.Property(c => c.ManifestJson)
                    .IsRequired()
                    .HasMaxLength(4000); 

                builder.HasOne(c => c.OutBoxDeviceMessage)
                    .WithOne(c=>c.Campaign) 
                    .HasForeignKey<Campaign>(c=>c.OutBoxDeviceMessageId) 
                    .OnDelete(DeleteBehavior.Cascade); 

  
                builder.HasMany(c => c.Targets)
                    .WithOne(x=>x.Campaign) 
                    .HasForeignKey(x=>x.CampaignId)
                    .OnDelete(DeleteBehavior.Cascade); 


                builder.Property(c => c.Status)
                    .IsRequired();
            builder.ConfigureBaseEntity();




        }
        }

    }

    


