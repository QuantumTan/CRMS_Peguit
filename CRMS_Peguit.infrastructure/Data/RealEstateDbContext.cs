using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRMS_Peguit.domain.entities;
using Microsoft.EntityFrameworkCore;

namespace CRMS_Peguit.infrastructure.data
{
    public class RealEstateDbContext : DbContext
    {
        private readonly int _tenantId;

        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Person> Persons => Set<Person>();
        public DbSet<User> Users => Set<User>();
        public DbSet<LoginSession> LoginSessions => Set<LoginSession>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<BuyerProfile> BuyerProfiles => Set<BuyerProfile>();
        public DbSet<Property> Properties => Set<Property>();
        public DbSet<Lead> Leads => Set<Lead>();
        public DbSet<Deal> Deals => Set<Deal>();
        public DbSet<DealContingency> DealContingencies => Set<DealContingency>();
        public DbSet<DealClause> DealClauses => Set<DealClause>();
        public DbSet<Activity> Activities => Set<Activity>();
        public DbSet<PropertyShowingDetail> PropertyShowingDetails => Set<PropertyShowingDetail>();
        public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
        public DbSet<TicketComment> TicketComments => Set<TicketComment>();
        public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
        public DbSet<BackupLog> BackupLogs => Set<BackupLog>();
        public DbSet<TaskReminder> TaskReminders => Set<TaskReminder>();
        public DbSet<Campaign> Campaigns => Set<Campaign>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
        public DbSet<AutomatedEmailSettings> AutomatedEmailSettings => Set<AutomatedEmailSettings>();
        public DbSet<MarketUpdateLog> MarketUpdateLogs => Set<MarketUpdateLog>();
        public DbSet<Branch> Branches => Set<Branch>();

        public RealEstateDbContext(
            DbContextOptions<RealEstateDbContext> options,
            int tenantId = 0
        ) : base(options)
        {
            _tenantId = tenantId;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Role>(entity =>
            {
                entity.HasKey(x => x.RoleId);
                entity.Property(x => x.RoleName).HasMaxLength(100).IsRequired();
            });

            builder.Entity<Person>(entity =>
            {
                entity.HasKey(x => x.PersonId);
                entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
                entity.Property(x => x.MiddleName).HasMaxLength(100);
                entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Suffix).HasMaxLength(20);
                entity.Property(x => x.Email).HasMaxLength(255);
                entity.Property(x => x.Phone).HasMaxLength(50);
                entity.Ignore(x => x.FullName);
            });

