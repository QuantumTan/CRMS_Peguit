using CRMS_Peguit.domain.entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS_Peguit.domain.entities
{
    public class Device
    {
        public Guid DeviceId { get; set; }
        public int CompanyId { get; set; }
        public string DeviceCode { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // navigation properties
        public Company Company { get; set; } = null!;

    }
}
