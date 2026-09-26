using System;
using System.Collections.Generic;
using System.Linq;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Models.Services;
using Microsoft.EntityFrameworkCore;

namespace CRMS_Peguit.winforms.Services
{
    public class EmailTemplateService
    {
        private static readonly Lazy<EmailTemplateService> _instance =
            new(() => new EmailTemplateService());

        public static EmailTemplateService Instance => _instance.Value;

        public List<EmailTemplate> GetActiveTemplates(int tenantId = 1)
        {
            using var db = LocalDb.CreateContext(tenantId);
            return db.EmailTemplates
                .AsNoTracking()
                .Where(t => t.TenantId == tenantId && !t.IsDeleted && t.IsActive)
                .OrderBy(t => t.IsSystem ? 0 : 1)
                .ThenBy(t => t.Category)
                .ThenBy(t => t.Name)
                .ToList();
        }

        public EmailTemplate? GetTemplateById(int templateId, int tenantId = 1)
        {
            using var db = LocalDb.CreateContext(tenantId);
            return db.EmailTemplates
                .AsNoTracking()
                .FirstOrDefault(t => t.TemplateId == templateId && t.TenantId == tenantId && !t.IsDeleted);
        }

        public bool SaveTemplate(EmailTemplate template, int tenantId, out string errorMessage)
        {
            errorMessage = string.Empty;

            // 1. RBAC Guard: Agents cannot edit or create master templates
            if (RbacService.IsAgent)
            {
                errorMessage = "Access Denied: Sales Agents cannot create or modify organizational email templates.";
                return false;
            }

            // 2. Validation
            if (string.IsNullOrWhiteSpace(template.Name))
            {
                errorMessage = "Template Name is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(template.Subject))
            {
                errorMessage = "Email Subject Line is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(template.Body))
            {
                errorMessage = "Email Body content is required.";
                return false;
            }

            try
            {
                using var db = LocalDb.CreateContext(tenantId);

                if (template.TemplateId > 0)
                {
                    var existing = db.EmailTemplates.FirstOrDefault(t => t.TemplateId == template.TemplateId && t.TenantId == tenantId);
                    if (existing == null || existing.IsDeleted)
                    {
                        errorMessage = "Template not found.";
                        return false;
                    }

                    // System templates are locked against direct overwrite
                    if (existing.IsSystem)
                    {
                        errorMessage = "System-generated standard templates cannot be modified directly. Please use 'Clone as Custom' to adapt this template.";
                        return false;
                    }

                    existing.Name = template.Name.Trim();
                    existing.Category = string.IsNullOrWhiteSpace(template.Category) ? "Equity Retention" : template.Category.Trim();
                    existing.TargetAudience = string.IsNullOrWhiteSpace(template.TargetAudience) ? "All" : template.TargetAudience.Trim();
                    existing.EmailFormat = string.IsNullOrWhiteSpace(template.EmailFormat) ? "Html" : template.EmailFormat.Trim();
                    existing.Subject = template.Subject.Trim();
                    existing.Body = template.Body.Trim();
                    existing.CallToActionText = template.CallToActionText?.Trim();
                    existing.CallToActionUrl = template.CallToActionUrl?.Trim();
                    existing.IsActive = template.IsActive;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    template.TenantId = tenantId;
                    template.Name = template.Name.Trim();
                    template.Category = string.IsNullOrWhiteSpace(template.Category) ? "Equity Retention" : template.Category.Trim();
                    template.TargetAudience = string.IsNullOrWhiteSpace(template.TargetAudience) ? "All" : template.TargetAudience.Trim();
                    template.EmailFormat = string.IsNullOrWhiteSpace(template.EmailFormat) ? "Html" : template.EmailFormat.Trim();
                    template.Subject = template.Subject.Trim();
                    template.Body = template.Body.Trim();
                    template.CallToActionText = template.CallToActionText?.Trim();
                    template.CallToActionUrl = template.CallToActionUrl?.Trim();
                    template.IsSystem = false;
                    template.IsActive = true;
                    template.CreatedByRole = RbacService.IsAdmin ? "Admin" : (RbacService.IsManager ? "Manager" : "SuperAdmin");
                    template.CreatedByUserId = CurrentSession.UserId > 0 ? CurrentSession.UserId : null;
                    template.CreatedAt = DateTime.UtcNow;
                    template.UpdatedAt = DateTime.UtcNow;

                    db.EmailTemplates.Add(template);
                }

                db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Database error: {ex.Message}";
                return false;
            }
        }

        public bool SoftDeleteTemplate(int templateId, int tenantId, out string errorMessage)
        {
            errorMessage = string.Empty;

            // 1. RBAC Guard: Agents cannot delete templates
            if (RbacService.IsAgent)
            {
                errorMessage = "Access Denied: Sales Agents cannot delete email templates.";
                return false;
            }

            try
            {
                using var db = LocalDb.CreateContext(tenantId);
                var existing = db.EmailTemplates.FirstOrDefault(t => t.TemplateId == templateId && t.TenantId == tenantId);
                if (existing == null || existing.IsDeleted)
                {
                    errorMessage = "Template not found.";
                    return false;
                }

                // 2. Protection Guard: System templates cannot be deleted
                if (existing.IsSystem)
                {
                    errorMessage = "System-generated standard templates cannot be deleted. You may choose another template instead.";
                    return false;
                }

                // 3. Soft Delete
                existing.IsDeleted = true;
                existing.DeletedAt = DateTime.UtcNow;
                existing.DeletedByUserId = CurrentSession.UserId > 0 ? CurrentSession.UserId : null;

                db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Database error: {ex.Message}";
                return false;
            }
        }

        public EmailTemplate? CloneAsCustom(int sourceTemplateId, string newName, int tenantId, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (RbacService.IsAgent)
            {
                errorMessage = "Access Denied: Agents cannot create new templates.";
                return null;
            }

            try
            {
                using var db = LocalDb.CreateContext(tenantId);
                var source = db.EmailTemplates.FirstOrDefault(t => t.TemplateId == sourceTemplateId && t.TenantId == tenantId && !t.IsDeleted);
                if (source == null)
                {
                    errorMessage = "Source template not found.";
                    return null;
                }

                var clone = new EmailTemplate
                {
                    TenantId = tenantId,
                    Name = string.IsNullOrWhiteSpace(newName) ? $"{source.Name} (Copy)" : newName.Trim(),
                    Category = source.Category,
                    TargetAudience = source.TargetAudience,
                    EmailFormat = source.EmailFormat,
                    Subject = source.Subject,
                    Body = source.Body,
                    CallToActionText = source.CallToActionText,
                    CallToActionUrl = source.CallToActionUrl,
                    IsSystem = false,
                    IsActive = true,
                    CreatedByRole = RbacService.IsAdmin ? "Admin" : "Manager",
                    CreatedByUserId = CurrentSession.UserId > 0 ? CurrentSession.UserId : null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                db.EmailTemplates.Add(clone);
                db.SaveChanges();
                return clone;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error cloning template: {ex.Message}";
                return null;
            }
        }
    }
}