            builder.Entity<User>(entity =>
            {
                entity.HasKey(x => x.UserId);
                entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
                entity.Property(x => x.Status).HasMaxLength(50);

                entity.Ignore(x => x.FirstName);
                entity.Ignore(x => x.MiddleName);
                entity.Ignore(x => x.LastName);
                entity.Ignore(x => x.Suffix);
                entity.Ignore(x => x.Email);
                entity.Ignore(x => x.Phone);
                entity.Ignore(x => x.FullName);

                entity.HasOne(x => x.Person)
                    .WithMany()
                    .HasForeignKey(x => x.PersonId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Navigation(x => x.Person).AutoInclude();

                entity.HasOne(x => x.Role)
                    .WithMany()
                    .HasForeignKey(x => x.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<LoginSession>(entity =>
            {
                entity.HasKey(x => x.SessionId);
                entity.Property(x => x.IpAddress).HasMaxLength(50);

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Customer>(entity =>
            {
                entity.HasKey(x => x.CustomerId);

                entity.Property(x => x.Type).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
                entity.Property(x => x.AssignmentStatus).HasMaxLength(50).IsRequired();
                entity.Property(x => x.AssignmentReviewNotes).HasMaxLength(1000);

                entity.Ignore(x => x.FirstName);
                entity.Ignore(x => x.MiddleName);
                entity.Ignore(x => x.LastName);
                entity.Ignore(x => x.Suffix);
                entity.Ignore(x => x.Phone);
                entity.Ignore(x => x.Email);
                entity.Ignore(x => x.FullName);

                entity.HasIndex(x => x.IsDeleted);

                entity.HasOne(x => x.Person)
                    .WithMany()
                    .HasForeignKey(x => x.PersonId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Navigation(x => x.Person).AutoInclude();

                entity.HasOne(x => x.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.AssignedAgent)
                    .WithMany()
                    .HasForeignKey(x => x.AssignedAgentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<BuyerProfile>(entity =>
            {
                entity.HasKey(x => x.CustomerId);
                entity.Property(x => x.Budget).HasColumnType("decimal(18,2)");
                entity.Property(x => x.PreferredLocation).HasMaxLength(200);
                entity.Property(x => x.PreferredPropertyType).HasMaxLength(100);

                entity.HasOne(x => x.Customer)
                    .WithOne()
                    .HasForeignKey<BuyerProfile>(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Property>(entity =>
            {
                entity.HasKey(x => x.PropertyId);
                entity.Property(x => x.Address).HasMaxLength(500).IsRequired();
                entity.Property(x => x.PropertyType).HasMaxLength(100);
                entity.Property(x => x.Price).HasColumnType("decimal(18,2)");
                entity.Property(x => x.Status).HasMaxLength(50);
                entity.Property(x => x.ListedByAgentId).IsRequired(false);
                entity.Property(x => x.AssignmentStatus).HasMaxLength(50).IsRequired();
                entity.Property(x => x.AssignmentReviewNotes).HasMaxLength(1000);

                entity.HasOne(x => x.OwnerCustomer)
                    .WithMany()
                    .HasForeignKey(x => x.OwnerCustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.ListedByAgent)
                    .WithMany()
                    .HasForeignKey(x => x.ListedByAgentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Lead>(entity =>
            {
                entity.HasKey(x => x.LeadId);

                entity.Property(x => x.Source).HasMaxLength(100);
                entity.Property(x => x.Stage).HasMaxLength(50).IsRequired();
                entity.Property(x => x.ExpectedValue).HasColumnType("decimal(18,2)");
                entity.Property(x => x.Notes).HasMaxLength(2000);
                entity.Property(x => x.Priority).HasMaxLength(20);
                entity.Property(x => x.AssignmentStatus).HasMaxLength(50).IsRequired();
                entity.Property(x => x.AssignmentReviewNotes).HasMaxLength(1000);

                entity.Ignore(x => x.FirstName);
                entity.Ignore(x => x.MiddleName);
                entity.Ignore(x => x.LastName);
                entity.Ignore(x => x.Suffix);
                entity.Ignore(x => x.Phone);
                entity.Ignore(x => x.Email);
                entity.Ignore(x => x.FullName);

                entity.HasIndex(x => x.IsDeleted);

                entity.HasOne(x => x.Person)
                    .WithMany()
                    .HasForeignKey(x => x.PersonId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Navigation(x => x.Person).AutoInclude();

                entity.HasOne(x => x.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.AssignedAgent)
                    .WithMany()
                    .HasForeignKey(x => x.AssignedAgentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.ConvertedCustomer)
                    .WithOne()
                    .HasForeignKey<Lead>(x => x.ConvertedCustomerId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Deal>(entity =>
            {
                entity.HasKey(x => x.DealId);
                entity.Property(x => x.Value).HasColumnType("decimal(18,2)");
                entity.Property(x => x.CommissionRate).HasColumnType("decimal(5,2)");
                entity.Property(x => x.Stage).HasMaxLength(50);
                entity.Property(x => x.PaymentScheme).HasMaxLength(50).IsRequired(false);
                entity.Property(x => x.ReservationFee).HasColumnType("decimal(18,2)").IsRequired(false);
                entity.Property(x => x.DownPaymentPercent).HasColumnType("decimal(5,2)").IsRequired(false);
                entity.Ignore(x => x.DownPaymentAmount);
                entity.Ignore(x => x.BalanceAmount);
                entity.Property(x => x.CgtPayer).HasMaxLength(50).IsRequired(false);
                entity.Property(x => x.DstPayer).HasMaxLength(50).IsRequired(false);
                entity.Property(x => x.TransferTaxPayer).HasMaxLength(50).IsRequired(false);
                entity.Property(x => x.RegistrationFeePayer).HasMaxLength(50).IsRequired(false);
                entity.Ignore(x => x.ContingenciesJson);
                entity.Ignore(x => x.ApprovedClauseIds);
                entity.Property(x => x.SpecialStipulations).HasMaxLength(4000).IsRequired(false);

                entity.HasOne(x => x.Customer)
                    .WithMany()
                    .HasForeignKey(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Property)
                    .WithMany()
                    .HasForeignKey(x => x.PropertyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Agent)
                    .WithMany()
                    .HasForeignKey(x => x.AgentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(x => x.Contingencies)
                    .WithOne(c => c.Deal)
                    .HasForeignKey(c => c.DealId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(x => x.DealClauses)
                    .WithOne(c => c.Deal)
                    .HasForeignKey(c => c.DealId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<DealContingency>(entity =>
            {
                entity.HasKey(x => x.DealContingencyId);
                entity.Property(x => x.ContingencyName).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(500);

                entity.HasOne(x => x.Deal)
                    .WithMany(d => d.Contingencies)
                    .HasForeignKey(x => x.DealId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<DealClause>(entity =>
            {
                entity.HasKey(x => x.DealClauseId);
                entity.Property(x => x.ClauseId).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Title).HasMaxLength(200);

                entity.HasOne(x => x.Deal)
                    .WithMany(d => d.DealClauses)
                    .HasForeignKey(x => x.DealId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Activity>(entity =>
            {
                entity.HasKey(x => x.ActivityId);
                entity.Property(x => x.Type).HasMaxLength(100);
                entity.Property(x => x.Notes).HasMaxLength(2000);

                entity.HasOne(x => x.RelatedLead)
                    .WithMany()
                    .HasForeignKey(x => x.RelatedLeadId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(x => x.RelatedCustomer)
                    .WithMany()
                    .HasForeignKey(x => x.RelatedCustomerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(x => x.LoggedByAgent)
                    .WithMany()
                    .HasForeignKey(x => x.LoggedByAgentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<PropertyShowingDetail>(entity =>
            {
                entity.HasKey(x => x.ShowingDetailId);
                entity.Property(x => x.FeedbackNotes).HasMaxLength(2000);

                entity.HasOne(x => x.Activity)
                    .WithOne()
                    .HasForeignKey<PropertyShowingDetail>(x => x.ActivityId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Property)
                    .WithMany()
                    .HasForeignKey(x => x.PropertyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<SupportTicket>(entity =>
            {
                entity.HasKey(x => x.TicketId);
                entity.Property(x => x.TicketNumber).HasMaxLength(30).IsRequired();
                entity.Property(x => x.Category).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(2000);
                entity.Property(x => x.Priority).HasMaxLength(20);
                entity.Property(x => x.Status).HasMaxLength(50);

                entity.HasIndex(x => x.TicketNumber);
                entity.HasIndex(x => x.IsDeleted);

                entity.HasOne(x => x.Customer)
                    .WithMany()
                    .HasForeignKey(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.RaisedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.RaisedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.AssignedToUser)
                    .WithMany()
                    .HasForeignKey(x => x.AssignedToUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(x => x.Comments)
                    .WithOne(c => c.Ticket)
                    .HasForeignKey(c => c.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<TicketComment>(entity =>
            {
                entity.HasKey(x => x.TicketCommentId);
                entity.Property(x => x.CommentText).HasMaxLength(2000).IsRequired();
                entity.Property(x => x.CommentType).HasMaxLength(50).IsRequired();

                entity.HasOne(x => x.Ticket)
                    .WithMany(t => t.Comments)
                    .HasForeignKey(x => x.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.AuthorUser)
                    .WithMany()
                    .HasForeignKey(x => x.AuthorUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<SystemSetting>(entity =>
            {
                entity.HasKey(x => x.SettingId);
                entity.Property(x => x.SettingKey).HasMaxLength(200).IsRequired();
                entity.Property(x => x.SettingValue).HasMaxLength(2000);
                entity.HasIndex(x => x.SettingKey).IsUnique();

                entity.HasOne(x => x.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<BackupLog>(entity =>
            {
                entity.HasKey(x => x.BackupId);
                entity.Property(x => x.Status).HasMaxLength(50);
                entity.Property(x => x.FileLocation).HasMaxLength(500);

                entity.HasOne(x => x.PerformedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.PerformedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<TaskReminder>(entity =>
            {
                entity.HasKey(x => x.TaskReminderId);
                entity.Ignore(x => x.Id);

                entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Type).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Priority).HasMaxLength(20).IsRequired();
                entity.Property(x => x.Notes).HasMaxLength(2000);

                entity.HasIndex(x => x.IsDeleted);
                entity.HasIndex(x => x.DueDate);
                entity.HasIndex(x => x.Status);
                entity.HasIndex(x => x.AssignedToUserId);

                entity.HasOne(x => x.AssignedToUser)
                    .WithMany()
                    .HasForeignKey(x => x.AssignedToUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.RelatedCustomer)
                    .WithMany()
                    .HasForeignKey(x => x.RelatedCustomerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(x => x.RelatedLead)
                    .WithMany()
                    .HasForeignKey(x => x.RelatedLeadId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Campaign>(entity =>
            {
                entity.HasKey(x => x.CampaignId);
                entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
                entity.Property(x => x.Channel).HasMaxLength(100);
                entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Budget).HasColumnType("decimal(18,2)");

                entity.HasIndex(x => x.TenantId);
                entity.HasIndex(x => x.IsActive);
                entity.HasIndex(x => x.Name);
            });

            builder.Entity<Activity>(entity =>
            {
                entity.HasKey(x => x.ActivityId);

                entity.Property(x => x.Type).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Outcome)
                    .HasConversion<string>()
                    .HasMaxLength(50);
                entity.Property(x => x.Notes).HasMaxLength(2000);

                entity.HasOne(x => x.LoggedByAgent)
                    .WithMany()
                    .HasForeignKey(x => x.LoggedByAgentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.RelatedCustomer)
                    .WithMany()
                    .HasForeignKey(x => x.RelatedCustomerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(x => x.RelatedLead)
                    .WithMany()
                    .HasForeignKey(x => x.RelatedLeadId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Notification>(entity =>
            {
                entity.HasKey(x => x.NotificationId);
                entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Message).HasMaxLength(1000).IsRequired();
                entity.Property(x => x.RelatedEntityType).HasMaxLength(50);

                entity.HasIndex(x => x.TenantId);
                entity.HasIndex(x => x.RecipientUserId);
                entity.HasIndex(x => x.IsRead);
                entity.HasIndex(x => x.CreatedAt);

                entity.HasOne(x => x.RecipientUser)
                    .WithMany()
                    .HasForeignKey(x => x.RecipientUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<NotificationPreference>(entity =>
            {
                entity.HasKey(x => x.NotificationPreferenceId);

                entity.HasIndex(x => x.TenantId);
                entity.HasIndex(x => new { x.UserId, x.Type }).IsUnique();

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // --- Global query filters (3NF Transitively Derived via FK chains) ---
            builder.Entity<Role>().HasQueryFilter(x => x.TenantId == _tenantId);
            builder.Entity<User>().HasQueryFilter(x => x.Role.TenantId == _tenantId);
            builder.Entity<LoginSession>().HasQueryFilter(x => x.User.Role.TenantId == _tenantId);
            builder.Entity<Customer>().HasQueryFilter(x => x.CreatedByUser.Role.TenantId == _tenantId && !x.IsDeleted);
            builder.Entity<BuyerProfile>().HasQueryFilter(x => x.Customer.CreatedByUser.Role.TenantId == _tenantId);
            builder.Entity<Property>().HasQueryFilter(x => x.CreatedByUser.Role.TenantId == _tenantId);
            builder.Entity<Lead>().HasQueryFilter(x => x.CreatedByUser.Role.TenantId == _tenantId && !x.IsDeleted);
            builder.Entity<Deal>().HasQueryFilter(x => x.CreatedByUser!.Role.TenantId == _tenantId);
            builder.Entity<Activity>().HasQueryFilter(x => x.LoggedByAgent.Role.TenantId == _tenantId);
            builder.Entity<PropertyShowingDetail>().HasQueryFilter(x => x.Activity.LoggedByAgent.Role.TenantId == _tenantId);
            builder.Entity<SupportTicket>().HasQueryFilter(x => x.RaisedByUser.Role.TenantId == _tenantId && !x.IsDeleted);
            builder.Entity<TicketComment>().HasQueryFilter(x => x.Ticket.RaisedByUser.Role.TenantId == _tenantId && !x.Ticket.IsDeleted);
            builder.Entity<TaskReminder>().HasQueryFilter(x => x.AssignedToUser.Role.TenantId == _tenantId && !x.IsDeleted);
            builder.Entity<SystemSetting>().HasQueryFilter(x => x.UpdatedByUser.Role.TenantId == _tenantId);
            builder.Entity<BackupLog>().HasQueryFilter(x => x.PerformedByUser.Role.TenantId == _tenantId);
            builder.Entity<Campaign>().HasQueryFilter(x => x.TenantId == _tenantId);
            builder.Entity<Notification>().HasQueryFilter(x => x.RecipientUser.Role.TenantId == _tenantId);
            builder.Entity<NotificationPreference>().HasQueryFilter(x => x.User.Role.TenantId == _tenantId);

            builder.Entity<AutomatedEmailSettings>(entity =>
            {
                entity.HasKey(x => x.SettingsId);
                entity.Property(x => x.SubjectTemplate).HasMaxLength(300).IsRequired();
                entity.Property(x => x.BodyTemplate).IsRequired();
            });
            builder.Entity<AutomatedEmailSettings>().HasQueryFilter(x => x.TenantId == _tenantId);

            builder.Entity<MarketUpdateLog>(entity =>
            {
                entity.HasKey(x => x.LogId);
                entity.Property(x => x.CustomerName).HasMaxLength(150).IsRequired();
                entity.Property(x => x.RecipientEmail).HasMaxLength(255).IsRequired();
                entity.Property(x => x.PropertyAddress).HasMaxLength(300);
                entity.Property(x => x.PropertyType).HasMaxLength(50);
                entity.Property(x => x.EmailFormat).HasMaxLength(20);
                entity.Property(x => x.Status).HasMaxLength(30);
                entity.Property(x => x.TriggerType).HasMaxLength(30);

                entity.HasOne(x => x.Customer)
                    .WithMany()
                    .HasForeignKey(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Branch>(entity =>
            {
                entity.HasKey(x => x.BranchId);
                entity.Property(x => x.BranchCode).HasMaxLength(50).IsRequired();
                entity.Property(x => x.BranchName).HasMaxLength(150).IsRequired();
                entity.Property(x => x.Address).HasMaxLength(300);
                entity.Property(x => x.Phone).HasMaxLength(50);

                entity.HasMany(x => x.Users)
                    .WithOne(x => x.Branch)
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(x => x.Properties)
                    .WithOne(x => x.Branch)
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(x => x.Leads)
                    .WithOne(x => x.Branch)
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(x => x.Deals)
                    .WithOne(x => x.Branch)
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<MarketUpdateLog>().HasQueryFilter(x => x.TenantId == _tenantId);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            EnforceTenantId();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            EnforceTenantId();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void EnforceTenantId()
        {
            if (_tenantId <= 0) return;

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Added)
                {
                    var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "TenantId");
                    if (prop != null)
                    {
                        prop.CurrentValue = _tenantId;
                    }
                }
            }
        }
    }
}
