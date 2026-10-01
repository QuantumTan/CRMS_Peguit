using System.Net;
using System.Net.Mail;
using System.Text.Json;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.winforms.Models.Services
{
    public static class ContactEmailService
    {
        private const string SmtpHost = "smtp.gmail.com";
        private const int SmtpPort = 587;

        public static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                var address = new MailAddress(email.Trim());
                return string.Equals(address.Address, email.Trim(), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public static async Task<EmailSendResult> SendAsync(
            string recipientEmail,
            string subject,
            string body,
            bool isBodyHtml = false)
        {
            var validation = ValidateMessage(recipientEmail, subject, body);
            if (!validation.Success)
            {
                return validation;
            }

            var config = SmtpSettings.Load();
            if (!config.IsConfigured)
            {
                return EmailSendResult.Failed(
                    "Google SMTP is not configured. Add smtp.local.json beside the WinForms project or set CRMS_SMTP_USERNAME and CRMS_SMTP_APP_PASSWORD, then restart the app.");
            }

            try
            {
                using var mail = new MailMessage
                {
                    From = new MailAddress(config.Username!, BrandingService.GetDisplayName()),
                    Subject = subject.Trim(),
                    Body = body.Trim(),
                    IsBodyHtml = isBodyHtml
                };
                mail.To.Add(recipientEmail.Trim());

                using var client = new SmtpClient(SmtpHost, SmtpPort)
                {
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(
                        config.Username,
                        config.AppPassword)
                };

                await client.SendMailAsync(mail);

                return EmailSendResult.Succeeded("Email sent successfully.");
            }
            catch (SmtpException ex)
            {
                return EmailSendResult.Failed($"SMTP failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                return EmailSendResult.Failed($"Email failed: {ex.Message}");
            }
        }

        public static Task<EmailSendResult> SendForgotPasswordAsync(
            string recipientEmail,
            string? companyId = null)
        {
            string brand = BrandingService.GetDisplayName();
            string subject = $"{brand} password reset request";
            string companyLine = string.IsNullOrWhiteSpace(companyId)
                ? string.Empty
                : $"Company ID: {companyId}" + Environment.NewLine + Environment.NewLine;

            string body =
                $"We received a password reset request for your {brand} account." +
                Environment.NewLine +
                Environment.NewLine +
                companyLine +
                "The secure reset-link workflow is still pending backend integration. Please contact your administrator to reset your password." +
                Environment.NewLine +
                Environment.NewLine +
                "If you did not request this, you can ignore this email.";

            return SendAsync(recipientEmail, subject, body);
        }

        private static EmailSendResult ValidateMessage(
            string recipientEmail,
            string subject,
            string body)
        {
            if (!IsValidEmail(recipientEmail))
            {
                return EmailSendResult.Failed("A valid recipient email is required.");
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                return EmailSendResult.Failed("Subject is required.");
            }

            if (subject.Contains('\r') || subject.Contains('\n'))
            {
                return EmailSendResult.Failed("Email subject must not contain newline characters.");
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                return EmailSendResult.Failed("Message is required.");
            }

            return EmailSendResult.Succeeded(string.Empty);
        }
    }

    internal sealed class SmtpSettings
    {
        public string? Username { get; private init; }
        public string? AppPassword { get; private init; }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(AppPassword);

        public static SmtpSettings Load()
        {
            var localSettings = FromLocalFile();
            if (localSettings.IsConfigured)
            {
                return localSettings;
            }

            return FromEnvironment();
        }

        private static SmtpSettings FromEnvironment()
        {
            return new SmtpSettings
            {
                Username = Environment.GetEnvironmentVariable("CRMS_SMTP_USERNAME"),
                AppPassword = Environment.GetEnvironmentVariable("CRMS_SMTP_APP_PASSWORD")
            };
        }

        private static SmtpSettings FromLocalFile()
        {
            foreach (string path in GetCandidatePaths())
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    var json = File.ReadAllText(path);
                    var settings = JsonSerializer.Deserialize<SmtpSettingsFile>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (settings is null)
                    {
                        continue;
                    }

                    return new SmtpSettings
                    {
                        Username = settings.Username,
                        AppPassword = settings.AppPassword
                    };
                }
                catch
                {
                    return new SmtpSettings();
                }
            }

            return new SmtpSettings();
        }

        private static IEnumerable<string> GetCandidatePaths()
        {
            yield return Path.Combine(AppContext.BaseDirectory, "smtp.local.json");
            yield return Path.Combine(Environment.CurrentDirectory, "smtp.local.json");

            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                yield return Path.Combine(directory.FullName, "smtp.local.json");
                directory = directory.Parent;
            }
        }
    }

    internal sealed class SmtpSettingsFile
    {
        public string? Username { get; set; }
        public string? AppPassword { get; set; }
    }

    public sealed class EmailSendResult
    {
        public bool Success { get; }
        public string Message { get; }

        private EmailSendResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public static EmailSendResult Succeeded(string message) => new(true, message);
        public static EmailSendResult Failed(string message) => new(false, message);
    }
}
