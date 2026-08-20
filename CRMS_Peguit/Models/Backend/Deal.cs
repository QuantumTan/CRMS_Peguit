using System;

namespace NEXA.Model
{
    public enum DealStage
    {
        Offer,
        Reservation,
        ContractSigned,
        Closed,
        Lost
    }

    // ============================================
    // CHILD CLASS - Deal (inherits BaseEntity)
    // Represents an offer/reservation/sale on a specific Property.
    // ============================================
    public class Deal : BaseEntity
    {
        // PUBLIC
        public int CustomerId;          // the buyer
        public int PropertyId;          // NEW - the property being bought
        public int AssignedToUserId;    // the agent handling the deal
        public DateTime ExpectedCloseDate;
        public bool ReservationFeePaid;

        // PRIVATE - validated on the way in, controlled transitions
        private decimal _value;
        private decimal _commissionRate;   // e.g. 0.03m for 3%
        private DealStage _stage;

        public Deal(int customerId, int propertyId, decimal value, decimal commissionRate) : base()
        {
            CustomerId = customerId;
            PropertyId = propertyId;
            SetValue(value);
            SetCommissionRate(commissionRate);
            _stage = DealStage.Offer;
        }

        public decimal Value
        {
            get { return _value; }
        }

        public decimal CommissionRate
        {
            get { return _commissionRate; }
        }

        public DealStage Stage
        {
            get { return _stage; }
        }

        // Validation lives in the model, not in the GUI form code
        public void SetValue(decimal newValue)
        {
            if (newValue < 0)
            {
                throw new ArgumentException("Deal value cannot be negative.");
            }
            _value = newValue;
            UpdateTimestamp();
        }

        public void SetCommissionRate(decimal rate)
        {
            if (rate < 0 || rate > 1)
            {
                throw new ArgumentException("Commission rate must be between 0 and 1 (e.g. 0.03 for 3%).");
            }
            _commissionRate = rate;
            UpdateTimestamp();
        }

        // The actual peso amount the agency earns on this deal
        public decimal CalculateCommission()
        {
            return _value * _commissionRate;
        }

        public void AdvanceStage(DealStage nextStage)
        {
            if (_stage == DealStage.Closed || _stage == DealStage.Lost)
            {
                throw new InvalidOperationException("Cannot change stage of a closed deal.");
            }
            _stage = nextStage;
            UpdateTimestamp();
        }

        public void MarkClosed()
        {
            _stage = DealStage.Closed;
            UpdateTimestamp();
        }

        public void MarkLost()
        {
            _stage = DealStage.Lost;
            UpdateTimestamp();
        }
    }
}