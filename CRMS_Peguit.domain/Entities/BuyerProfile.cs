using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS_Peguit.domain.entities
{
    public class BuyerProfile
    {
        public int CustomerId { get; set; }

        public int TenantId { get; set; }
        public decimal Budget { get; set; }
        public string? PreferredLocation { get; set; }
        public string? PreferredPropertyType { get; set; }
    }
}
