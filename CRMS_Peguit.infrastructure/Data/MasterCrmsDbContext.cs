using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using CRMS_Peguit.domain.entities;

namespace CRMS_Peguit.infrastructure.data
{
    public class MasterCrmsDbContext : DbContext
    {
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<CompanyDatabase> CompanyDatabases => Set<CompanyDatabase>();
        public MasterCrmsDbContext(DbContextOptions<MasterCrmsDbContext> options) : base(options) { }




        protected override void OnModelCreating(ModelBuilder builder)

        {

            base.OnModelCreating(builder);

            builder.Entity<Company>(entity =>

            {

                entity.HasKey(x => x.CompanyId);


                entity.Property(x => x.CompanyCode)

                  .HasMaxLength(50)

                  .IsRequired();


                entity.Property(x => x.CompanyName)

                  .HasMaxLength(200)

                  .IsRequired();


                entity.HasIndex(x => x.CompanyCode)

                  .IsUnique();

            });



            builder.Entity<CompanyDatabase>(entity =>

            {

                entity.HasKey(x => x.CompanyDatabaseId);


                entity.Property(x => x.ServerName)

                  .HasMaxLength(200)

                  .IsRequired();


                entity.Property(x => x.DatabaseName)

                  .HasMaxLength(200)

                  .IsRequired();



                entity.HasOne(x => x.Company)

                  .WithMany()

                  .HasForeignKey(x => x.CompanyId)

                  .OnDelete(DeleteBehavior.Restrict);

            });

        }
    }
}
