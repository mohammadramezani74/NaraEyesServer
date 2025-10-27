using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NaraEyes.Domain.Entities.Identity;
using NaraEyes.Infrastructure.Persistence.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Infrastructure.Persistence.Context
{
    public partial class ApplicationDbContext
        (DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<User, Role, Guid>(options)
    {



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<IdentityUserRole<Guid>>(b =>
            {
                b.ToTable("AspNetUserRoles");
                b.HasKey(ur => new { ur.UserId, ur.RoleId });

                b.HasOne<User>()
                 .WithMany(u => u.UserRoles)
                 .HasForeignKey(ur => ur.UserId);

                b.HasOne<Role>()
                 .WithMany(r => r.UserRoles)
                 .HasForeignKey(ur => ur.RoleId);
            });
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            modelBuilder.AddAuditableShadowProperties();

        }




    }
}
