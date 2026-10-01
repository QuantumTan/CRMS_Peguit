using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CRMS_Peguit.domain.entities
{
    public class Deal
    {
        public int DealId { get; set; }

        public int CustomerId { get; set; }
        public int PropertyId { get; set; }
        public int? AgentId { get; set; }
        public int CreatedByUserId { get; set; }
        public decimal Value { get; set; }
        public decimal CommissionRate { get; set; }
        public string Stage { get; set; } = string.Empty;
        public DateTime? ExpectedCloseDate { get; set; }
        public DateTime CreatedAt { get; set; }

        // --- Structured Commercial Terms & Conditions ---
        public string? PaymentScheme { get; set; } = "Bank Financing";
        public decimal? ReservationFee { get; set; }
        public decimal? DownPaymentPercent { get; set; } = 20m;

        // --- 3NF Computed Properties (Derivable on the fly, not stored) ---
        [NotMapped]
        public decimal DownPaymentAmount
        {
            get => string.Equals(PaymentScheme, "Spot Cash", StringComparison.OrdinalIgnoreCase)
                ? Value : (Value * (DownPaymentPercent ?? 0m)) / 100m;
            set { /* Calculated on the fly */ }
        }

        [NotMapped]
        public decimal BalanceAmount
        {
            get => Math.Max(0m, Value - DownPaymentAmount - (ReservationFee ?? 0m));
            set { /* Calculated on the fly */ }
        }

        // --- Statutory Tax & Closing Cost Allocation ---
        public string? CgtPayer { get; set; } = "Seller";
        public string? DstPayer { get; set; } = "Buyer";
        public string? TransferTaxPayer { get; set; } = "Buyer";
        public string? RegistrationFeePayer { get; set; } = "Buyer";

        // --- 1NF Normalized Navigation Collections ---
        [JsonIgnore]
        public virtual ICollection<DealContingency> Contingencies { get; set; } = new List<DealContingency>();
        [JsonIgnore]
        public virtual ICollection<DealClause> DealClauses { get; set; } = new List<DealClause>();

        [NotMapped]
        public string? ContingenciesJson
        {
            get => (Contingencies != null && Contingencies.Count > 0) ? DealContingency.SerializeList(Contingencies) : null;
            set
            {
                if (value != null) Contingencies = DealContingency.DeserializeList(value);
            }
        }

        [NotMapped]
        public string? ApprovedClauseIds
        {
            get => DealClauses.Count > 0 ? string.Join(",", DealClauses.Select(c => c.ClauseId))
                : ClauseSelectionProvided ? string.Empty : null;
            set
            {
                if (value == null) return;
                ClauseSelectionProvided = true;
                DealClauses = (value ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(id => new DealClause { ClauseId = id }).ToList();
            }
        }

        [NotMapped, JsonIgnore]
        public bool ClauseSelectionProvided { get; private set; }

        public void MarkClauseSelectionProvided() => ClauseSelectionProvided = true;

        public string? SpecialStipulations { get; set; }
        public DateTime? ContractSignedDate { get; set; }

        public int? BranchId { get; set; }

        // --- Navigation Properties ---
        [JsonIgnore]
        public virtual Customer? Customer { get; set; }
        [JsonIgnore]
        public virtual Property? Property { get; set; }
        [JsonIgnore]
        public virtual User? Agent { get; set; }
        [JsonIgnore]
        public virtual User? CreatedByUser { get; set; }
        [JsonIgnore]
        public virtual Branch? Branch { get; set; }
    }
}
