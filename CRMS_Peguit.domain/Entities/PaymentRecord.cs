using System;

namespace CRMS_Peguit.domain.entities
{
    public enum PaymentMethod
    {
        BankTransfer = 1,
        GCash = 2,
        Check = 3,
        Cash = 4,
        Other = 5
    }

    /// <summary>
    /// Represents an offline/manual payment recorded by a Super Admin for a tenant's subscription.
    /// Does NOT store card numbers, PAN, CVV, or bank account credentials.
    /// Captures proof-of-payment reference (bank transaction ID, GCash ref, check number).
    /// </summary>
    public class PaymentRecord
    {
        public int PaymentRecordId { get; set; }

        /// <summary>Foreign key to the Subscription being paid/extended.</summary>
        public int SubscriptionId { get; set; }

        /// <summary>The monetary amount paid in PHP.</summary>
        public decimal AmountPaid { get; set; }

        /// <summary>External payment channel used (BankTransfer, GCash, Check, Cash, Other).</summary>
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.BankTransfer;

        /// <summary>
        /// Proof-of-payment reference string (e.g. bank transaction ID, GCash reference number, check number).
        /// REQUIRED field for auditability and verification.
        /// </summary>
        public string PaymentReference { get; set; } = string.Empty;

        /// <summary>The date the payment occurred externally.</summary>
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        /// <summary>Foreign key to the SuperAdmin user who verified and recorded the payment.</summary>
        public int RecordedByUserId { get; set; }

        /// <summary>Optional Super Admin notes regarding the transaction or reconciliation.</summary>
        public string? Notes { get; set; }

        /// <summary>Timestamp when this payment record was created in the system.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public Subscription? Subscription { get; set; }
        public SuperAdmin? RecordedBySuperAdmin { get; set; }
    }
}
