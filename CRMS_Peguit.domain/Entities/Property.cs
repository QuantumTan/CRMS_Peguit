using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS_Peguit.domain.entities
{
    public class Property
    {
        public int PropertyId { get; set; }
        public int OwnerCustomerId { get; set; }
        public int ListedByAgentId { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? PropertyType { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}