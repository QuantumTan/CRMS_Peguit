using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS_Peguit.domain.entities
{
    public class LoginSession
    {
        public int SessionId { get; set; }

        public int TenantId { get; set; }
        public int UserId { get; set; }
        public DateTime LoginAt { get; set; }
        public DateTime? LogoutAt { get; set; }
        public string? IpAddress { get; set; }
    }
}
