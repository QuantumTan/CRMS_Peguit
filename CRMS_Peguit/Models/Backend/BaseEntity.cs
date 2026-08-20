using System;

namespace NEXA.Model
{
    
    // PARENT CLASS - BaseEntity
    public class BaseEntity
    {
        public int Id;
        public DateTime CreatedAt;
        public DateTime UpdatedAt;
        public bool IsActive;

        public BaseEntity()
        {
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
            IsActive = true;
        }

        public void UpdateTimestamp()
        {
            UpdatedAt = DateTime.Now;
        }

        public void ToggleActive()
        {
            IsActive = !IsActive;
            UpdatedAt = DateTime.Now;
        }
    }
}