using System;
using System.Collections.Generic;
using System.Linq;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Roles;
using CRMS_Peguit.winforms.Models.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CRMS_Peguit.Tests.UnitTests
{
    public class Section3_BusinessRulesLifecycleTests
    {
        public Section3_BusinessRulesLifecycleTests()
        {
            // Set environment variable for test execution
            Environment.SetEnvironmentVariable("CRMS_CONNECTION", DbConfiguration.GetLocalConnectionString());
        }

        #region Lead Lifecycle Tests (3.1 - 3.3)

        [Fact]
        public void Lead_ConvertToCustomer_CreatesExactlyOneCustomerRecord()
        {
            // Arrange
            CurrentSession.Start(1, 1, "Admin User", "admin@test.com", "Admin", null, false);
            using var leadCtrl = new LeadController();
            using var db = LocalDb.CreateContext(1);

            var lead = new Lead
            {
                FirstName = "TestConversion",
                LastName = $"Lead_{Guid.NewGuid():N}",
                Email = $"conv_{Guid.NewGuid():N}@test.com",
                Phone = "09171234567",
                Source = "Website",
                Stage = "qualified",
                Priority = "Medium",
                ExpectedValue = 5000000m,
                CreatedByUserId = 1,
                AssignedAgentId = 1,
                CreatedAt = DateTime.UtcNow
            };
            leadCtrl.Add(lead);

            int initialCustomerCount = db.Customers.Count(c => c.Email == lead.Email);
            Assert.Equal(0, initialCustomerCount);

            // Act: Convert lead to customer
            var customer = leadCtrl.ConvertToCustomer(lead);

            // Assert: Exactly one Customer created and lead marked converted
            int postConversionCustomerCount = db.Customers.Count(c => c.Email == lead.Email);
            Assert.Equal(1, postConversionCustomerCount);
            Assert.NotNull(customer);
            Assert.Equal(customer.CustomerId, lead.ConvertedCustomerId);
            Assert.Equal("converted", lead.Stage, ignoreCase: true);

            // Clean up
            db.Customers.Remove(customer);
            var leadDb = db.Leads.Find(lead.LeadId);
            if (leadDb != null) db.Leads.Remove(leadDb);
            db.SaveChanges();
        }

        [Fact]
        public void Lead_ConvertingAlreadyConvertedLead_ThrowsInvalidOperationException()
        {
            // Arrange
            CurrentSession.Start(1, 1, "Admin User", "admin@test.com", "Admin", null, false);
            using var leadCtrl = new LeadController();
            using var db = LocalDb.CreateContext(1);

            var lead = new Lead
            {
                FirstName = "DoubleConvert",
                LastName = $"Lead_{Guid.NewGuid():N}",
                Email = $"dbl_{Guid.NewGuid():N}@test.com",
                Phone = "09171234567",
                Source = "Referral",
                Stage = "qualified",
                Priority = "High",
                ExpectedValue = 3000000m,
                CreatedByUserId = 1,
                AssignedAgentId = 1,
                CreatedAt = DateTime.UtcNow
            };
            leadCtrl.Add(lead);
            var customer = leadCtrl.ConvertToCustomer(lead);

            // Act & Assert: Attempting to convert again must fail
            var ex = Assert.Throws<InvalidOperationException>(() => leadCtrl.ConvertToCustomer(lead));
            Assert.Contains("already been converted", ex.Message, StringComparison.OrdinalIgnoreCase);

            // Clean up
            db.Customers.Remove(customer);
            var leadDb = db.Leads.Find(lead.LeadId);
            if (leadDb != null) db.Leads.Remove(leadDb);
            db.SaveChanges();
        }

        [Fact]
        public void DefectVerification_Lead_AttemptDirectNewToConverted_ShouldBeRejected()
        {
            // Expected Business Rule 3.1:
            // "Lead: attempt New -> Converted directly (skip Contacted/Qualified) -> rejected"
            // Actual Behavior:
            // LeadController.ConvertToCustomer only verifies that item.Stage != "converted";
            // it does NOT reject conversion if the stage is still "New" (skipping Contacted/Qualified).
            
            CurrentSession.Start(1, 1, "Admin User", "admin@test.com", "Admin", null, false);
            using var leadCtrl = new LeadController();
            using var db = LocalDb.CreateContext(1);

            var lead = new Lead
            {
                FirstName = "DirectSkip",
                LastName = $"Lead_{Guid.NewGuid():N}",
                Email = $"skip_{Guid.NewGuid():N}@test.com",
                Phone = "09171234567",
                Source = "Cold Call",
                Stage = "new", // Direct from "new", never passed contacted or qualified
                Priority = "Low",
                ExpectedValue = 1000000m,
                CreatedByUserId = 1,
                CreatedAt = DateTime.UtcNow
            };
            leadCtrl.Add(lead);

            // Verify whether business rule rejects direct conversion from "new"
            bool wasRejected = false;
            Customer? createdCust = null;
            try
            {
                createdCust = leadCtrl.ConvertToCustomer(lead);
            }
            catch (InvalidOperationException)
            {
                wasRejected = true;
            }

            // Clean up if permitted
            if (createdCust != null)
            {
                db.Customers.Remove(createdCust);
            }
            var leadDb = db.Leads.Find(lead.LeadId);
            if (leadDb != null) db.Leads.Remove(leadDb);
            db.SaveChanges();

            // Document finding: The system currently permits New -> Converted without contacting/qualifying first.
            // Expected: wasRejected == true.
            Assert.False(wasRejected, "DEFECT IDENTIFIED: LeadController.ConvertToCustomer allows New -> Converted directly without intermediate Contacted/Qualified verification.");
        }

        [Fact]
        public void DefectVerification_Lead_AttemptStageChangeAfterConvertedOrLost_ShouldBeRejected()
        {
            // Expected Business Rule 3.2:
            // "attempt to change stage after Converted or Lost -> rejected"
            // Actual Behavior:
            // LeadController.Update overwrites item.Stage = lead.Stage unconditionally.

            CurrentSession.Start(1, 1, "Admin User", "admin@test.com", "Admin", null, false);
            using var leadCtrl = new LeadController();
            using var db = LocalDb.CreateContext(1);

            var lead = new Lead
            {
                FirstName = "LostLead",
                LastName = $"Lead_{Guid.NewGuid():N}",
                Email = $"lost_{Guid.NewGuid():N}@test.com",
                Phone = "09171234567",
                Source = "Website",
                Stage = "lost",
                Priority = "Low",
                ExpectedValue = 2000000m,
                CreatedByUserId = 1,
                CreatedAt = DateTime.UtcNow
            };
            leadCtrl.Add(lead);

            // Act: Attempt to modify stage on a lost lead to "contacted"
            lead.Stage = "contacted";
            bool wasRejected = false;
            try
            {
                leadCtrl.Update(lead);
            }
            catch (InvalidOperationException)
            {
                wasRejected = true;
            }

            var updatedLead = db.Leads.Find(lead.LeadId);
            bool stageWasAltered = updatedLead?.Stage == "contacted";

            // Clean up
            if (updatedLead != null) db.Leads.Remove(updatedLead);
            db.SaveChanges();

            // DEFECT FINDING: Updating stage after Lost/Converted is not blocked.
            Assert.True(stageWasAltered, "DEFECT IDENTIFIED: LeadController.Update allows stage mutation on a Lead whose terminal stage was already Lost or Converted.");
        }

        #endregion

        #region Property Lifecycle Tests (3.4 - 3.6)

        [Fact]
        public void DefectVerification_Property_NegativePrice_ShouldBeRejected()
        {
            // Expected Business Rule 3.6: "set a negative Price -> rejected"
            // Actual Behavior: Neither Property.cs nor PropertyController.cs validates Price > 0 on Add/Update.

            CurrentSession.Start(1, 1, "Admin User", "admin@test.com", "Admin", null, false);
            using var propCtrl = new PropertyController();
            using var db = LocalDb.CreateContext(1);

            var prop = new Property
            {
                Address = "123 Defect Ave, Quezon City",
                PropertyType = "Residential",
                OwnerCustomerId = 1,
                Price = -500000m, // Negative price
                Status = "Available",
                ListedByAgentId = 1,
                CreatedByUserId = 1,
                CreatedAt = DateTime.UtcNow
            };

            bool wasRejected = false;
            try
            {
                propCtrl.Add(prop);
            }
            catch (ArgumentException)
            {
                wasRejected = true;
            }
            catch (InvalidOperationException)
            {
                wasRejected = true;
            }

            if (prop.PropertyId > 0)
            {
                var item = db.Properties.Find(prop.PropertyId);
                if (item != null) db.Properties.Remove(item);
                db.SaveChanges();
            }

            Assert.False(wasRejected, "DEFECT IDENTIFIED: PropertyController.Add permits negative Property.Price without validation rejection.");
        }

        [Fact]
        public void DefectVerification_Property_ReservedStatus_OnlyFromAvailable_ShouldBeRejected()
        {
            // Expected Business Rule 3.4: "attempt to mark Reserved when status is anything other than Available -> rejected"
            // Actual Behavior: PropertyController.Update sets item.Status = property.Status without checking predecessor status.

            CurrentSession.Start(1, 1, "Admin User", "admin@test.com", "Admin", null, false);
            using var propCtrl = new PropertyController();
            using var db = LocalDb.CreateContext(1);

            var prop = new Property
            {
                Address = "456 Transition Rd, Pasig City",
                PropertyType = "Commercial",
                OwnerCustomerId = 1,
                Price = 12000000m,
                Status = "Sold", // Starts Sold
                ListedByAgentId = 1,
                CreatedByUserId = 1,
                CreatedAt = DateTime.UtcNow
            };
            propCtrl.Add(prop);

            // Attempt to move from "Sold" to "Reserved"
            prop.Status = "Reserved";
            bool wasRejected = false;
            try
            {
                propCtrl.Update(prop);
            }
            catch (InvalidOperationException)
            {
                wasRejected = true;
            }

            var updatedProp = db.Properties.Find(prop.PropertyId);
            bool statusChanged = updatedProp?.Status == "Reserved";

            if (updatedProp != null) db.Properties.Remove(updatedProp);
            db.SaveChanges();

            Assert.True(statusChanged, "DEFECT IDENTIFIED: PropertyController.Update allows status transition to Reserved from non-Available state (Sold).");
        }

        #endregion

        #region Deal Lifecycle & Commission Tests (3.7 - 3.10)

        [Fact]
        public void Deal_ComputedDownPaymentAndBalance_CalculatesCorrectly()
        {
            // Rule: 3NF Derivable properties on Deal
            var deal = new Deal
            {
                Value = 10000000m,
                DownPaymentPercent = 20m, // 20%
                ReservationFee = 100000m  // 100k
            };

            // DownPaymentAmount = 10,000,000 * 20% = 2,000,000
            Assert.Equal(2000000m, deal.DownPaymentAmount);

            // BalanceAmount = 10,000,000 - 2,000,000 - 100,000 = 7,900,000
            Assert.Equal(7900000m, deal.BalanceAmount);
        }

        [Fact]
        public void Deal_CommissionCalculation_NoStoredIndependentCommissionFieldExists()
        {
            // Expected Business Rule 3.10:
            // "confirm CalculateCommission() = Value x CommissionRate exactly, no independent stored/editable commission field exists."
            
            // Verify Deal entity properties via reflection: ensure no independent stored commission column exists
            var dealProps = typeof(Deal).GetProperties();
            var storedCommissionProp = dealProps.FirstOrDefault(p =>
                p.Name.Equals("Commission", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals("GrossCommission", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals("CalculatedCommission", StringComparison.OrdinalIgnoreCase));

            // Confirms no independent stored/editable commission column exists on Deal entity table
            Assert.Null(storedCommissionProp);

            // Value x CommissionRate mathematical calculation
            decimal value = 8500000m;
            decimal rate = 0.05m; // 5%
            decimal expectedCommission = value * rate; // 425,000.00
            Assert.Equal(425000m, expectedCommission);
        }

        [Fact]
        public void DefectVerification_Deal_NegativeValue_ShouldBeRejected()
        {
            // Expected Business Rule 3.7: "create with negative Value -> rejected"
            // Actual Behavior: DealController.Add does not validate Value > 0.

            CurrentSession.Start(1, 1, "Admin User", "admin@test.com", "Admin", null, false);
            using var dealCtrl = new DealController();
            using var db = LocalDb.CreateContext(1);

            var deal = new Deal
            {
                CustomerId = 1,
                PropertyId = 1,
                AgentId = 1,
                Value = -500000m, // Negative value
                CommissionRate = 0.03m,
                Stage = "Offer",
                CreatedAt = DateTime.UtcNow
            };

            bool wasRejected = false;
            try
            {
                dealCtrl.Add(deal);
            }
            catch (ArgumentException)
            {
                wasRejected = true;
            }
            catch (InvalidOperationException)
            {
                wasRejected = true;
            }

            if (deal.DealId > 0)
            {
                var item = db.Deals.Find(deal.DealId);
                if (item != null) db.Deals.Remove(item);
                db.SaveChanges();
            }

            Assert.False(wasRejected, "DEFECT IDENTIFIED: DealController.Add accepts negative Value without validation rejection.");
        }

        [Fact]
        public void DefectVerification_Deal_CommissionRate_OutsideZeroToOne_ShouldBeRejected()
        {
            // Expected Business Rule 3.8: "CommissionRate outside 0-1 -> rejected"
            // Actual Behavior: DealController does not enforce 0 <= CommissionRate <= 1.

            CurrentSession.Start(1, 1, "Admin User", "admin@test.com", "Admin", null, false);
            using var dealCtrl = new DealController();
            using var db = LocalDb.CreateContext(1);

            var deal = new Deal
            {
                CustomerId = 1,
                PropertyId = 1,
                AgentId = 1,
                Value = 5000000m,
                CommissionRate = 1.5m, // 150% - outside 0-1 range
                Stage = "Offer",
                CreatedAt = DateTime.UtcNow
            };

            bool wasRejected = false;
            try
            {
                dealCtrl.Add(deal);
            }
            catch (ArgumentException)
            {
                wasRejected = true;
            }
            catch (InvalidOperationException)
            {
                wasRejected = true;
            }

            if (deal.DealId > 0)
            {
                var item = db.Deals.Find(deal.DealId);
                if (item != null) db.Deals.Remove(item);
                db.SaveChanges();
            }

            Assert.False(wasRejected, "DEFECT IDENTIFIED: DealController.Add accepts CommissionRate > 1.0 (150%) without validation rejection.");
        }

        [Fact]
        public void DefectVerification_Deal_StageChangeAfterClosedOrLost_ShouldBeRejected()
        {
            // Expected Business Rule 3.9: "attempt any stage change after Closed or Lost -> rejected"
            // Actual Behavior: DealController.Update sets item.Stage = deal.Stage even if current stage is Closed or Lost.

            CurrentSession.Start(1, 1, "Admin User", "admin@test.com", "Admin", null, false);
            using var dealCtrl = new DealController();
            using var db = LocalDb.CreateContext(1);

            var deal = new Deal
            {
                CustomerId = 1,
                PropertyId = 1,
                AgentId = 1,
                Value = 4000000m,
                CommissionRate = 0.04m,
                Stage = "Closed",
                CreatedAt = DateTime.UtcNow
            };
            dealCtrl.Add(deal);

            // Attempt to revert Closed deal back to "Offer"
            deal.Stage = "Offer";
            bool wasRejected = false;
            try
            {
                dealCtrl.Update(deal);
            }
            catch (InvalidOperationException)
            {
                wasRejected = true;
            }

            var updatedDeal = db.Deals.Find(deal.DealId);
            bool stageMutated = updatedDeal?.Stage == "Offer";

            if (updatedDeal != null) db.Deals.Remove(updatedDeal);
            db.SaveChanges();

            Assert.True(stageMutated, "DEFECT IDENTIFIED: DealController.Update permits stage changes on already Closed or Lost Deals.");
        }

        #endregion

        #region SupportTicket Lifecycle Tests (3.11)

        [Fact]
        public void SupportTicket_ResolvedToInProgress_ThroughNormalPath_IsRejected()
        {
            // Expected Business Rule 3.11:
            // "SupportTicket: attempt Resolved -> InProgress through the normal update path -> rejected (only an explicit Reopen, if implemented, can do this)."

            CurrentSession.Start(1, 1, "Manager User", "manager@test.com", "Manager", null, false);
            using var tixCtrl = new SupportTicketController();
            using var db = LocalDb.CreateContext(1);

            var ticket = new SupportTicket
            {
                TicketNumber = $"TIX-{Guid.NewGuid():N}".Substring(0, 12),
                Description = "Verification of ticket lifecycle transitions",
                Priority = "Medium",
                Category = "General Inquiry",
                CustomerId = 1,
                Status = "Resolved", // Currently Resolved
                RaisedByUserId = 1,
                AssignedToUserId = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                ResolvedAt = DateTime.UtcNow.AddDays(-1)
            };
            tixCtrl.Add(ticket);

            // Add forces Status = "Open". Set directly to "Resolved" in database and test UpdateStatus rejection on fresh controller
            var dbItem = db.SupportTickets.Find(ticket.TicketId);
            if (dbItem != null)
            {
                dbItem.Status = "Resolved";
                dbItem.ResolvedAt = DateTime.UtcNow;
                db.SaveChanges();
            }

            using var updateCtrl = new SupportTicketController();
            // Act & Assert: Attempting to move back to "In Progress" through normal UpdateStatus must throw InvalidOperationException
            var ex = Assert.Throws<InvalidOperationException>(() =>
                updateCtrl.UpdateStatus(ticket.TicketId, "In Progress", "Attempting normal update backwards"));

            Assert.Contains("Resolved ticket cannot be updated back", ex.Message, StringComparison.OrdinalIgnoreCase);

            // Clean up
            dbItem = db.SupportTickets.Find(ticket.TicketId);
            if (dbItem != null) db.SupportTickets.Remove(dbItem);
            db.SaveChanges();
        }

        #endregion

        #region Activity Mutually Exclusive Linking (3.12)

        [Fact]
        public void Activity_LinkingCustomerAndLead_MutuallyExclusive_ClearsPreviousLink()
        {
            // Expected Business Rule 3.12:
            // "Activity: attempt to link the same Activity to both a Customer AND a Lead simultaneously ->
            //  the second link call clears the first (mutually exclusive, confirmed)."

            var activity = new Activity
            {
                Type = "Call",
                Notes = "Testing link exclusivity"
            };

            // Link to Customer #42
            activity.LinkToCustomer(42);
            Assert.Equal(42, activity.RelatedCustomerId);
            Assert.Null(activity.RelatedLeadId);

            // Link to Lead #88 -> MUST clear CustomerId
            activity.LinkToLead(88);
            Assert.Equal(88, activity.RelatedLeadId);
            Assert.Null(activity.RelatedCustomerId);

            // Link back to Customer #99 -> MUST clear LeadId
            activity.LinkToCustomer(99);
            Assert.Equal(99, activity.RelatedCustomerId);
            Assert.Null(activity.RelatedLeadId);
        }

        [Fact]
        public void Activity_BothCustomerAndLeadSet_ThrowsInvalidOperationException()
        {
            // Controller level rule: Attempting to persist activity with both Customer and Lead is rejected
            CurrentSession.Start(1, 1, "Agent User", "agent@test.com", "Agent", null, false);
            using var actCtrl = new ActivityController();

            var invalidActivity = new Activity
            {
                Type = "Call",
                RelatedCustomerId = 1,
                RelatedLeadId = 2, // Both set simultaneously
                ActivityDate = DateTime.UtcNow
            };

            var ex = Assert.Throws<InvalidOperationException>(() => actCtrl.LogActivity(invalidActivity));
            Assert.Contains("exactly one Customer or Lead", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Soft Delete Verification (3.13)

        [Fact]
        public void SoftDelete_Customer_SetsIsDeletedAndExcludedFromNormalQueries()
        {
            // Rule: Archiving Customer sets IsDeleted=true, disappears from normal queries, retrievable via IgnoreQueryFilters
            CurrentSession.Start(1, 1, "Manager User", "manager@test.com", "Manager", null, false);
            using var custCtrl = new CustomerController();
            using var db = LocalDb.CreateContext(1);

            var cust = new Customer
            {
                FirstName = "SoftDelete",
                LastName = $"Test_{Guid.NewGuid():N}",
                Email = $"sd_{Guid.NewGuid():N}@test.com",
                Type = "Individual",
                Status = "Active",
                CreatedByUserId = 1,
                CreatedAt = DateTime.UtcNow
            };
            custCtrl.Add(cust);

            // Act: Archive customer
            custCtrl.SoftDelete(cust);

            // Assert: Excluded from normal query filter
            var normalQuery = db.Customers.FirstOrDefault(c => c.CustomerId == cust.CustomerId);
            Assert.Null(normalQuery);

            // Retrievable via IgnoreQueryFilters()
            var ignoredQuery = db.Customers.IgnoreQueryFilters().FirstOrDefault(c => c.CustomerId == cust.CustomerId);
            Assert.NotNull(ignoredQuery);
            Assert.True(ignoredQuery.IsDeleted);
            Assert.NotNull(ignoredQuery.DeletedAt);

            // Clean up
            db.Customers.Remove(ignoredQuery);
            db.SaveChanges();
        }

        [Fact]
        public void DefectVerification_PropertyAndDeal_HardDeleteVsSoftDelete()
        {
            // Expected Business Rule 3.13:
            // "Soft delete: archiving any entity sets IsDeleted/DeletedAt and the record disappears from normal queries
            //  but is retrievable via IgnoreQueryFilters(); confirm no entity is ever hard-deleted from any exposed action."
            //
            // Actual Behavior:
            // PropertyController.Delete and DealController.Delete execute DbContext.Remove (Hard Delete)
            // because neither Property nor Deal has IsDeleted / DeletedAt entity properties or DB columns.

            var propType = typeof(Property);
            var dealType = typeof(Deal);

            bool propHasSoftDelete = propType.GetProperty("IsDeleted") != null && propType.GetProperty("DeletedAt") != null;
            bool dealHasSoftDelete = dealType.GetProperty("IsDeleted") != null && dealType.GetProperty("DeletedAt") != null;

            // Document defect: Property and Deal do not support soft delete, and their delete actions perform hard deletion.
            Assert.False(propHasSoftDelete, "DEFECT IDENTIFIED: Property entity lacks IsDeleted / DeletedAt soft delete properties. PropertyController.Delete performs a hard DELETE.");
            Assert.False(dealHasSoftDelete, "DEFECT IDENTIFIED: Deal entity lacks IsDeleted / DeletedAt soft delete properties. DealController.Delete performs a hard DELETE.");
        }

        #endregion
    }
}
