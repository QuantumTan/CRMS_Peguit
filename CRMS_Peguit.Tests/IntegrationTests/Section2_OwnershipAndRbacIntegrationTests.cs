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

namespace CRMS_Peguit.Tests.IntegrationTests
{
    public class Section2_OwnershipAndRbacIntegrationTests
    {
        public Section2_OwnershipAndRbacIntegrationTests()
        {
            Environment.SetEnvironmentVariable("CRMS_CONNECTION", DbConfiguration.GetLocalConnectionString());
        }

        #region 2.1 - 2.4 Frontline Ownership & Scoping (Customer, Lead, Deal, Property, SupportTicket, FollowUp)

        [Fact]
        public void Rbac_AgentACreatesRecord_StartsUnassigned_VisibleOnlyToAgentA()
        {
            // Requirement 2.1:
            // "Agent A creates a record -> starts Unassigned/Pending, visible only to Agent A;
            //  Agent B (same tenant) cannot see or query it."

            int agentAId = 5;
            int agentBId = 6;

            // Agent A Session
            CurrentSession.Start(agentAId, 1, "Agent A", "agentA@test.com", "Agent", null, false);
            Assert.True(RbacService.IsAgent);
            Assert.False(RbacService.HasFullOversight);

            using var custCtrl = new CustomerController();
            using var db = LocalDb.CreateContext(1);

            var customer = new Customer
            {
                FirstName = "Ownership",
                LastName = $"Customer_{Guid.NewGuid():N}",
                Email = $"obac_{Guid.NewGuid():N}@test.com",
                Type = "Individual",
                Status = "Active",
                CreatedByUserId = agentAId,
                CreatedAt = DateTime.UtcNow
            };
            custCtrl.Add(customer);

            // Record starts Unassigned / Pending
            Assert.Null(customer.AssignedAgentId);
            Assert.Equal("pending_review", customer.AssignmentStatus);

            // Visibility for Agent A (creator of pending record) -> Visible
            bool agentACanSee = RbacService.CanAgentViewRecord(customer.AssignedAgentId, customer.CreatedByUserId);
            Assert.True(agentACanSee);

            // Visibility for Agent B -> NOT visible
            CurrentSession.Start(agentBId, 1, "Agent B", "agentB@test.com", "Agent", null, false);
            bool agentBCanSee = RbacService.CanAgentViewRecord(customer.AssignedAgentId, customer.CreatedByUserId);
            Assert.False(agentBCanSee);

            // Clean up
            var dbCust = db.Customers.Find(customer.CustomerId);
            if (dbCust != null) db.Customers.Remove(dbCust);
            db.SaveChanges();
        }

        [Fact]
        public void Rbac_ManagerAssignsToAgentB_VisibilityTransfersToAgentB_HiddenFromAgentA()
        {
            // Requirement 2.2:
            // "Manager assigns the record to Agent B -> now visible only to Agent B;
            //  Agent A can no longer see it; Agent C still cannot see it."

            int agentAId = 5;
            int agentBId = 6;
            int agentCId = 7;
            int managerId = 2;

            // Created by Agent A
            int? createdBy = agentAId;
            int? assignedAgent = null;

            // Initially visible only to Agent A
            CurrentSession.Start(agentAId, 1, "Agent A", "agentA@test.com", "Agent", null, false);
            Assert.True(RbacService.CanAgentViewRecord(assignedAgent, createdBy));

            // Manager assigns to Agent B
            CurrentSession.Start(managerId, 1, "Manager User", "manager@test.com", "Manager", null, false);
            Assert.True(RbacService.CanAssignRecords);
            assignedAgent = agentBId;

            // Visibility checks:
            // Agent B (assignee) -> Visible
            CurrentSession.Start(agentBId, 1, "Agent B", "agentB@test.com", "Agent", null, false);
            Assert.True(RbacService.CanAgentViewRecord(assignedAgent, createdBy));

            // Agent A (former creator, but record now assigned elsewhere) -> Hidden!
            CurrentSession.Start(agentAId, 1, "Agent A", "agentA@test.com", "Agent", null, false);
            Assert.False(RbacService.CanAgentViewRecord(assignedAgent, createdBy));

            // Agent C (unrelated agent) -> Hidden!
            CurrentSession.Start(agentCId, 1, "Agent C", "agentC@test.com", "Agent", null, false);
            Assert.False(RbacService.CanAgentViewRecord(assignedAgent, createdBy));
        }

        [Fact]
        public void Rbac_AgentAttemptReassignment_IsBlocked()
        {
            // Requirement 2.3:
            // "Agent B attempts to reassign the record to themselves or anyone else -> blocked (assignment is Manager/Admin only)."

            CurrentSession.Start(6, 1, "Agent B", "agentB@test.com", "Agent", null, false);
            Assert.False(RbacService.CanAssignRecords);

            // Attempting to invoke assignment as an Agent must be disallowed
            Assert.False(RbacService.CanAssignRecords);
        }

