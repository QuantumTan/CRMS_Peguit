using System;

namespace CRMS_Peguit.domain.entities
{
    public class AutomatedEmailSettings
    {
        public int SettingsId { get; set; }
        public int TenantId { get; set; } = 1;

        public bool IsEnabled { get; set; } = false;
        public int FrequencyDays { get; set; } = 180;
        public decimal AnnualAppreciationRatePercent { get; set; } = 5.0m;

        public int? ActiveTemplateId { get; set; }
        public string EmailFormat { get; set; } = "Html"; // "Html" or "PlainText"
        public string TargetAudience { get; set; } = "All"; // "All", "Buyers", "Sellers"
        public string BrokerageName { get; set; } = "NEXA Real Estate Advisory";
        public string CallToActionText { get; set; } = "Schedule a Complimentary Equity Consultation";
        public string CallToActionUrl { get; set; } = "https://nexacrm.local/cma-request";

        public string SubjectTemplate { get; set; } = "Market Valuation & Equity Report for {PropertyAddress}";

        public string BodyTemplate { get; set; } =
            "Dear {CustomerName},\n\n" +
            "We hope you are doing well!\n\n" +
            "As part of our continuous client care service at NEXA Real Estate Advisory, we monitor local real estate values in your area. " +
            "Based on recent historical sales and appreciation trends, here is an updated valuation estimate for your property:\n\n" +
            "• Property: {PropertyAddress} ({PropertyType})\n" +
            "• Acquisition Price: {OriginalPrice}\n" +
            "• Years Owned: {YearsOwned} year(s)\n" +
            "• Estimated Current Market Value: {EstimatedValue} (+{AppreciationRate}% est. annual growth)\n" +
            "• Estimated Equity Growth: {EquityGain} (+{EquityPercent}% total)\n\n" +
            "If you have questions regarding this valuation, wish to explore refinancing, or are considering listing in the current market, please feel free to reach out to us.\n\n" +
            "Warm regards,\n" +
            "{AgentName}\n" +
            "NEXA Real Estate Advisory Team";

        public DateTime? LastBatchRunAt { get; set; }
        public string? LastBatchStatus { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Returns an industry-standard pre-template tailored specifically for the chosen audience segment.
        /// </summary>
        public static (string Subject, string Body, string CtaText, decimal DefaultRate) GetPreTemplate(string audience)
        {
            string aud = audience?.Trim().ToLowerInvariant() ?? "all";

            if (aud.Contains("buyer"))
            {
                // ── BUYERS ONLY PRE-TEMPLATE: Homeowner Equity & Investment Performance ──
                string subject = "Homeowner Equity Update: Your property at {PropertyAddress} has appreciated!";
                string cta = "Explore Home Equity & Wealth Strategy";
                decimal rate = 5.0m;
                string body =
                    "Dear {FirstName},\n\n" +
                    "Congratulations on your continuing journey as a homeowner with NEXA Real Estate Advisory!\n\n" +
                    "It has been {YearsOwned} year(s) since you acquired your property, and we wanted to share an exciting update regarding your investment performance:\n\n" +
                    "• Property Address: {PropertyAddress} ({PropertyType})\n" +
                    "• Acquisition Price: {OriginalPrice}\n" +
                    "• Estimated Current Value: {EstimatedValue}\n" +
                    "• Net Equity Accumulated: +{EquityGain} (+{EquityPercent}% gain since purchase!)\n\n" +
                    "Your home continues to be one of your most dependable wealth-building assets. Many homeowners use this accumulated equity to fund home improvements, eliminate mortgage insurance, or leverage into a secondary income-generating rental property.\n\n" +
                    "If you would like a detailed breakdown of your equity options or have questions on the current neighborhood market, feel free to reply directly to this email or reach out anytime.\n\n" +
                    "Best regards,\n" +
                    "{AgentName}\n" +
                    "Your Dedicated Real Estate Advisor";

                return (subject, body, cta, rate);
            }
            else if (aud.Contains("seller"))
            {
                // ── SELLERS ONLY PRE-TEMPLATE: Market Timing & Listing Valuation ──
                string subject = "High Buyer Demand in Your Area: What your property at {PropertyAddress} is worth today";
                string cta = "Request Comprehensive CMA & Net Sheet";
                decimal rate = 5.5m;
                string body =
                    "Dear {CustomerName},\n\n" +
                    "Market conditions in your neighborhood are creating prime opportunities for property owners!\n\n" +
                    "Inventory levels remain tight, and qualified buyers are actively looking for properties in your area. Based on recent comparable sales, here is what your asset could command in today's active market:\n\n" +
                    "• Property: {PropertyAddress} ({PropertyType})\n" +
                    "• Historical Benchmark Price: {OriginalPrice}\n" +
                    "• Estimated Current Market Value: {EstimatedValue} (+{AppreciationRate}% annual benchmark)\n" +
                    "• Potential Capital Gain: +{EquityGain} (+{EquityPercent}% upside)\n\n" +
                    "If you have been considering selling, upgrading to a larger home, or reallocating your capital into higher-yielding investments, now may be an optimal time to capitalize on peak market valuation.\n\n" +
                    "We would be delighted to prepare a comprehensive Comparative Market Analysis (CMA) and estimate your net proceeds at no cost or obligation.\n\n" +
                    "Warm regards,\n" +
                    "{AgentName}\n" +
                    "Senior Real Estate Listing Specialist\n" +
                    "NEXA Real Estate Advisory";

                return (subject, body, cta, rate);
            }
            else
            {
                // ── ALL PAST CLIENTS PRE-TEMPLATE: General Client Care & Asset Valuation ──
                string subject = "Market Valuation & Equity Report for {PropertyAddress}";
                string cta = "Schedule a Complimentary Equity Consultation";
                decimal rate = 5.0m;
                string body =
                    "Dear {CustomerName},\n\n" +
                    "We hope you are doing well!\n\n" +
                    "As part of our continuous client care service at NEXA Real Estate Advisory, we actively track local transactions and neighborhood appreciation trends.\n\n" +
                    "Based on recent market activity in your area, here is an updated valuation and equity summary for your property:\n\n" +
                    "• Property: {PropertyAddress} ({PropertyType})\n" +
                    "• Acquisition Price: {OriginalPrice}\n" +
                    "• Holding Period: {YearsOwned} year(s)\n" +
                    "• Estimated Current Market Value: {EstimatedValue} (+{AppreciationRate}% est. annual growth)\n" +
                    "• Estimated Equity Growth: +{EquityGain} (+{EquityPercent}% total)\n\n" +
                    "Whether you are thinking about making home improvements, exploring refinancing options, or simply keeping tabs on your net worth, we are here to support your real estate goals.\n\n" +
                    "Warm regards,\n" +
                    "{AgentName}\n" +
                    "NEXA Real Estate Advisory Team";

                return (subject, body, cta, rate);
            }
        }
    }
}
