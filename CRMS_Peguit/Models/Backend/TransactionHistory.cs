using NEXA.Model;
using System;

namespace CRMS_Peguit.winform.Models.Backend
{
    /// <summary>
    /// Tracks state changes and events for deals (transactions).
    /// Inherits BaseEntity which provides Id, CreatedAt, UpdatedAt, etc.
    /// </summary>
    public class TransactionHistory : BaseEntity
    {
        // Id, CreatedAt, UpdatedAt from BaseEntity

        public int DealId { get; set; }
        public virtual Deal Deal { get; set; }

        public int? PropertyId { get; set; }
        public virtual Property Property { get; set; }

        public int? BuyerCustomerId { get; set; }
        public virtual Customer Buyer { get; set; }

        public int? SellerCustomerId { get; set; }
        public virtual Customer Seller { get; set; }

        public int RecordedByUserId { get; set; }
        public virtual User RecordedBy { get; set; }

        public string Action { get; set; }               // e.g., "StatusChanged", "OfferAccepted"
        public string PreviousStatus { get; set; }
        public string NewStatus { get; set; }
        public decimal? Amount { get; set; }
        public string Notes { get; set; }
    }
}
