using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Models;
using CRMS_Peguit.winforms.Models.Services;
using Microsoft.EntityFrameworkCore;

namespace CRMS_Peguit.winforms.Services
{
    public sealed class MarketUpdateBackgroundService : IDisposable
    {
        private static readonly Lazy<MarketUpdateBackgroundService> _instance =
            new(() => new MarketUpdateBackgroundService());

        public static MarketUpdateBackgroundService Instance => _instance.Value;

        private System.Threading.Timer? _timer;
        private bool _isProcessing = false;
        private readonly object _lock = new();

        public event Action<string>? LogMessageReceived;
        public event Action<BatchExecutionResult>? BatchCompleted;

        private MarketUpdateBackgroundService()
        {
        }

        public void Start(int intervalMinutes = 60)
        {
            lock (_lock)
            {
                if (_timer != null) return;

                _timer = new System.Threading.Timer(
                    async _ => await ExecuteScheduledCheckAsync(),
                    null,
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromMinutes(intervalMinutes));

                Log("Automated Market Update background service started.");
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _timer?.Change(Timeout.Infinite, Timeout.Infinite);
                _timer?.Dispose();
                _timer = null;
                Log("Automated Market Update background service stopped.");
            }
        }

        private async Task ExecuteScheduledCheckAsync()
        {
            if (_isProcessing) return;

            try
            {
                _isProcessing = true;
                await RunBatchAsync(tenantId: 1, forceRunAll: false, triggerType: "Scheduler");
            }
            catch (Exception ex)
            {
                Log($"Error in scheduled market update: {ex.Message}");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        public AutomatedEmailSettings GetSettings(int tenantId = 1)
        {
            using var db = LocalDb.CreateContext(tenantId);
            var settings = db.AutomatedEmailSettings.FirstOrDefault(s => s.TenantId == tenantId);
            if (settings == null)
            {
                settings = new AutomatedEmailSettings
                {
                    TenantId = tenantId,
                    IsEnabled = false,
                    FrequencyDays = 180,
                    AnnualAppreciationRatePercent = 5.0m,
                    EmailFormat = "Html",
                    TargetAudience = "All",
                    BrokerageName = "NEXA Real Estate Advisory",
                    CallToActionText = "Schedule a Complimentary Equity Consultation",
                    CallToActionUrl = "https://nexacrm.local/cma-request",
                    SubjectTemplate = "Market Valuation & Equity Report for {PropertyAddress}",
                    UpdatedAt = DateTime.UtcNow
                };
                db.AutomatedEmailSettings.Add(settings);
                db.SaveChanges();
            }

            return settings;
        }

        public bool SaveSettings(AutomatedEmailSettings updatedSettings, int tenantId = 1)
        {
            try
            {
                using var db = LocalDb.CreateContext(tenantId);
                var existing = db.AutomatedEmailSettings.FirstOrDefault(s => s.TenantId == tenantId);
                if (existing == null)
                {
                    updatedSettings.TenantId = tenantId;
                    updatedSettings.UpdatedAt = DateTime.UtcNow;
                    db.AutomatedEmailSettings.Add(updatedSettings);
                }
                else
                {
                    existing.IsEnabled = updatedSettings.IsEnabled;
                    existing.ActiveTemplateId = updatedSettings.ActiveTemplateId;
                    existing.FrequencyDays = Math.Max(1, updatedSettings.FrequencyDays);
                    existing.AnnualAppreciationRatePercent = updatedSettings.AnnualAppreciationRatePercent;
                    existing.EmailFormat = string.IsNullOrWhiteSpace(updatedSettings.EmailFormat) ? "Html" : updatedSettings.EmailFormat;
                    existing.TargetAudience = string.IsNullOrWhiteSpace(updatedSettings.TargetAudience) ? "All" : updatedSettings.TargetAudience;
                    existing.BrokerageName = string.IsNullOrWhiteSpace(updatedSettings.BrokerageName) ? "NEXA Real Estate Advisory" : updatedSettings.BrokerageName.Trim();
                    existing.CallToActionText = string.IsNullOrWhiteSpace(updatedSettings.CallToActionText) ? "Schedule Valuation Review" : updatedSettings.CallToActionText.Trim();
                    existing.CallToActionUrl = string.IsNullOrWhiteSpace(updatedSettings.CallToActionUrl) ? "https://nexacrm.local" : updatedSettings.CallToActionUrl.Trim();
                    existing.SubjectTemplate = updatedSettings.SubjectTemplate?.Trim() ?? "Market Valuation Update";
                    existing.BodyTemplate = updatedSettings.BodyTemplate?.Trim() ?? string.Empty;
                    existing.UpdatedAt = DateTime.UtcNow;
                }

                db.SaveChanges();
                Log("Automated email settings successfully saved.");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Failed to save settings: {ex.Message}");
                return false;
            }
        }

        public List<ClientPickerItem> GetEligibleClients(int tenantId = 1, int? scopedAgentId = null)
        {
            using var db = LocalDb.CreateContext(tenantId);
            var settings = GetSettings(tenantId);

            int? filterAgentId = scopedAgentId ?? (RbacService.IsAgent ? CurrentSession.UserId : null);

            var query = db.Deals
                .AsNoTracking()
                .Include(d => d.Customer)
                .Include(d => d.Property)
                .Where(d => d.Customer != null && !d.Customer.IsDeleted)
                .Where(d => d.Stage.ToLower() == "closed" || d.Stage.ToLower() == "closed-won" || d.Stage.ToLower() == "won");

            if (filterAgentId.HasValue && filterAgentId.Value > 0)
            {
                query = query.Where(d => d.AgentId == filterAgentId.Value);
            }

            if (settings.TargetAudience.Equals("Buyers", StringComparison.OrdinalIgnoreCase))
                query = query.Where(d => d.Customer!.Type.ToLower() == "buyer");
            else if (settings.TargetAudience.Equals("Sellers", StringComparison.OrdinalIgnoreCase))
                query = query.Where(d => d.Customer!.Type.ToLower() == "seller");

            var deals = query
                .OrderByDescending(d => d.ContractSignedDate ?? d.CreatedAt)
                .ToList();

            var uniqueByCustomer = deals
                .GroupBy(d => d.CustomerId)
                .Select(g => g.First())
                .Select(d => new ClientPickerItem
                {
                    CustomerId = d.CustomerId,
                    FullName = d.Customer?.FullName ?? "Unknown",
                    Email = d.Customer?.Email ?? "No Email",
                    PropertyAddress = d.Property?.Address ?? "Property Record",
                    OriginalPrice = d.Value > 0 ? d.Value : (d.Property?.Price ?? 0m),
                    LastSentAt = d.Customer?.LastMarketUpdateSentAt
                })
                .OrderBy(x => x.FullName)
                .ToList();

            return uniqueByCustomer;
        }

        public MarketUpdateKpis GetAnalyticsKpis(int tenantId = 1, int? scopedAgentId = null)
        {
            using var db = LocalDb.CreateContext(tenantId);
            var settings = GetSettings(tenantId);

            int? filterAgentId = scopedAgentId ?? (RbacService.IsAgent ? CurrentSession.UserId : null);

            var clients = GetEligibleClients(tenantId, filterAgentId);
            int enrolled = clients.Count;

            int delivered;
            if (filterAgentId.HasValue && filterAgentId.Value > 0)
            {
                var clientIds = clients.Select(c => c.CustomerId).ToHashSet();
                delivered = db.MarketUpdateLogs.Count(l => l.Status == "Sent" && l.CustomerId.HasValue && clientIds.Contains(l.CustomerId.Value));
            }
            else
            {
                delivered = db.MarketUpdateLogs.Count(l => l.Status == "Sent");
            }

            decimal totalGain = 0m;
            int countWithGains = 0;

            foreach (var c in clients)
            {
                if (c.OriginalPrice > 0)
                {
                    decimal gain = c.OriginalPrice * ((settings.AnnualAppreciationRatePercent / 100m) * 1.5m);
                    totalGain += gain;
                    countWithGains++;
                }
            }

            decimal avgGain = countWithGains > 0 ? Math.Round(totalGain / countWithGains, 0) : 0m;

            return new MarketUpdateKpis
            {
                EnrolledClientsCount = enrolled,
                LifetimeDeliveredCount = delivered,
                AvgClientEquityGain = avgGain,
                IsActive = settings.IsEnabled,
                FrequencyDays = settings.FrequencyDays
            };
        }

        public List<MarketUpdateLog> GetDeliveryHistory(int tenantId = 1, int maxCount = 50, int? scopedAgentId = null)
        {
            using var db = LocalDb.CreateContext(tenantId);
            int? filterAgentId = scopedAgentId ?? (RbacService.IsAgent ? CurrentSession.UserId : null);

            var query = db.MarketUpdateLogs.AsNoTracking();

            if (filterAgentId.HasValue && filterAgentId.Value > 0)
            {
                var clientIds = GetEligibleClients(tenantId, filterAgentId).Select(c => c.CustomerId).ToHashSet();
                query = query.Where(l => l.CustomerId.HasValue && clientIds.Contains(l.CustomerId.Value));
            }

            return query
                .OrderByDescending(l => l.SentAt)
                .Take(maxCount)
                .ToList();
        }

        public (string Subject, string Body, string Recipient, ValuationMetrics Metrics) GeneratePreview(int tenantId = 1, int? customerId = null, int? scopedAgentId = null)
        {
            var settings = GetSettings(tenantId);

            using var db = LocalDb.CreateContext(tenantId);
            var query = db.Deals
                .Include(d => d.Customer)
                .Include(d => d.Property)
                .Include(d => d.Agent)
                .Where(d => d.Customer != null && !d.Customer.IsDeleted)
                .Where(d => d.Stage.ToLower() == "closed" || d.Stage.ToLower() == "closed-won" || d.Stage.ToLower() == "won");

            int? filterAgentId = scopedAgentId ?? (RbacService.IsAgent ? CurrentSession.UserId : null);
            if (filterAgentId.HasValue && filterAgentId.Value > 0)
            {
                query = query.Where(d => d.AgentId == filterAgentId.Value);
            }

            if (customerId.HasValue && customerId.Value > 0)
            {
                query = query.Where(d => d.CustomerId == customerId.Value);
            }

            var deal = query
                .OrderByDescending(d => d.ContractSignedDate ?? d.CreatedAt)
                .FirstOrDefault();

            if (deal != null && deal.Customer != null)
            {
                var metrics = CalculateValuation(settings, deal);
                var (subject, body) = settings.EmailFormat == "Html"
                    ? FormatHtmlMessage(settings, deal, metrics)
                    : FormatPlainTextMessage(settings, deal, metrics);

                string recipient = deal.Customer.Email ?? "client@example.com";
                return (subject, body, recipient, metrics);
            }

            // Fallback sample data
            string fallbackAgent = (RbacService.IsAgent ? CurrentSession.CurrentUser?.FullName : null) ?? "NEXA Real Estate Advisor";
            var fallbackMetrics = new ValuationMetrics
            {
                CustomerName = "Maria Santos",
                FirstName = "Maria",
                PropertyAddress = "Unit 1204, One Serendra, BGC, Taguig",
                PropertyType = "Condominium",
                OriginalPrice = 18500000m,
                EstimatedValue = 20396250m,
                EquityGain = 1896250m,
                EquityGainPercent = 10.25m,
                YearsOwned = 2.0,
                AnnualRatePercent = settings.AnnualAppreciationRatePercent,
                AgentName = fallbackAgent
            };

            var (fSubj, fBody) = settings.EmailFormat == "Html"
                ? FormatHtmlMessage(settings, null, fallbackMetrics)
                : FormatPlainTextMessage(settings, null, fallbackMetrics);

            return (fSubj, fBody, "maria.santos@example.ph", fallbackMetrics);
        }

        public static string FormatVisualCardMockup(AutomatedEmailSettings settings, ValuationMetrics metrics, string recipient)
        {
            string origStr = $"₱{metrics.OriginalPrice:N0}";
            string estStr = $"₱{metrics.EstimatedValue:N0}";
            string gainStr = $"₱{metrics.EquityGain:N0}";
            string rateStr = metrics.AnnualRatePercent.ToString("F1");
            string yearsStr = metrics.YearsOwned.ToString("F1");
            string gainPercentStr = metrics.EquityGainPercent.ToString("F1");

            string subject = ReplaceTokens(settings.SubjectTemplate, metrics.CustomerName, metrics.FirstName,
                metrics.PropertyAddress, metrics.PropertyType, origStr, estStr, gainStr, gainPercentStr, rateStr, yearsStr, metrics.AgentName);

            string body = ReplaceTokens(settings.BodyTemplate, metrics.CustomerName, metrics.FirstName,
                metrics.PropertyAddress, metrics.PropertyType, origStr, estStr, gainStr, gainPercentStr, rateStr, yearsStr, metrics.AgentName);

            string formatStr = settings.EmailFormat == "Html" ? "Branded Client Report (Visual)" : "1-on-1 Personal Note (Plain Text)";

            return
                $"{settings.BrokerageName.ToUpperInvariant()} · ASSET REPORT\r\n\r\n" +
                $"TO : {recipient}\r\n" +
                $"SUBJECT : {subject}\r\n" +
                $"FORMAT : {formatStr}\r\n\r\n" +
                $"Dear {metrics.FirstName},\r\n\r\n" +
                $"As part of our continuous advisory service at {settings.BrokerageName},\r\n\r\n" +
                $"{body}\r\n\r\n" +
                $"ACTION BUTTON : [{settings.CallToActionText}]\r\n\r\n" +
                $"ADVISOR SIGNATURE :\r\n" +
                $"{metrics.AgentName}\r\n" +
                $"Licensed Real Estate Advisory Team\r\n" +
                $"{settings.BrokerageName}";
        }

        public static string SavePreviewHtmlToFile(AutomatedEmailSettings settings, ValuationMetrics metrics)
        {
            var (_, html) = FormatHtmlMessage(settings, null, metrics);
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nexa_market_valuation_preview.html");
            System.IO.File.WriteAllText(path, html);
            return path;
        }

        public async Task<EmailSendResult> SendTestEmailAsync(string testEmail, int tenantId = 1, int? sampleCustomerId = null)
        {
            if (!ContactEmailService.IsValidEmail(testEmail))
            {
                return EmailSendResult.Failed("Please enter a valid email address.");
            }

            var settings = GetSettings(tenantId);
            var (subject, body, _, metrics) = GeneratePreview(tenantId, sampleCustomerId);
            string testSubject = $"[TEST DISPATCH] {subject}";

            Log($"Sending test personalized market update ({settings.EmailFormat}) to {testEmail}...");
            var result = await ContactEmailService.SendAsync(testEmail, testSubject, body, isBodyHtml: settings.EmailFormat == "Html");

            // Audit record
            try
            {
                using var db = LocalDb.CreateContext(tenantId);
                db.MarketUpdateLogs.Add(new MarketUpdateLog
                {
                    TenantId = tenantId,
                    CustomerId = sampleCustomerId,
                    CustomerName = $"{metrics.CustomerName} (Test Mode)",
                    RecipientEmail = testEmail,
                    PropertyAddress = metrics.PropertyAddress,
                    PropertyType = metrics.PropertyType,
                    OriginalPrice = metrics.OriginalPrice,
                    EstimatedValue = metrics.EstimatedValue,
                    EquityGainAmount = metrics.EquityGain,
                    EquityGainPercent = metrics.EquityGainPercent,
                    EmailFormat = settings.EmailFormat,
                    Status = result.Success ? "Sent" : "Failed",
                    ErrorMessage = result.Success ? null : result.Message,
                    TriggerType = "TestDispatch",
                    SentAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log($"Could not log test dispatch: {ex.Message}");
            }

            Log(result.Success ? $"Test email dispatched successfully to {testEmail}." : $"Test email failed: {result.Message}");
            return result;
        }

        public async Task<BatchExecutionResult> RunBatchAsync(int tenantId = 1, bool forceRunAll = false, string triggerType = "ManualBatch", int? scopedAgentId = null)
        {
            var result = new BatchExecutionResult();
            var settings = GetSettings(tenantId);

            if (!settings.IsEnabled && !forceRunAll)
            {
                result.Summary = "Automation is currently paused. Batch skipped.";
                Log(result.Summary);
                return result;
            }

            using var db = LocalDb.CreateContext(tenantId);

            var query = db.Deals
                .AsNoTracking()
                .Include(d => d.Customer)
                .Include(d => d.Property)
                .Include(d => d.Agent)
                .Where(d => d.Customer != null && !d.Customer.IsDeleted)
                .Where(d => d.Stage.ToLower() == "closed" || d.Stage.ToLower() == "closed-won" || d.Stage.ToLower() == "won");

            int? filterAgentId = scopedAgentId ?? (triggerType != "Scheduler" && RbacService.IsAgent ? CurrentSession.UserId : null);
            if (filterAgentId.HasValue && filterAgentId.Value > 0)
            {
                query = query.Where(d => d.AgentId == filterAgentId.Value);
            }

            if (settings.TargetAudience.Equals("Buyers", StringComparison.OrdinalIgnoreCase))
                query = query.Where(d => d.Customer!.Type.ToLower() == "buyer");
            else if (settings.TargetAudience.Equals("Sellers", StringComparison.OrdinalIgnoreCase))
                query = query.Where(d => d.Customer!.Type.ToLower() == "seller");

            var deals = await query
                .OrderByDescending(d => d.ContractSignedDate ?? d.CreatedAt)
                .ToListAsync();

            var latestDealsByCustomer = deals
                .GroupBy(d => d.CustomerId)
                .Select(g => g.First())
                .ToList();

            result.TotalEvaluated = latestDealsByCustomer.Count;
            DateTime now = DateTime.UtcNow;

            var customersToUpdate = new List<int>();
            var logsToAdd = new List<MarketUpdateLog>();

            foreach (var deal in latestDealsByCustomer)
            {
                var customer = deal.Customer;
                if (customer == null) continue;

                string email = customer.Email?.Trim() ?? string.Empty;
                var metrics = CalculateValuation(settings, deal);

                if (!ContactEmailService.IsValidEmail(email))
                {
                    result.SkippedCount++;
                    string reason = "Missing or invalid email address";
                    result.Details.Add($"Skipped '{customer.FullName}': {reason}.");

                    logsToAdd.Add(new MarketUpdateLog
                    {
                        TenantId = tenantId,
                        CustomerId = customer.CustomerId,
                        CustomerName = customer.FullName,
                        RecipientEmail = string.IsNullOrEmpty(email) ? "None" : email,
                        PropertyAddress = metrics.PropertyAddress,
                        PropertyType = metrics.PropertyType,
                        OriginalPrice = metrics.OriginalPrice,
                        EstimatedValue = metrics.EstimatedValue,
                        EquityGainAmount = metrics.EquityGain,
                        EquityGainPercent = metrics.EquityGainPercent,
                        EmailFormat = settings.EmailFormat,
                        Status = "Skipped",
                        ErrorMessage = reason,
                        TriggerType = triggerType,
                        SentAt = now
                    });
                    continue;
                }

                if (!forceRunAll && customer.LastMarketUpdateSentAt.HasValue)
                {
                    double daysSinceLast = (now - customer.LastMarketUpdateSentAt.Value).TotalDays;
                    if (daysSinceLast < settings.FrequencyDays)
                    {
                        result.SkippedCount++;
                        string reason = $"Delivered {daysSinceLast:F0} days ago (Interval is {settings.FrequencyDays} days)";
                        result.Details.Add($"Skipped '{customer.FullName}': {reason}.");
                        continue;
                    }
                }

                var (subject, body) = settings.EmailFormat == "Html"
                    ? FormatHtmlMessage(settings, deal, metrics)
                    : FormatPlainTextMessage(settings, deal, metrics);

                try
                {
                    var sendResult = await ContactEmailService.SendAsync(email, subject, body, isBodyHtml: settings.EmailFormat == "Html");
                    if (sendResult.Success)
                    {
                        result.SentCount++;
                        customersToUpdate.Add(customer.CustomerId);
                        result.Details.Add($"✓ Sent to {customer.FullName} ({email}) - {metrics.PropertyAddress}");

                        logsToAdd.Add(new MarketUpdateLog
                        {
                            TenantId = tenantId,
                            CustomerId = customer.CustomerId,
                            CustomerName = customer.FullName,
                            RecipientEmail = email,
                            PropertyAddress = metrics.PropertyAddress,
                            PropertyType = metrics.PropertyType,
                            OriginalPrice = metrics.OriginalPrice,
                            EstimatedValue = metrics.EstimatedValue,
                            EquityGainAmount = metrics.EquityGain,
                            EquityGainPercent = metrics.EquityGainPercent,
                            EmailFormat = settings.EmailFormat,
                            Status = "Sent",
                            TriggerType = triggerType,
                            SentAt = now
                        });
                    }
                    else
                    {
                        result.FailedCount++;
                        result.Details.Add($"✗ Failed {customer.FullName} ({email}): {sendResult.Message}");

                        logsToAdd.Add(new MarketUpdateLog
                        {
                            TenantId = tenantId,
                            CustomerId = customer.CustomerId,
                            CustomerName = customer.FullName,
                            RecipientEmail = email,
                            PropertyAddress = metrics.PropertyAddress,
                            PropertyType = metrics.PropertyType,
                            OriginalPrice = metrics.OriginalPrice,
                            EstimatedValue = metrics.EstimatedValue,
                            EquityGainAmount = metrics.EquityGain,
                            EquityGainPercent = metrics.EquityGainPercent,
                            EmailFormat = settings.EmailFormat,
                            Status = "Failed",
                            ErrorMessage = sendResult.Message,
                            TriggerType = triggerType,
                            SentAt = now
                        });
                    }
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.Details.Add($"✗ Exception {customer.FullName} ({email}): {ex.Message}");
                }
            }

            // Persist customer updates & logs
            try
            {
                using var updateDb = LocalDb.CreateContext(tenantId);

                if (customersToUpdate.Count > 0)
                {
                    var custs = await updateDb.Customers
                        .Where(c => customersToUpdate.Contains(c.CustomerId))
                        .ToListAsync();

                    foreach (var c in custs)
                        c.LastMarketUpdateSentAt = now;
                }

                if (logsToAdd.Count > 0)
                {
                    updateDb.MarketUpdateLogs.AddRange(logsToAdd);
                }

                var dbSettings = await updateDb.AutomatedEmailSettings.FirstOrDefaultAsync(s => s.TenantId == tenantId);
                if (dbSettings != null)
                {
                    dbSettings.LastBatchRunAt = now;
                    dbSettings.LastBatchStatus = $"Sent {result.SentCount}, Skipped {result.SkippedCount}, Failed {result.FailedCount}";
                }

                await updateDb.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log($"Error saving batch audit logs: {ex.Message}");
            }

            result.Summary = $"Batch complete: {result.SentCount} sent, {result.SkippedCount} skipped, {result.FailedCount} failed.";
            Log(result.Summary);
            BatchCompleted?.Invoke(result);

            return result;
        }

        public static ValuationMetrics CalculateValuation(AutomatedEmailSettings settings, Deal deal)
        {
            var customer = deal.Customer!;
            string customerName = customer.FullName;
            string firstName = customer.FirstName;
            if (string.IsNullOrWhiteSpace(firstName))
                firstName = customerName.Split(' ').FirstOrDefault() ?? "Client";

            string propAddress = deal.Property?.Address ?? "your property";
            string propType = deal.Property?.PropertyType ?? "Real Estate";
            if (!string.IsNullOrWhiteSpace(propType))
                propType = char.ToUpper(propType[0]) + (propType.Length > 1 ? propType[1..] : "");

            decimal originalPrice = deal.Value > 0 ? deal.Value : (deal.Property?.Price ?? 0m);

            DateTime dealDate = deal.ContractSignedDate ?? deal.CreatedAt;
            double yearsOwned = Math.Max(0.5, (DateTime.UtcNow - dealDate).TotalDays / 365.25);

            double rate = (double)settings.AnnualAppreciationRatePercent / 100.0;
            decimal estimatedValue = originalPrice * (decimal)Math.Pow(1.0 + rate, yearsOwned);
            estimatedValue = Math.Round(estimatedValue / 10000m) * 10000m;

            decimal equityGain = Math.Max(0m, estimatedValue - originalPrice);
            decimal equityPercent = originalPrice > 0 ? Math.Round((equityGain / originalPrice) * 100m, 1) : 0m;

            string agentName = deal.Agent?.FullName
                ?? (RbacService.IsAgent ? CurrentSession.CurrentUser?.FullName : null)
                ?? "NEXA Real Estate Advisory Team";

            return new ValuationMetrics
            {
                CustomerName = customerName,
                FirstName = firstName,
                PropertyAddress = propAddress,
                PropertyType = propType,
                OriginalPrice = originalPrice,
                EstimatedValue = estimatedValue,
                EquityGain = equityGain,
                EquityGainPercent = equityPercent,
                YearsOwned = Math.Round(yearsOwned, 1),
                AnnualRatePercent = settings.AnnualAppreciationRatePercent,
                AgentName = agentName
            };
        }

        public static (string Subject, string Body) FormatPlainTextMessage(AutomatedEmailSettings settings, Deal? deal, ValuationMetrics metrics)
        {
            string origStr = $"₱{metrics.OriginalPrice:N2}";
            string estStr = $"₱{metrics.EstimatedValue:N2}";
            string gainStr = $"₱{metrics.EquityGain:N2}";
            string rateStr = metrics.AnnualRatePercent.ToString("F1");
            string yearsStr = metrics.YearsOwned.ToString("F1");
            string gainPercentStr = metrics.EquityGainPercent.ToString("F1");

            string subject = ReplaceTokens(settings.SubjectTemplate, metrics.CustomerName, metrics.FirstName,
                metrics.PropertyAddress, metrics.PropertyType, origStr, estStr, gainStr, gainPercentStr, rateStr, yearsStr, metrics.AgentName);

            string body = ReplaceTokens(settings.BodyTemplate, metrics.CustomerName, metrics.FirstName,
                metrics.PropertyAddress, metrics.PropertyType, origStr, estStr, gainStr, gainPercentStr, rateStr, yearsStr, metrics.AgentName);

            return (subject, body);
        }

        public static (string Subject, string Body) FormatHtmlMessage(AutomatedEmailSettings settings, Deal? deal, ValuationMetrics metrics)
        {
            string origStr = $"₱{metrics.OriginalPrice:N2}";
            string estStr = $"₱{metrics.EstimatedValue:N2}";
            string gainStr = $"₱{metrics.EquityGain:N2}";
            string rateStr = metrics.AnnualRatePercent.ToString("F1");
            string yearsStr = metrics.YearsOwned.ToString("F1");
            string gainPercentStr = metrics.EquityGainPercent.ToString("F1");

            string subject = ReplaceTokens(settings.SubjectTemplate, metrics.CustomerName, metrics.FirstName,
                metrics.PropertyAddress, metrics.PropertyType, origStr, estStr, gainStr, gainPercentStr, rateStr, yearsStr, metrics.AgentName);

            string html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{WebUtility.HtmlEncode(subject)}</title>
</head>
<body style=""margin: 0; padding: 24px 0; background-color: #F1F5F9; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; -webkit-font-smoothing: antialiased;"">
    <table align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; border: 1px solid #E2E8F0; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);"">
        <!-- Top Branded Header -->
        <tr>
            <td style=""background-color: #0F172A; padding: 28px 32px; border-bottom: 4px solid #0284C7;"">
                <table width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"">
                    <tr>
                        <td>
                            <span style=""font-size: 20px; font-weight: 800; color: #ffffff; letter-spacing: 0.5px;"">{WebUtility.HtmlEncode(settings.BrokerageName)}</span>
                            <div style=""font-size: 11px; color: #94A3B8; text-transform: uppercase; letter-spacing: 1px; margin-top: 4px;"">Client Retention & Private Wealth Advisory</div>
                        </td>
                        <td align=""right"">
                            <span style=""background-color: #1E293B; color: #38BDF8; font-size: 11px; font-weight: 700; padding: 6px 12px; border-radius: 20px; border: 1px solid #334155;"">CONFIDENTIAL VALUATION</span>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>

        <!-- Main Body Content -->
        <tr>
            <td style=""padding: 32px 32px 24px 32px;"">
                <h1 style=""font-size: 22px; font-weight: 700; color: #0F172A; margin: 0 0 14px 0; line-height: 1.3;"">
                    Your Personalized Market Valuation Update
                </h1>
                <p style=""font-size: 15px; color: #334155; line-height: 1.6; margin: 0 0 24px 0;"">
                    Dear <strong>{WebUtility.HtmlEncode(metrics.CustomerName)}</strong>,<br><br>
                    As part of our commitment to safeguarding your real estate investments, our advisory team monitors local transactions and neighborhood appreciation trends. Below is your updated asset valuation report:
                </p>

                <!-- Equity Gain Highlight Card -->
                <table width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""background: linear-gradient(135deg, #F0FDF4 0%, #ECFDF5 100%); border: 1.5px solid #86EFAC; border-radius: 10px; margin-bottom: 24px; padding: 20px;"">
                    <tr>
                        <td>
                            <div style=""font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.8px; color: #166534; margin-bottom: 6px;"">
                                EST. EQUITY GROWTH SINCE ACQUISITION
                            </div>
                            <div style=""font-size: 30px; font-weight: 800; color: #15803D; margin-bottom: 4px;"">
                                +{gainStr} <span style=""font-size: 18px; font-weight: 700;"">(+{gainPercentStr}%)</span>
                            </div>
                            <div style=""font-size: 13px; color: #166534;"">
                                Calculated Current Market Value: <strong style=""color: #0F172A; font-size: 15px;"">{estStr}</strong>
                            </div>
                        </td>
                    </tr>
                </table>

                <!-- Property Details Breakdown -->
                <table width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #F8FAFC; border-radius: 8px; border: 1px solid #E2E8F0; margin-bottom: 28px;"">
                    <tr>
                        <td style=""padding: 14px 18px; border-bottom: 1px solid #E2E8F0; font-size: 13px; color: #64748B; width: 40%;"">Property Address</td>
                        <td style=""padding: 14px 18px; border-bottom: 1px solid #E2E8F0; font-size: 14px; font-weight: 600; color: #0F172A;"">{WebUtility.HtmlEncode(metrics.PropertyAddress)}</td>
                    </tr>
                    <tr>
                        <td style=""padding: 14px 18px; border-bottom: 1px solid #E2E8F0; font-size: 13px; color: #64748B;"">Asset Category</td>
                        <td style=""padding: 14px 18px; border-bottom: 1px solid #E2E8F0; font-size: 14px; font-weight: 600; color: #0F172A;"">{WebUtility.HtmlEncode(metrics.PropertyType)}</td>
                    </tr>
                    <tr>
                        <td style=""padding: 14px 18px; border-bottom: 1px solid #E2E8F0; font-size: 13px; color: #64748B;"">Original Acquisition Price</td>
                        <td style=""padding: 14px 18px; border-bottom: 1px solid #E2E8F0; font-size: 14px; font-weight: 600; color: #0F172A;"">{origStr}</td>
                    </tr>
                    <tr>
                        <td style=""padding: 14px 18px; font-size: 13px; color: #64748B;"">Holding Period & Annual Rate</td>
                        <td style=""padding: 14px 18px; font-size: 14px; font-weight: 600; color: #0F172A;"">{yearsStr} yrs at +{rateStr}% / yr</td>
                    </tr>
                </table>

                <!-- Call to Action Button -->
                <table width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom: 32px;"">
                    <tr>
                        <td align=""center"">
                            <a href=""{WebUtility.HtmlEncode(settings.CallToActionUrl)}"" target=""_blank"" style=""display: inline-block; background-color: #0F5B9E; color: #ffffff; text-decoration: none; font-size: 14px; font-weight: 700; padding: 14px 28px; border-radius: 8px; box-shadow: 0 2px 4px rgba(15, 91, 158, 0.2);"">
                                {WebUtility.HtmlEncode(settings.CallToActionText)} &rarr;
                            </a>
                        </td>
                    </tr>
                </table>

                <!-- Advisor Signature Card -->
                <table width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""border-top: 1px solid #E2E8F0; padding-top: 20px;"">
                    <tr>
                        <td>
                            <div style=""font-size: 14px; font-weight: 700; color: #0F172A;"">{WebUtility.HtmlEncode(metrics.AgentName)}</div>
                            <div style=""font-size: 12px; color: #64748B; margin-top: 2px;"">Licensed Real Estate Specialist & Advisor</div>
                            <div style=""font-size: 12px; color: #0284C7; font-weight: 600; margin-top: 4px;"">{WebUtility.HtmlEncode(settings.BrokerageName)}</div>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>

        <!-- Footer / Compliance Disclaimers -->
        <tr>
            <td style=""background-color: #F8FAFC; padding: 20px 32px; border-top: 1px solid #E2E8F0; font-size: 11px; color: #94A3B8; line-height: 1.5;"">
                This automated valuation report is prepared as an informational market overview based on historical transaction growth rates and does not constitute a certified appraisal. If your property is currently listed with another broker, this is not intended as a solicitation.<br><br>
                &copy; {DateTime.UtcNow.Year} {WebUtility.HtmlEncode(settings.BrokerageName)}. All rights reserved.
            </td>
        </tr>
    </table>
</body>
</html>";

            return (subject, html);
        }

        public static string ReplaceTokens(
            string template,
            string customerName,
            string firstName,
            string propertyAddress,
            string propertyType,
            string originalPrice,
            string estimatedValue,
            string equityGain,
            string equityPercent,
            string appreciationRate,
            string yearsOwned,
            string agentName)
        {
            if (string.IsNullOrEmpty(template)) return string.Empty;

            return template
                .Replace("{CustomerName}", customerName, StringComparison.OrdinalIgnoreCase)
                .Replace("{FirstName}", firstName, StringComparison.OrdinalIgnoreCase)
                .Replace("{PropertyAddress}", propertyAddress, StringComparison.OrdinalIgnoreCase)
                .Replace("{PropertyType}", propertyType, StringComparison.OrdinalIgnoreCase)
                .Replace("{OriginalPrice}", originalPrice, StringComparison.OrdinalIgnoreCase)
                .Replace("{EstimatedValue}", estimatedValue, StringComparison.OrdinalIgnoreCase)
                .Replace("{EquityGain}", equityGain, StringComparison.OrdinalIgnoreCase)
                .Replace("{EquityPercent}", equityPercent, StringComparison.OrdinalIgnoreCase)
                .Replace("{AppreciationRate}", appreciationRate, StringComparison.OrdinalIgnoreCase)
                .Replace("{YearsOwned}", yearsOwned, StringComparison.OrdinalIgnoreCase)
                .Replace("{AgentName}", agentName, StringComparison.OrdinalIgnoreCase);
        }

        private void Log(string message)
        {
            Debug.WriteLine($"[MarketUpdateService] {message}");
            LogMessageReceived?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        public void Dispose()
        {
            Stop();
        }
    }

    public sealed class ClientPickerItem
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PropertyAddress { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public DateTime? LastSentAt { get; set; }

        public override string ToString() => $"{FullName} — {PropertyAddress}";
    }

    public sealed class ValuationMetrics
    {
        public string CustomerName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string PropertyAddress { get; set; } = string.Empty;
        public string PropertyType { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public decimal EstimatedValue { get; set; }
        public decimal EquityGain { get; set; }
        public decimal EquityGainPercent { get; set; }
        public double YearsOwned { get; set; }
        public decimal AnnualRatePercent { get; set; }
        public string AgentName { get; set; } = string.Empty;
    }

    public sealed class MarketUpdateKpis
    {
        public int EnrolledClientsCount { get; set; }
        public int LifetimeDeliveredCount { get; set; }
        public decimal AvgClientEquityGain { get; set; }
        public bool IsActive { get; set; }
        public int FrequencyDays { get; set; }
    }

    public sealed class BatchExecutionResult
    {
        public int TotalEvaluated { get; set; }
        public int SentCount { get; set; }
        public int SkippedCount { get; set; }
        public int FailedCount { get; set; }
        public string Summary { get; set; } = string.Empty;
        public List<string> Details { get; } = new();
    }
}
