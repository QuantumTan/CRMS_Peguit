using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;

namespace CRMS_Peguit.infrastructure.data
{
    public class MasterCrmsDbContext : DbContext
    {
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<CompanyDatabase> CompanyDatabases => Set<CompanyDatabase>();
        public DbSet<Device> Devices => Set<Device>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<SuperAdmin> SuperAdmins => Set<SuperAdmin>();
        public DbSet<GlobalSetting> GlobalSettings => Set<GlobalSetting>();
        public DbSet<PlatformAuditLog> PlatformAuditLogs => Set<PlatformAuditLog>();
        public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();
        public DbSet<TenantBranding> TenantBrandings => Set<TenantBranding>();

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

                entity.Property(x => x.CredentialKey)
                    .HasMaxLength(100)
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

                entity.HasOne(x => x.Company)
                    .WithMany(c => c.Devices)
                    .HasForeignKey(x => x.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new { x.CompanyId, x.DeviceCode })
                    .IsUnique();
            });

            builder.Entity<Subscription>(entity =>
            {
                entity.HasKey(x => x.SubscriptionId);

                entity.Property(x => x.PlanName).HasMaxLength(100).IsRequired();
                entity.Property(x => x.BillingAmount).HasColumnType("decimal(18,2)");
                entity.Property(x => x.Status).HasMaxLength(50);

                entity.HasOne(x => x.Company)
                    .WithMany(c => c.Subscriptions)
                    .HasForeignKey(x => x.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<PaymentRecord>(entity =>
            {
                entity.HasKey(x => x.PaymentRecordId);

                entity.Property(x => x.AmountPaid)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(x => x.PaymentMethod)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.PaymentReference)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Notes)
                    .HasMaxLength(1000);

                entity.HasOne(x => x.Subscription)
                    .WithMany(s => s.PaymentRecords)
                    .HasForeignKey(x => x.SubscriptionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.RecordedBySuperAdmin)
                    .WithMany()
                    .HasForeignKey(x => x.RecordedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.SubscriptionId);
                entity.HasIndex(x => x.PaymentDate);
                entity.HasIndex(x => x.PaymentReference);
            });

            builder.Entity<SuperAdmin>(entity =>
            {
                entity.HasKey(x => x.SuperAdminId);

                entity.Property(x => x.Email).HasMaxLength(255).IsRequired();
                entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
                entity.Property(x => x.FirstName).HasMaxLength(100);
                entity.Property(x => x.LastName).HasMaxLength(100);

                entity.HasIndex(x => x.Email).IsUnique();
            });

            builder.Entity<GlobalSetting>(entity =>
            {
                entity.HasKey(x => x.GlobalSettingId);
                entity.Property(x => x.SettingKey).HasMaxLength(200).IsRequired();
                entity.Property(x => x.SettingValue).HasMaxLength(2000);
                entity.HasIndex(x => x.SettingKey).IsUnique();

                entity.HasOne(x => x.UpdatedBySuperAdmin)
                    .WithMany()
                    .HasForeignKey(x => x.UpdatedBySuperAdminId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<PlatformAuditLog>(entity =>
            {
                entity.HasKey(x => x.AuditLogId);
                entity.Property(x => x.PerformedByName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.ActionType).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Detail).HasMaxLength(2000).IsRequired();
                entity.Property(x => x.TargetCompanyName).HasMaxLength(200);

                entity.HasOne(x => x.PerformedBySuperAdmin)
                    .WithMany()
                    .HasForeignKey(x => x.PerformedBySuperAdminId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.ActionType);
                entity.HasIndex(x => x.CreatedAt);
                entity.HasIndex(x => x.TargetCompanyId);
            });

            builder.Entity<TenantBranding>(entity =>
            {
                entity.HasKey(x => x.CompanyId);
                entity.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
                entity.Property(x => x.AccentColor).HasMaxLength(20);
                entity.Property(x => x.ContactEmail).HasMaxLength(255);
                entity.Property(x => x.ContactPhone).HasMaxLength(50);
                entity.Property(x => x.Address).HasMaxLength(500);
                entity.Property(x => x.LogoVersion).IsRequired();
                entity.Property(x => x.UpdatedAt).IsRequired();

                entity.HasOne(x => x.Company)
                    .WithOne(c => c.Branding)
                    .HasForeignKey<TenantBranding>(x => x.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}