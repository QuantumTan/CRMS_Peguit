using System;

namespace NEXA.Model
{
    public enum PropertyType
    {
        House,
        Condo,
        Lot,
        Commercial
    }

    public enum PropertyStatus
    {
        Available,
        Reserved,
        Sold,
        Rented
    }

    // ============================================
    // CHILD CLASS - Property (inherits BaseEntity)
    // ============================================
    public class Property : BaseEntity
    {
        // PUBLIC - listing details
        public string Address;
        public PropertyType Type;
        public int Bedrooms;
        public double AreaSqm;
        public int OwnerCustomerId;     // the Seller who owns this property
        public int ListedByUserId;      // the agent who listed it

        // PRIVATE - price and status change through controlled methods
        private decimal _price;
        private PropertyStatus _status;

        public Property(string address, PropertyType type, decimal price, int ownerCustomerId, int listedByUserId) : base()
        {
            Address = address;
            Type = type;
            OwnerCustomerId = ownerCustomerId;
            ListedByUserId = listedByUserId;
            SetPrice(price);
            _status = PropertyStatus.Available;
        }

        public decimal Price
        {
            get { return _price; }
        }

        public PropertyStatus Status
        {
            get { return _status; }
        }

        public void SetPrice(decimal newPrice)
        {
            if (newPrice < 0)
            {
                throw new ArgumentException("Property price cannot be negative.");
            }
            _price = newPrice;
            UpdateTimestamp();
        }

        // Reservation takes the property off-market temporarily while a deal is in progress
        public void MarkReserved()
        {
            if (_status != PropertyStatus.Available)
            {
                throw new InvalidOperationException("Only an available property can be reserved.");
            }
            _status = PropertyStatus.Reserved;
            UpdateTimestamp();
        }

        public void MarkSold()
        {
            _status = PropertyStatus.Sold;
            UpdateTimestamp();
        }

        public void MarkRented()
        {
            _status = PropertyStatus.Rented;
            UpdateTimestamp();
        }

        // If a reservation/deal falls through, the property goes back on the market
        public void ReturnToMarket()
        {
            if (_status == PropertyStatus.Sold || _status == PropertyStatus.Rented)
            {
                throw new InvalidOperationException("Cannot return a closed property to market.");
            }
            _status = PropertyStatus.Available;
            UpdateTimestamp();
        }
    }
}