using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.domain.Entities;

namespace CRMS_Peguit.infrastructure.data
{
    public class MasterCrmsDbContext : DbContext
    {
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<CompanyDatabase> CompanyDatabases => Set<CompanyDatabase>();
        public DbSet<Device> Devices => Set<Device>();
        public DbSet<Customer> Customers => Set<Customer>();

        public MasterCrmsDbContext(
            DbContextOptions<MasterCrmsDbContext> options
        ) : base(options)
        {
        }

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

            builder.Entity<Device>(entity =>
            {
                entity.HasKey(x => x.DeviceId);

                entity.Property(x => x.DeviceCode)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.DeviceName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.HasIndex(x => x.DeviceCode)
                    .IsUnique();

                entity.HasOne(x => x.Company)
                    .WithMany(c => c.Devices)
                    .HasForeignKey(x => x.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Customer restored here (split-name schema, matches the tenant context).
            builder.Entity<Customer>(entity =>
            {
                entity.HasKey(x => x.CustomerId);

                entity.Property(x => x.FirstName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.MiddleName)
                    .HasMaxLength(100);

                entity.Property(x => x.LastName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Suffix)
                    .HasMaxLength(20);

                entity.Property(x => x.Phone)
                    .HasMaxLength(50);

                entity.Property(x => x.Email)
                    .HasMaxLength(255);

                entity.Property(x => x.Type)
                    .HasMaxLength(50);

                entity.Property(x => x.Status)
                    .HasMaxLength(50);

                entity.Ignore(x => x.FullName);

                entity.HasQueryFilter(c => !c.IsDeleted);

                entity.HasIndex(x => x.IsDeleted);
                entity.HasIndex(x => x.TenantId);
            });
        }
    }
}