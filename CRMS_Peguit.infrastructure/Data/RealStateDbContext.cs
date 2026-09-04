using CRMS_Peguit.domain.entities;
using CRMS_Peguit.domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS_Peguit.infrastructure.data
{
    public class RealEstateDbContext : DbContext
    {
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<User> Users => Set<User>();
        public DbSet<LoginSession> LoginSessions => Set<LoginSession>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<BuyerProfile> BuyerProfiles => Set<BuyerProfile>();
        public DbSet<Property> Properties => Set<Property>();
        public DbSet<Lead> Leads => Set<Lead>();
        public DbSet<Deal> Deals => Set<Deal>();
        public DbSet<Activity> Activities => Set<Activity>();
        public DbSet<PropertyShowingDetail> PropertyShowingDetails => Set<PropertyShowingDetail>();
        public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
        public DbSet<BackupLog> BackupLogs => Set<BackupLog>();

        public RealEstateDbContext(
            DbContextOptions<RealEstateDbContext> options
        ) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Role>(entity =>
            {
                entity.HasKey(x => x.RoleId);

                entity.Property(x => x.RoleName)
                    .HasMaxLength(100)
                    .IsRequired();
            });

            builder.Entity<User>(entity =>
            {
                entity.HasKey(x => x.UserId);

                entity.Property(x => x.FullName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Email)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.HasIndex(x => x.Email)
                    .IsUnique();

                entity.Property(x => x.PasswordHash)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .HasMaxLength(50);

                entity.HasOne<Role>()
                    .WithMany()
                    .HasForeignKey(x => x.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<LoginSession>(entity =>
            {
                entity.HasKey(x => x.SessionId);

                entity.Property(x => x.IpAddress)
                    .HasMaxLength(50);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Customer>(entity =>
            {
                entity.HasKey(x => x.CustomerId);

                entity.Property(x => x.Name)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Phone)
                    .HasMaxLength(50);

                entity.Property(x => x.Email)
                    .HasMaxLength(200);

                entity.Property(x => x.Type)
                    .HasMaxLength(50);

                entity.Property(x => x.Status)
                    .HasMaxLength(50);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.AssignedAgentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<BuyerProfile>(entity =>
            {
                entity.HasKey(x => x.CustomerId);

                entity.Property(x => x.Budget)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.PreferredLocation)
                    .HasMaxLength(200);

                entity.Property(x => x.PreferredPropertyType)
                    .HasMaxLength(100);

                entity.HasOne<Customer>()
                    .WithOne()
                    .HasForeignKey<BuyerProfile>(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Property>(entity =>
            {
                entity.HasKey(x => x.PropertyId);

                entity.Property(x => x.Address)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(x => x.PropertyType)
                    .HasMaxLength(100);

                entity.Property(x => x.Price)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.Status)
                    .HasMaxLength(50);

                entity.HasOne<Customer>()
                    .WithMany()
                    .HasForeignKey(x => x.OwnerCustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.ListedByAgentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Lead>(entity =>
            {
                entity.HasKey(x => x.LeadId);

                entity.Property(x => x.Name)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Phone)
                    .HasMaxLength(50);

                entity.Property(x => x.Email)
                    .HasMaxLength(200);

                entity.Property(x => x.Source)
                    .HasMaxLength(100);

                entity.Property(x => x.Stage)
                    .HasMaxLength(50);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.AssignedAgentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Customer>()
                    .WithOne()
                    .HasForeignKey<Lead>(x => x.ConvertedCustomerId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Deal>(entity =>
            {
                entity.HasKey(x => x.DealId);

                entity.Property(x => x.Value)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.CommissionRate)
                    .HasColumnType("decimal(5,2)");

                entity.Property(x => x.Stage)
                    .HasMaxLength(50);

                entity.HasOne<Customer>()
                    .WithMany()
                    .HasForeignKey(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Property>()
                    .WithMany()
                    .HasForeignKey(x => x.PropertyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.AgentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Activity>(entity =>
            {
                entity.HasKey(x => x.ActivityId);

                entity.Property(x => x.Type)
                    .HasMaxLength(100);

                entity.Property(x => x.Notes)
                    .HasMaxLength(2000);

                entity.HasOne<Lead>()
                    .WithMany()
                    .HasForeignKey(x => x.RelatedLeadId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne<Customer>()
                    .WithMany()
                    .HasForeignKey(x => x.RelatedCustomerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.LoggedByAgentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<PropertyShowingDetail>(entity =>
            {
                entity.HasKey(x => x.ShowingDetailId);

                entity.Property(x => x.FeedbackNotes)
                    .HasMaxLength(2000);

                entity.HasOne<Activity>()
                    .WithOne()
                    .HasForeignKey<PropertyShowingDetail>(x => x.ActivityId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Property>()
                    .WithMany()
                    .HasForeignKey(x => x.PropertyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<SupportTicket>(entity =>
            {
                entity.HasKey(x => x.TicketId);

                entity.Property(x => x.Description)
                    .HasMaxLength(2000);

                entity.Property(x => x.Priority)
                    .HasMaxLength(20);

                entity.Property(x => x.Status)
                    .HasMaxLength(50);

                entity.HasOne<Customer>()
                    .WithMany()
                    .HasForeignKey(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.RaisedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.AssignedToUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Subscription>(entity =>
            {
                entity.HasKey(x => x.SubscriptionId);

                entity.Property(x => x.PlanName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.BillingAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.Status)
                    .HasMaxLength(50);
            });

            builder.Entity<SystemSetting>(entity =>
            {
                entity.HasKey(x => x.SettingId);

                entity.Property(x => x.SettingKey)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.SettingValue)
                    .HasMaxLength(2000);

                entity.HasIndex(x => x.SettingKey)
                    .IsUnique();

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<BackupLog>(entity =>
            {
                entity.HasKey(x => x.BackupId);

                entity.Property(x => x.Status)
                    .HasMaxLength(50);

                entity.Property(x => x.FileLocation)
                    .HasMaxLength(500);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.PerformedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