        [Fact]
        public void Rbac_ManagerAndAdmin_RetainFullOversightAcrossAllRecords()
        {
            // Requirement 2.4:
            // "Manager and Admin can see and act on ALL records in their tenant regardless of assignment
            //  (oversight override) -> confirmed for every entity type above."

            // Test Manager
            CurrentSession.Start(2, 1, "Manager User", "manager@test.com", "Manager", null, false);
            Assert.True(RbacService.HasFullOversight);
            Assert.True(RbacService.CanAgentViewRecord(999, 888)); // Arbitrary agent & creator

            // Test Admin
            CurrentSession.Start(1, 1, "Admin User", "admin@test.com", "Admin", null, false);
            Assert.True(RbacService.HasFullOversight);
            Assert.True(RbacService.CanAgentViewRecord(999, 888));
        }

        #endregion

        #region 2.7 Follow-Up Reassignment Transfer

        [Fact]
        public void Rbac_ReassigningCustomer_TransfersOpenFollowUpsAutomatically()
        {
            // Requirement 2.7:
            // "Follow-Up specific: reassigning the underlying Customer/Lead also transfers its open Follow-Ups
            //  to the new Agent automatically."

            CurrentSession.Start(1, 1, "Admin User", "admin@test.com", "Admin", null, false);
            using var custCtrl = new CustomerController();
            using var db = LocalDb.CreateContext(1);

            int agentAId = 5;
            int agentBId = 6;

            var customer = new Customer
            {
                FirstName = "FollowUpTransfer",
                LastName = $"Cust_{Guid.NewGuid():N}",
                Email = $"fu_{Guid.NewGuid():N}@test.com",
                Type = "Individual",
                Status = "Active",
                AssignedAgentId = agentAId,
                CreatedByUserId = 1,
                CreatedAt = DateTime.UtcNow
            };
            custCtrl.Add(customer);

            // Create open follow-up assigned to Agent A
            var followUp = new TaskReminder
            {
                Title = "Initial Follow Up",
                DueDate = DateTime.UtcNow.AddDays(2),
                Status = "Pending",
                Priority = "High",
                RelatedCustomerId = customer.CustomerId,
                AssignedToUserId = agentAId,
                CreatedAt = DateTime.UtcNow
            };
            db.TaskReminders.Add(followUp);
            db.SaveChanges();

            // Act: Reassign customer from Agent A to Agent B
            custCtrl.AssignAgent(customer, agentBId, approve: true, notes: "Transferring to Agent B");

            // Assert: The open follow-up was transferred to Agent B automatically
            db.Entry(followUp).Reload();
            Assert.Equal(agentBId, followUp.AssignedToUserId);

            // Clean up
            db.TaskReminders.Remove(followUp);
            var dbCust = db.Customers.Find(customer.CustomerId);
            if (dbCust != null) db.Customers.Remove(dbCust);
            db.SaveChanges();
        }

        #endregion

        #region 2.8 - 2.9 Menu Uniformity & Admin Direct CRUD Restriction

        [Fact]
        public void Rbac_AllAgents_HaveIdenticalAvailableActionsAndMenus()
        {
            // Requirement 2.8:
            // "Confirm ALL Agents have IDENTICAL available actions/menus (GetAccessibleModules() returns
            //  the same list for every Agent) — the only variance across two Agent test accounts is which
            //  records they can see, never which buttons/features exist."

            var agent1 = new SalesStaff("Agent 1", "agent1@test.com");
            var agent2 = new SalesStaff("Agent 2", "agent2@test.com");

            var modules1 = agent1.GetAccessibleModules();
            var modules2 = agent2.GetAccessibleModules();

            Assert.Equal(modules1.Count, modules2.Count);
            Assert.True(modules1.SequenceEqual(modules2));
        }

        [Fact]
        public void Rbac_AdminCannotDirectlyCreateOrEditSalesRecords()
        {
            // Requirement 2.9:
            // "Admin cannot directly create/edit a Customer, Lead, Property, or Deal through any exposed action
            //  (their role only reaches these via reporting/oversight, never direct CRUD)."

            CurrentSession.Start(1, 1, "Admin User", "admin@test.com", "Admin", null, false);
            Assert.True(RbacService.IsAdmin);

            // Admin cannot create operational sales records
            Assert.False(RbacService.CanCreateSalesRecord);

            // Admin cannot edit records (oversight only, management/agents edit)
            Assert.False(RbacService.CanEditRecord(1, 1));
            Assert.False(RbacService.CanArchiveRecord(1, 1));
        }

        #endregion
    }
}
