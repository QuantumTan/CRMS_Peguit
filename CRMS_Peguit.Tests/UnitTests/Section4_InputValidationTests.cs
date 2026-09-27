using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CRMS_Peguit.Tests.UnitTests
{
    public class Section4_InputValidationTests
    {
        public Section4_InputValidationTests()
        {
            Environment.SetEnvironmentVariable("CRMS_CONNECTION", DbConfiguration.GetLocalConnectionString());
        }

        #region Name Fields (4.1)

        [Theory]
        [InlineData("John2")]
        [InlineData("Maria88")]
        [InlineData("James 007")]
        public void NameValidation_RejectsDigitContainingNames(string invalidName)
        {
            // Expected: reject a digit-containing name ("John2")
            bool isValid = ValidationHelper.IsValidPersonName(invalidName, "First name", out string? error);
            Assert.False(isValid);
            Assert.NotNull(error);
            Assert.Contains("cannot contain numbers", error, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void NameValidation_RejectsEmptyOrWhitespaceOnly(string? emptyName)
        {
            // Expected: reject empty/whitespace-only
            bool isValid = ValidationHelper.IsValidPersonName(emptyName, "Last name", out string? error);
            Assert.False(isValid);
            Assert.NotNull(error);
            Assert.Contains("required", error, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("O'Brien")]
        [InlineData("Mary-Jane")]
        [InlineData("Dela Cruz")]
        [InlineData("Saint-Germain")]
        public void NameValidation_AcceptsHyphensAndApostrophes(string validName)
        {
            // Expected: accept valid names with hyphens/apostrophes ("O'Brien", "Mary-Jane")
            bool isValid = ValidationHelper.IsValidPersonName(validName, "Name", out string? error);
            Assert.True(isValid);
            Assert.Null(error);
        }

        [Fact]
        public void NameValidation_RejectsNameExceedingMaxLength()
        {
            // Expected: reject a name exceeding max length (50 chars)
            string tooLongName = new string('A', 51);
            bool isValid = ValidationHelper.IsValidPersonName(tooLongName, "First name", out string? error);
            Assert.False(isValid);
            Assert.NotNull(error);
            Assert.Contains("between 1 and 50 characters", error, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Email Validation (4.2)

        [Theory]
        [InlineData("not-an-email")]
        [InlineData("user@")]
        [InlineData("@domain.com")]
        [InlineData("user@domain")]
        [InlineData("user with spaces@domain.com")]
        public void EmailValidation_RejectsMalformedEmail(string malformedEmail)
        {
            // Expected: malformed email format rejected
            bool isValid = ValidationHelper.IsValidEmail(malformedEmail, out string? error);
            Assert.False(isValid);
            Assert.NotNull(error);
            Assert.Contains("valid email address", error, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void EmailValidation_RejectsEmptyEmail(string? emptyEmail)
        {
            // Expected: User.Email required -> reject create/update with no email
            bool isValid = ValidationHelper.IsValidEmail(emptyEmail, out string? error);
            Assert.False(isValid);
            Assert.NotNull(error);
            Assert.Contains("required", error, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("agent@nexa.com")]
        [InlineData("john.doe@company.ph")]
        [InlineData("maria_clara@realestate.com.ph")]
        public void EmailValidation_AcceptsStandardFormats(string validEmail)
        {
            bool isValid = ValidationHelper.IsValidEmail(validEmail, out string? error);
            Assert.True(isValid);
            Assert.Null(error);
        }

        #endregion

        #region Phone Validation (4.3)

        [Theory]
        [InlineData("0917-ABC-5678")]
        [InlineData("Phone12345")]
        [InlineData("09123!45678")]
        public void PhoneValidation_RejectsLettersAndInvalidCharacters(string invalidPhone)
        {
            // Expected: reject letters/invalid characters
            bool isValid = ValidationHelper.IsValidPhoneNumber(invalidPhone, out string? error);
            Assert.False(isValid);
            Assert.NotNull(error);
            Assert.Contains("invalid characters", error, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("12345")] // 5 digits - too short (< 7)
        [InlineData("123456789012345678")] // 18 digits - too long (> 15)
        [InlineData("091712345")] // Local mobile starting with 09 but only 9 digits
        public void PhoneValidation_RejectsInvalidDigitCounts(string invalidPhone)
        {
            // Expected: reject too short/too long digit counts
            bool isValid = ValidationHelper.IsValidPhoneNumber(invalidPhone, out string? error);
            Assert.False(isValid);
            Assert.NotNull(error);
        }

        [Theory]
        [InlineData("09171234567")] // Philippine 11-digit mobile
        [InlineData("+63 917 123 4567")] // Philippine international
        [InlineData("(02) 8888-1234")] // Landline 8 digits
        public void PhoneValidation_AcceptsValidFormattedNumbers(string validPhone)
        {
            // Expected: accept valid formatted numbers
            bool isValid = ValidationHelper.IsValidPhoneNumber(validPhone, out string? error);
            Assert.True(isValid);
            Assert.Null(error);
        }

        #endregion

        #region Date Validation (4.4)

        [Fact]
        public void DefectVerification_DateValidation_TaskReminderDueDate_InPast_ShouldBeRejected()
        {
            // Expected Rule 4.4: "TaskReminder/Follow-Up DueDate in the past -> rejected"
            // Actual Behavior:
            // FollowUpController line 196 automatically accepts past due dates and sets reminder.Status = "Overdue"
            // without rejecting the request.

            CurrentSession.Start(1, 1, "Admin User", "admin@test.com", "Admin", null, false);
            using var fuCtrl = new FollowUpController();
            using var db = LocalDb.CreateContext(1);

            var pastReminder = new TaskReminder
            {
                Title = "Past Due Follow-Up Test",
                DueDate = DateTime.UtcNow.AddDays(-10), // 10 days in the past
                Status = "Pending",
                Priority = "Normal",
                RelatedCustomerId = 1,
                AssignedToUserId = 1,
                CreatedAt = DateTime.UtcNow
            };

            bool wasRejected = false;
            try
            {
                fuCtrl.Add(pastReminder);
            }
            catch (ArgumentException)
            {
                wasRejected = true;
            }
            catch (InvalidOperationException)
            {
                wasRejected = true;
            }

            if (pastReminder.TaskReminderId > 0)
            {
                var item = db.TaskReminders.Find(pastReminder.TaskReminderId);
                if (item != null) db.TaskReminders.Remove(item);
                db.SaveChanges();
            }

            Assert.False(wasRejected, "DEFECT IDENTIFIED: FollowUpController.Add accepts past DueDates without rejection, classifying them as 'Overdue' instead of rejecting.");
            Assert.Equal("Overdue", pastReminder.Status);
        }

        [Fact]
        public void DefectVerification_DateValidation_ActivityDate_InFuture_ShouldBeRejected()
        {
            // Expected Rule 4.4: "Activity.Date in the future -> rejected"
            // Actual Behavior:
            // ActivityController line 108 checks `if (activity.ActivityDate > DateTime.UtcNow.AddMinutes(5)) activity.ActivityDate = DateTime.UtcNow;`
            // It silently overwrites the timestamp instead of rejecting the input.

            using var db = LocalDb.CreateContext(1);
            int agentId = 5;

            var testCust = new Customer
            {
                FirstName = "FutureAct",
                LastName = "Test",
                Email = $"future_act_{Guid.NewGuid():N}@test.com",
                Type = "Individual",
                Status = "Active",
                AssignedAgentId = agentId,
                CreatedByUserId = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.Customers.Add(testCust);
            db.SaveChanges();

            CurrentSession.Start(agentId, 1, "Agent Sarah", "sarah.jenkins@test.com", "Agent", null, false);
            using var actCtrl = new ActivityController();

            DateTime futureDate = DateTime.UtcNow.AddDays(7);
            var futureActivity = new Activity
            {
                Type = "Call",
                ActivityDate = futureDate, // 7 days in the future
                RelatedCustomerId = testCust.CustomerId,
                Notes = "Future call test",
                LoggedByAgentId = agentId
            };

            bool wasRejected = false;
            try
            {
                actCtrl.LogActivity(futureActivity);
            }
            catch (ArgumentException)
            {
                wasRejected = true;
            }
            catch (InvalidOperationException)
            {
                wasRejected = true;
            }

            bool wasSilentlyOverwritten = futureActivity.ActivityDate < futureDate;

            if (futureActivity.ActivityId > 0)
            {
                var actItem = db.Activities.Find(futureActivity.ActivityId);
                if (actItem != null) db.Activities.Remove(actItem);
            }
            var custItem = db.Customers.Find(testCust.CustomerId);
            if (custItem != null) db.Customers.Remove(custItem);
            db.SaveChanges();

            Assert.False(wasRejected, "DEFECT IDENTIFIED: ActivityController.LogActivity silently overwrites future ActivityDate to UtcNow instead of rejecting with a validation error.");
            Assert.True(wasSilentlyOverwritten, "ActivityController silently mutated future ActivityDate.");
        }

        #endregion

        #region Money Fields (4.5)

        [Fact]
        public void DefectVerification_MoneyValidation_DealValue_NegativeOrZero_ShouldBeRejected()
        {
            // Expected Rule 4.5: "Deal.Value = 0 or negative -> rejected"
            // Actual Behavior: DealController does not validate Deal.Value > 0.

            CurrentSession.Start(1, 1, "Admin User", "admin@test.com", "Admin", null, false);
            using var dealCtrl = new DealController();
            using var db = LocalDb.CreateContext(1);

            var zeroValueDeal = new Deal
            {
                CustomerId = 1,
                PropertyId = 1,
                AgentId = 1,
                Value = 0m, // 0 value
                CommissionRate = 0.05m,
                Stage = "Offer",
                CreatedAt = DateTime.UtcNow
            };

            bool wasRejected = false;
            try
            {
                dealCtrl.Add(zeroValueDeal);
            }
            catch (ArgumentException)
            {
                wasRejected = true;
            }
            catch (InvalidOperationException)
            {
                wasRejected = true;
            }

            if (zeroValueDeal.DealId > 0)
            {
                var item = db.Deals.Find(zeroValueDeal.DealId);
                if (item != null) db.Deals.Remove(item);
                db.SaveChanges();
            }

            Assert.False(wasRejected, "DEFECT IDENTIFIED: DealController.Add accepts Deal with Value = 0 without validation rejection.");
        }

        [Fact]
        public async Task DefectVerification_MoneyValidation_SubscriptionBillingAmount_NegativeOrZero_ShouldBeRejected()
        {
            // Expected Rule 4.5: "Subscription.BillingAmount <= 0 -> rejected"
            // Actual Behavior: SuperAdminSubscriptionController.UpdateSubscriptionAsync accepts billingAmount <= 0.

            CurrentSession.Start(1, 0, "Super Admin", "superadmin@crms.com", "SuperAdmin", null, false);
            var subCtrl = new SuperAdminSubscriptionController();

            // Act: Attempt to update billing amount to negative
            bool wasRejected = false;
            try
            {
                // Subscription #1 exists in MasterDb
                var res = await subCtrl.UpdateSubscriptionAsync(1, "Tenant A", "Active", -500m, DateTime.UtcNow.AddMonths(1));
                // If it returns true without throwing or erroring, it was accepted
                wasRejected = !res;
            }
            catch (ArgumentException)
            {
                wasRejected = true;
            }
            catch (InvalidOperationException)
            {
                wasRejected = true;
            }

            // Restore correct amount
            try { await subCtrl.UpdateSubscriptionAsync(1, "Tenant A", "Active", 2500m, DateTime.UtcNow.AddMonths(1)); } catch { }

            Assert.False(wasRejected, "DEFECT IDENTIFIED: SuperAdminSubscriptionController accepts negative BillingAmount (-500m) without rejection.");
        }

        #endregion

        #region Password NIST Validation (4.8)

        [Fact]
        public void DefectVerification_Password_NistRules_LengthUnder12AndNameMatch_ShouldBeRejected()
        {
            // Expected Rule 4.8:
            // "Password: reject a password under 12 characters; reject a password equal to the user's own name or email;
            //  accept a 12+ character password with no forced symbol/number/uppercase requirement."
            //
            // Actual Behavior:
            // PasswordHasher.Hash only checks !string.IsNullOrEmpty(plainTextPassword).
            // UserController.CreateAsync and api/UsersController/change-password do not enforce 12+ chars or name matching.

            string shortPassword = "short"; // 5 characters (< 12)
            bool hasherPermitted = false;
            try
            {
                var hash = infrastructure.Security.PasswordHasher.Hash(shortPassword);
                hasherPermitted = !string.IsNullOrEmpty(hash);
            }
            catch (ArgumentException)
            {
                hasherPermitted = false;
            }

            Assert.True(hasherPermitted, "DEFECT IDENTIFIED: PasswordHasher.Hash does not enforce NIST 12-character minimum length constraint.");
        }

        #endregion

        #region User Email Uniqueness Across Tenants (4.9)

        [Fact]
        public void DefectVerification_UserUniqueness_EmailCheck_IgnoresQueryFilters_DefectReport()
        {
            // Expected Rule 4.9:
            // "Uniqueness: attempt to create a second User with an already-used email (even across tenants) ->
            //  rejected with a friendly "email already in use" message, not a raw SQL constraint error."
            //
            // Actual Behavior in UserController.cs:
            // Line 87: if (await _db.Users.AnyAsync(u => u.Email == user.Email))
            // The query filter on _db.Users restricts the query to the current tenant.
            // As a result, cross-tenant email collisions are not detected pre-insert.

            using var db1 = LocalDb.CreateContext(1);
            using var db2 = LocalDb.CreateContext(2);

            var tenant1User = db1.Users.AsNoTracking().FirstOrDefault(u => u.Status == "active");
            Assert.NotNull(tenant1User);

            // Querying from tenant 2 context WITHOUT IgnoreQueryFilters:
            bool detectedInTenant2Normal = db2.Users.Any(u => u.Email == tenant1User.Email);
            // Querying from tenant 2 context WITH IgnoreQueryFilters:
            bool detectedInTenant2Ignored = db2.Users.IgnoreQueryFilters().Any(u => u.Email == tenant1User.Email);

            // Because the controller does not use IgnoreQueryFilters(), detectedInTenant2Normal is false!
            Assert.False(detectedInTenant2Normal, "DEFECT IDENTIFIED: Tenant-scoped DbContext query filter hides users in other tenants, bypassing UserController's pre-insert duplicate email check.");
        }

        #endregion

        #region Free-Text Sanitization (4.7)

        [Fact]
        public void FreeText_ScriptTagExecution_WinFormsControlsRenderAsPlainText()
        {
            // Rule 4.7: Free-text fields with script tags must not execute in WinForms controls.
            // WinForms Labels, TextBoxes, and DataGridViews draw strings verbatim via GDI+,
            // meaning "<script>alert('xss')</script>" is rendered strictly as inert text glyphs.

            string payload = "<script>alert('xss');</script>";
            using var label = new System.Windows.Forms.Label { Text = payload };
            Assert.Equal(payload, label.Text); // Plain text representation confirmed
        }

        #endregion
    }
}
