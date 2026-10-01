using System;
using System.Windows.Forms;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Models.Roles;
using CRMS_Peguit.winforms.Services;
using CRMS_Peguit.winforms.Views.Shared;
using Xunit;

namespace CRMS_Peguit.Tests.UnitTests
{
    public class EmailTemplateRestrictionTests
    {
        [Fact]
        public void Agent_CannotCreateOrSaveEmailTemplate()
        {
            // Set session to Sales Agent
            CurrentSession.Start(
                userId: 1034,
                tenantId: 1,
                fullName: "Tenant A Agent",
                email: "tenanta_agent@test.com",
                roleName: "SalesStaff",
                jwtToken: null,
                isOffline: false,
                tier: TenantTier.TenantA,
                tenantName: "Apex Realty");

            var tpl = new EmailTemplate
            {
                Name = "Agent Custom Template",
                Category = "Outreach",
                Subject = "Custom Subject",
                Body = "Custom Body",
                IsActive = true
            };

            bool saved = EmailTemplateService.Instance.SaveTemplate(tpl, 1, out string errorMessage);

            Assert.False(saved);
            Assert.Contains("Access Denied", errorMessage);
        }

        [Fact]
        public void EmailMessageForm_AgentMode_EnforcesReadOnlySubjectAndBody()
        {
            // Set session to Sales Agent
            CurrentSession.Start(
                userId: 1034,
                tenantId: 1,
                fullName: "Tenant A Agent",
                email: "tenanta_agent@test.com",
                roleName: "SalesStaff",
                jwtToken: null,
                isOffline: false,
                tier: TenantTier.TenantA,
                tenantName: "Apex Realty");

            using var form = new EmailMessageForm("Jane Doe", "jane@example.com");

            var txtSubject = form.Controls.Find("txtSubject", true);
            var txtBody = form.Controls.Find("txtBody", true);
            var cboTemplate = form.Controls.Find("cboTemplate", true);
            var pnlNotice = form.Controls.Find("pnlNotice", true);

            Assert.NotEmpty(txtSubject);
            Assert.NotEmpty(txtBody);
            Assert.NotEmpty(cboTemplate);
            Assert.NotEmpty(pnlNotice);

            var subjectBox = (TextBox)txtSubject[0];
            var bodyBox = (TextBox)txtBody[0];
            var templateCombo = (ComboBox)cboTemplate[0];

            // Agent must have read-only subject and body
            Assert.True(subjectBox.ReadOnly, "Agent should not be able to edit email subject.");
            Assert.True(bodyBox.ReadOnly, "Agent should not be able to edit email body.");
            Assert.True(templateCombo.DropDownStyle == ComboBoxStyle.DropDownList, "Template selector should be a fixed list.");
        }

        [Fact]
        public void EmailMessageForm_AdminMode_AllowsCustomEditing()
        {
            // Set session to Admin
            CurrentSession.Start(
                userId: 1,
                tenantId: 1,
                fullName: "System Admin",
                email: "admin@test.com",
                roleName: "Admin",
                jwtToken: null,
                isOffline: false,
                tier: TenantTier.TenantA,
                tenantName: "Apex Realty");

            using var form = new EmailMessageForm("Jane Doe", "jane@example.com");

            var txtSubject = form.Controls.Find("txtSubject", true);
            var txtBody = form.Controls.Find("txtBody", true);

            Assert.NotEmpty(txtSubject);
            Assert.NotEmpty(txtBody);

            var subjectBox = (TextBox)txtSubject[0];
            var bodyBox = (TextBox)txtBody[0];

            Assert.False(subjectBox.ReadOnly, "Admin should be able to edit email subject.");
            Assert.False(bodyBox.ReadOnly, "Admin should be able to edit email body.");
        }
    }
}
