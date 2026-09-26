using System;

namespace CRMS_Peguit.domain.entities
{
    public class Property
    {
        public int PropertyId { get; set; }

        public int OwnerCustomerId { get; set; }
        public int CreatedByUserId { get; set; }
        public int? ListedByAgentId { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? PropertyType { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty;
        public string AssignmentStatus { get; set; } = "pending_review";
        public int? AssignmentReviewedByUserId { get; set; }
        public DateTime? AssignmentReviewedAt { get; set; }
        public string? AssignmentReviewNotes { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? BranchId { get; set; }

        public virtual Customer OwnerCustomer { get; set; } = null!;
        public virtual User CreatedByUser { get; set; } = null!;
        public virtual User? ListedByAgent { get; set; }
        public virtual Branch? Branch { get; set; }
    }
}
